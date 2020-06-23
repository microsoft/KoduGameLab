using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;

namespace Boku.Programming
{
    /// <summary>
    /// Old Desc:
    /// This selector produces a scaled vector indicating desired facing direction and the urgency of our need to achieve it.
    ///
    /// New Desc:
    /// This creates the actions needed to turn.  No clue why Turn is a Selector rather than an Actuator.
    /// Sometimes it just doesn't pay to be too inquisitive.  You end up being expected to fix what you find.
    /// 
    /// </summary>
    public class TurnSelector : Selector
    {
        public override ProgrammingElement Clone()
        {
            TurnSelector clone = new TurnSelector();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(TurnSelector clone)
        {
            base.CopyTo(clone);
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

        public override void Used(bool newUse)
        {
        }

        public override ActionSet ComposeActionSet(Reflex reflex, GameActor gameActor)
        {
            ClearActionSet(actionSet);
            //UpdateCanBlend(reflex);

            if (!reflex.targetSet.AnyAction)
                return actionSet;

            TurnModifier turnMod = reflex.GetModifierByType(typeof(TurnModifier)) as TurnModifier;
            GamePadStickFilter stickFilt = reflex.Data.GetFilterByType(typeof(GamePadStickFilter)) as GamePadStickFilter;
            GamePadTriggerFilter triggerFilt = reflex.Data.GetFilterByType(typeof(GamePadTriggerFilter)) as GamePadTriggerFilter;
            MouseFilter mouseFilt = reflex.Data.GetFilterByType(typeof(MouseFilter)) as MouseFilter;
            TouchGestureFilter touchFilt = reflex.Data.GetFilterByType(typeof(TouchGestureFilter)) as TouchGestureFilter;
            KeyBoardKeyFilter keyBoardKeyFilt = reflex.Data.GetFilterByType(typeof(KeyBoardKeyFilter)) as KeyBoardKeyFilter;

            // Will be modified depending on stick or trigger position.
            float speedModifier = 1.0f;

            // 
            // Start by looking at the input filters to attenuate speed.
            // 

            if (stickFilt != null)
            {
                // Note that we take the sign into account further below.
                // For now, just get the magnitude.
                speedModifier *= (float)Math.Abs(stickFilt.stickPosition.X);
            }
            else if (triggerFilt != null)
            {
                speedModifier *= triggerFilt.triggerValue;
            }
            else if (mouseFilt != null)
            {
                // Note that we take the sign into account further below.
                // For now, just get the magnitude.
                speedModifier *= Math.Abs(mouseFilt.ScreenPosition.X);
            }
            else if (touchFilt != null)
            {
                if (touchFilt.type == TouchGestureFilterType.Rotate)
                {
                    reflex.Task.GameActor.Chassis.WasTouchRotated = true;
                }
            }

            // Decide if we're turning toward a particular direction or
            // setting a turn rate.  Note, this will miss some cases
            // that we clean up further down.  The failure is rooted
            // in TurnModifier if you want to go fix it.  :-)

            Vector3 desiredHeading = Vector3.Zero;
            float targetHeading = 0;

            // See if the modifiers want to give us an absolute direction.
            reflex.ModifyHeading(gameActor, Modifier.ReferenceFrames.World, ref desiredHeading);
            targetHeading = MyMath.ZRotationFromDirection(desiredHeading);

            // If we have a desiredHeading, use that, else assume we're just turning.
            if (desiredHeading != Vector3.Zero)
            {
                // We were given an absolute direction, use it.
                actionSet.AddActionTarget(Action.AllocHeadingAction(reflex, targetHeading, speedModifier));
            }
            else
            {
                // Determine which relative direction we should use.
                TurnModifier.TurnDirections direction = TurnModifier.TurnDirections.None;

                if (turnMod != null)
                {
                    // If a turn modifier was specified, use its direction.
                    direction = turnMod.direction;
                }
                else if (stickFilt != null)
                {
                    // No turn modifier but we have a gamepad stick, use it to determine turn direction.
                    direction = stickFilt.stickPosition.X > 0 ? TurnModifier.TurnDirections.Right : TurnModifier.TurnDirections.Left;
                }
                else if (triggerFilt != null)
                {
                    // No turn modifier but we have a gamepad trigger, use it to determine turn direction.
                    // Basically this automatically maps left trigger to left turn and right trigger to right turn.
                    direction = triggerFilt.trigger == GamePadTriggerFilter.GamePadTrigger.LeftTrigger ? TurnModifier.TurnDirections.Left : TurnModifier.TurnDirections.Right;
                }
                else if (mouseFilt != null)
                {
                    // No turn modifier but we do have mouse input.
                    direction = mouseFilt.ScreenPosition.X > 0 ? TurnModifier.TurnDirections.Right : TurnModifier.TurnDirections.Left;
                    // This scaling by 2 has no real justification except
                    // that it matches the magnitude of the original code
                    // so this is more compatible with older games.
                    speedModifier *= 2.0f;
                }
                else if (touchFilt != null)
                {
                    // No turn modifier but we do have touch input.
                    if (touchFilt.type == TouchGestureFilterType.Rotate)
                    {
                        if (touchFilt.DeltaRotation < 0)
                        {
                            direction = TurnModifier.TurnDirections.Left;
                        }
                        else if (touchFilt.DeltaRotation > 0)
                        {
                            direction = TurnModifier.TurnDirections.Right;
                        }
                    }
                    else if (touchFilt.type == TouchGestureFilterType.Tap)
                    {
                        direction = TurnModifier.TurnDirections.Toward;
                    }
                }
                else if (keyBoardKeyFilt != null)
                {
                    if (reflex.targetSet != null)
                    {
                        Vector2 dir = (Vector2)(reflex.targetSet.Param);
                        if (dir.X == 1)
                        {
                            direction = TurnModifier.TurnDirections.Right;
                        }
                        else if (dir.X == -1)
                        {
                            direction = TurnModifier.TurnDirections.Left;
                        }
                    }
                }
                else
                {
                    // Nothing exists that would help us decide which way we should turn, choose left.
                    // This could be the user programming WHEN DO Turn
                    direction = TurnModifier.TurnDirections.Left;
                }

                // Calculate the new direction we want to face.
                switch (direction)
                {
                    // Turn to our left.
                    case TurnModifier.TurnDirections.Left:
                        {
                            float angle = 0.0f;

                            if (touchFilt != null && touchFilt.type == TouchGestureFilterType.Rotate)
                            {
                                angle = gameActor.Movement.RotationZ + Math.Abs(touchFilt.DeltaRotation);
                            }
                            else
                            {
                                angle = gameActor.Movement.RotationZ + MathHelper.PiOver2;
                            }
                            desiredHeading = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);

                            actionSet.AddActionTarget(Action.AllocTurnSpeedAction(reflex, speedModifier), 0);
                        }
                        break;

                    // Turn to our right.
                    case TurnModifier.TurnDirections.Right:
                        {
                            float angle = 0.0f;

                            if (touchFilt != null && touchFilt.type == TouchGestureFilterType.Rotate)
                            {
                                angle = gameActor.Movement.RotationZ - Math.Abs(touchFilt.DeltaRotation);
                            }
                            else
                            {
                                angle = gameActor.Movement.RotationZ - MathHelper.PiOver2;
                            }

                            desiredHeading = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);

                            actionSet.AddActionTarget(Action.AllocTurnSpeedAction(reflex, -speedModifier), 0);
                        }
                        break;

                    // Turn to face the direction we are moving.
                    case TurnModifier.TurnDirections.Forward:
                        {
                            float angle = MyMath.ZRotationFromDirection(gameActor.Movement.Velocity);
                            desiredHeading = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);

                            actionSet.AddActionTarget(Action.AllocHeadingAction(reflex, targetHeading, speedModifier));
                        }
                        break;

                    // Turn to face the sensed object.
                    case TurnModifier.TurnDirections.Toward:
                        // We're probably here because of WHEN Mouse Left DO Turn Toward.
                        // This is another case where we want a Heading Action.
                        if (reflex.targetSet.Nearest != null)
                        {
                            speedModifier = 1;

                            // If we're doing mouse-look (WHEN Mouse Over DO Turn Toward) try
                            // and create a bit of a dead-zone in the middle.
                            bool mouseOver = false;
                            foreach (Filter f in reflex.Filters)
                            {
                                MouseFilter mf = f as MouseFilter;
                                if (mf != null && mf.type == MouseFilterType.Hover)
                                {
                                    mouseOver = true;
                                    break;
                                }
                            }

                            desiredHeading = reflex.targetSet.Nearest.Direction;
                            targetHeading = MyMath.ZRotationFromDirection(desiredHeading);

                            // If mouseOver, calc damping to slow down turning when already looking 
                            // in the direction we want.  This helps make it less twitchy.
                            float dampingFactor = 1.0f;
                            if (mouseOver)
                            {
                                float dot = Vector3.Dot(desiredHeading, gameActor.Movement.Heading);

                                float deadzoneAngle = 0.96f;
                                float dampzoneAngle = 0.9f;
                                
                                // If very close to center, just treat as zero.
                                if (dot > deadzoneAngle)
                                {
                                    dampingFactor = 0;
                                } 
                                else if (dot > dampzoneAngle)
                                {
                                    // Not in center, but close so damp turning.
                                    // The closer we are to already looking in the correct direction, increase damping.
                                    dampingFactor = MyMath.RemapRange(dot, deadzoneAngle, dampzoneAngle, 0, 1);
                                    // Smooth the result to flatten out the center.
                                    dampingFactor = MyMath.SmoothStep(0, 1, dampingFactor);
                                }
                            }

                            actionSet.AddActionTarget(Action.AllocHeadingAction(reflex, targetHeading, dampingFactor * speedModifier));
                        }
                        break;
                }
            }

            return actionSet;
        }   // end of ComposeActionSet()

        public override bool ActorCompatible(GameActor gameActor)
        {
            if (gameActor != null && !gameActor.Chassis.HasFacingDirection)
                return false;

            return base.ActorCompatible(gameActor);
        }
    }
}
