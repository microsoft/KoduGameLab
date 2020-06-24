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

using KoiX;
using KoiX.Input;

using Boku.Base;
using Boku.Common;
using Boku.Input;

namespace Boku.Programming
{
    public enum MouseFilterType
    {
        Hover,
        LeftButton,
        RightButton,
        Move            // Implies inputs are used in screen space rather than world space.
    }

    /// <summary>
    /// Hybrid filter that provides the source of mouse button input
    /// 
    /// 
    /// </summary>
    public class MouseFilter : Filter
    {
        [XmlAttribute]
        public MouseFilterType type;

        private bool wasPressed = false;    // Keep track if this is a mouse down event
                                            // so we can make the once modifier work.

        private Vector2 screenPosition;     // Normalized mouse position (screen space).

        /// <summary>
        /// Normalized change in mouse position.
        /// Mapeed into [-1, 1]
        /// </summary>
        [XmlIgnore]
        public Vector2 ScreenPosition
        {
            get { return screenPosition; }
        }

        [XmlIgnore]
        public bool IsPressed
        {
            get
            {
                switch (type)
                {
                    case MouseFilterType.LeftButton:
                        bool l = LowLevelMouseInput.Left.IsPressed;
                        wasPressed = LowLevelMouseInput.Left.WasPressed;
                        return l;

                    case MouseFilterType.RightButton:
                        bool r = LowLevelMouseInput.Right.IsPressed;
                        wasPressed = LowLevelMouseInput.Right.WasPressed;
                        return r;

                    case MouseFilterType.Hover:
                        return true;

                    case MouseFilterType.Move:
                        return true;

                    default:
                        throw new Exception("Unknown mouse filter");
                }
            }
        }


        public override ProgrammingElement Clone()
        {
            MouseFilter clone = new MouseFilter();
            CopyTo(clone);
            return clone;
        }

        protected void CopyTo(MouseFilter clone)
        {
            base.CopyTo(clone);
            clone.type = this.type;
        }

        public override bool MatchTarget(Reflex reflex, SensorTarget sensorTarget)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reflex"></param>
        /// <param name="param"></param>
        /// <returns>Return true if the MouseFilter passes.  Ignoring the other filters.</returns>
        public override bool MatchAction(Reflex reflex, out object param)
        {
            bool result = IsPressed;

            // Return as a parameter a vector that can be used for input to the movement system, so
            // that players can drive and turn bots using mouse buttons.
            param = new Vector2(0, 0);

            // Only set the mouse position onto the reflex if the filter actually passes.
            if (result)
            {
                
                // If we have a Once modifier, clear it on mouse down.
                if (wasPressed)
                {
                    for(int i=0; i<reflex.Modifiers.Count; i++)
                    {
                        OnceModifier om = reflex.Modifiers[i] as OnceModifier;
                        if(om != null)
                        {
                            om.Reset(reflex);
                        }
                    }
                }

                if (type == MouseFilterType.Move)
                {
                    // This is poorly named.  What this is doing is not mouse movement but
                    // instead working with a normalized version of the mouse position in
                    // screenspace.

                    // Screen space mouse position, don't care about world space position
                    // or what mouse is over.

                    // Get change in mouse position.
                    Vector2 pos = new Vector2(LowLevelMouseInput.Position.X, LowLevelMouseInput.Position.Y);

                    // Rescale and clamp into [-1,1] range.

                    // Scale to 0,1 range
                    pos.X /= KoiLibrary.GraphicsDevice.Viewport.Width;
                    pos.Y /= KoiLibrary.GraphicsDevice.Viewport.Height;

                    // Transform from 0,1 to -1,1
                    pos.X = pos.X * 2.0f - 1.0f;
                    pos.Y = pos.Y * 2.0f - 1.0f;

                    // Clamp
                    pos.X = MathHelper.Clamp(pos.X, -1.0f, 1.0f);
                    pos.Y = MathHelper.Clamp(-pos.Y, -1.0f, 1.0f);    // Negate so positive is up.

                    screenPosition = pos;
                    param = pos;
                }
                else
                {
                    bool meFilter = reflex.Data.FilterExists("filter.me");

                    if (MouseEdit.MouseTouchHitInfo.ActorHit != null)
                    {
                        // If this reflex uses the movement actuator then totally ignore clicks on self.  This
                        // allows us to still use clicks on self to change state w/o overwriting movement target.
                        if (!(reflex.IsMovement && MouseEdit.MouseTouchHitInfo.ActorHit == reflex.Task.GameActor))
                        {
                            // At this point we need to peek at the filters and look for a MeFilter.  We just use
                            // this to decide what to put into the MousePosition and MouseActor fields.  The real
                            // filtering will be done in the sensor.
                            if (!meFilter || MouseEdit.MouseTouchHitInfo.ActorHit == reflex.Task.GameActor)
                            {
                                reflex.MousePosition = MouseEdit.MouseTouchHitInfo.ActorPosition;
                                reflex.MouseActor = MouseEdit.MouseTouchHitInfo.ActorHit;

                                param = new Vector2(MouseEdit.MouseTouchHitInfo.ActorPosition.X, MouseEdit.MouseTouchHitInfo.ActorPosition.Y);
                            }
                        }
                    }
                    else
                    {
                        if (!meFilter)
                        {
                            reflex.MousePosition = MouseEdit.MouseTouchHitInfo.TerrainPosition;
                            reflex.MouseActor = null;

                            param = new Vector2(MouseEdit.MouseTouchHitInfo.TerrainPosition.X, MouseEdit.MouseTouchHitInfo.TerrainPosition.Y);
                        }
                    }
                }
            }

            // Don't hold onto a ref to a dead bot.
            if (reflex.MouseActor != null && reflex.MouseActor.CurrentState == GameThing.State.Inactive)
            {
                reflex.MouseActor = null;
                reflex.MousePosition = null;
            }

            return result;
        }

        public override void Reset(Reflex reflex)
        {
            base.Reset(reflex);
        }

    }   // end of class MouseButtonFilter

}   // end of namespace Boku.Programming
