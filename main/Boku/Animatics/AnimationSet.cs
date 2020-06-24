
//#define ANIMATION_DEBUG
//#define ANIMATION_DEBUG_MINIMAL       //  show minimal anim info - useful for video capture of oneshots. must also turn on ANIMATION_DEBUG

///
/// This was originally in Common, but migrated here when the new animation
/// system was written.
/// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;

using Boku.Base;
using Boku.Common;
using Boku.SimWorld;
using Boku.SimWorld.Chassis;

using Boku.Animatics;

namespace Boku.Animatics
{
    /// <summary>
    /// Class to hold all the animation controllers for an actor and the 
    /// functions associated with them.
    /// </summary>
    public class AnimationSet
    {
        #region Members 

        private GameActor parent = null;
        private BlendController blendController = null;    // Holds all the other controllers and blends between them.

        private float idleProduceBlend = 0.0f;  // Blend value between idle animations and the produce animations.
                                                // 0 = idle, 1 = produce

        /// <summary>
        /// These are the looping/continuous animations that are blended 
        /// together based on the movement of the chassis.
        /// </summary>
        private SimpleController idleController = null;
        private SimpleController idleWhileOpenController = null;
        private SimpleController produceController = null;
        private SimpleController produceWhileOpenController = null;
        private SimpleController forwardController = null;
        private SimpleController backwardsController = null;
        private SimpleController leftController = null;
        private SimpleController rightController = null;
        private SimpleController inspectController = null;
        private SimpleController beamController = null;
        private SimpleController scanController = null;

        /// <summary>
        /// Controllers for one-shot animations.  Note that these are lists of 
        /// controllers.  This allows there to be multiple versions of each 
        /// animation.  When an event is triggered we randomly pick from the 
        /// available animations.  We also keep a list with all the one-shot
        /// controllers since this is often easier to work with.
        /// </summary>
        private List<SimpleController> allOneShotControllers = null;
        private List<SimpleController> entertainmentControllers = null;
        private List<SimpleController> jumpControllers = null;
        private List<SimpleController> landControllers = null;
        private List<SimpleController> cursorReactControllers = null;
        private List<SimpleController> rocketControllers = null;
        private List<SimpleController> happyControllers = null;
        private List<SimpleController> surpriseControllers = null;
        private List<SimpleController> fearControllers = null;
        private List<SimpleController> excitementControllers = null;
        private List<SimpleController> angerControllers = null;
        private List<SimpleController> sadnessControllers = null;
        private List<SimpleController> dizzinessControllers = null;
        private List<SimpleController> rapidFireControllers = null;
        private List<SimpleController> eatControllers = null;
        private List<SimpleController> kickControllers = null;
        private List<SimpleController> grabbingControllers = null;
        private List<SimpleController> throwControllers = null;
        private List<SimpleController> giveControllers = null;
        private List<SimpleController> dropControllers = null;
        private List<SimpleController> openControllers = null;
        private List<SimpleController> closeControllers = null;
        private List<SimpleController> idleOpenControllers = null;
        private List<SimpleController> idleCloseControllers = null;
        private List<SimpleController> produceOpenControllers = null;
        private List<SimpleController> produceCloseControllers = null;

        private bool oneShotActive = false;     // Is a one-shot animation acitve.

        private bool isOpen = true;             // State of bots that open/close.
        private bool hasIdleWhileOpen = false;  // There are two categories of bots that have the ability to open and close.
                                                // This flag let's us distinguish between them and act accordingly.
                                                // This will be false for bots like the turtle and stickboy where the default 
                                                // idle state is 'open' and the 'closed' state is just the last frame of the 'close' animation.
                                                // This will be true for bot like the factory and hut.  For these bots the default
                                                // idle animation shows the bot in the 'closed' state.  The 'idleWhileOpen' animation
                                                // is used for the bot in the 'idle' state.

        //
        // For the entertainment animation, we want it to run randomly but only if
        // the bot is doing nothing else except the idle animation.
        //
        private double lastNonIdleTime = 0.0f;  // This is updated to the current time whenever 
                                                // any non-idle animation is playing.
        private float minWaitTime = 5.0f;       // The minimum amount of time we'll have between idle animations.
        private float deltaWaitTime = 10.0f;    // The random amount of time added on to the minimum wait time.
        private float waitTime = 5.0f;          // The amount of time we're waiting before triggering an entertainment 
                                                // animation.  This will get reset each time an entertainment animation 
                                                // is triggered.
        private long idleTicks;                 // This is where the idle animation is in it's loop when we decide to
                                                // start an entertainment animation.  We'll use this to tell when the
                                                // animation loops and trigger the entertainment then.

        private long idleValueAtEntStart = 0;       // This is the position of the idle animation when we decided to do an entertainment animation.
                                                    // Used to calc the below attenuation.
        private float backwardsAttenuation = 1.0f;  // Weighting used to attenuate backwards.

        #endregion

        #region Accessors

        /// <summary>
        /// Returns true if a one-shot animation is already
        /// running, false otherwise.
        /// </summary>
        public bool OneShotAnimationActive
        {
            get { return oneShotActive; }
            set { oneShotActive = value; }
        }

        /// <summary>
        /// Only relevant for bots that have open and close animations.  This
        /// is how the chassis can tell if the bot is closed and act accordingly.
        /// Note, this should only be set during a bot's initialization.  This
        /// shouldn't be set externally otherwise.
        /// </summary>
        public bool IsOpen
        {
            get { return isOpen; }
            set { isOpen = value; }
        }

        /// <summary>
        /// Set the weight for the idle animation.
        /// </summary>
        public float IdleWeight
        {
            set
            {
                if (idleController != null)
                {
                    idleController.Weight = CalcWeight(idleController.Weight, value);
                }
            }
            get { return idleController.Weight; }
        }

        /// <summary>
        /// Set the weight for the forward animation.
        /// </summary>
        public float ForwardWeight
        {
            set
            {
                if (forwardController != null)
                {
                    forwardController.Weight = CalcWeight(forwardController.Weight, value);
                }
            }
            get { return forwardController.Weight; }
        }

        /// <summary>
        /// Set the weight back the backward animation.
        /// </summary>
        public float BackwardsWeight
        {
            set
            {
                if (backwardsController != null)
                {
                    backwardsController.Weight = CalcWeight(backwardsController.Weight, value);
                }
            }
            get { return backwardsController.Weight; }
        }

        /// <summary>
        /// Set the weight for the right animation.
        /// </summary>
        public float RightWeight
        {
            set
            {
                if (rightController != null)
                {
                    rightController.Weight = CalcWeight(rightController.Weight, value);
                }
            }
            get { return rightController.Weight; }
        }

        /// <summary>
        /// Set the weight for the left animation.
        /// </summary>
        public float LeftWeight
        {
            set
            {
                if (leftController != null)
                {
                    leftController.Weight = CalcWeight(leftController.Weight, value);
                }
            }
            get { return leftController.Weight; }
        }

        /// <summary>
        /// Set the weight for the inspect animation.
        /// </summary>
        public float InspectWeight
        {
            set
            {
                if (inspectController != null)
                {
                    inspectController.Weight = CalcWeight(inspectController.Weight, value);
                }
            }
            get { return inspectController.Weight; }
        }

        /// <summary>
        /// Set the weight for the beam animation.
        /// </summary>
        public float BeamWeight
        {
            set
            {
                if (beamController != null)
                {
                    beamController.Weight = CalcWeight(beamController.Weight, value);
                }
            }
            get { return beamController.Weight; }
        }

        /// <summary>
        /// Set the weight for the scan animation.
        /// </summary>
        public float ScanWeight
        {
            set
            {
                if (scanController != null)
                {
                    scanController.Weight = CalcWeight(scanController.Weight, value);
                }
            }
            get { return scanController.Weight; }
        }

        #region Internal Accessors - limit exposure to underlying controllers.

        /// <summary>
        /// Debug access to the multiblend controller.
        /// </summary>
        private BlendController BlendController
        {
            get { return blendController; }
        }

        /// <summary>
        /// Debug access to the idle animation controller.
        /// </summary>
        public SimpleController IdleController
        {
            get { return idleController; }
        }

        /// <summary>
        /// Debug access to the forward animation controller.
        /// </summary>
        private SimpleController ForwardController
        {
            get { return forwardController; }
        }

        /// <summary>
        /// Debug access to the backwards animation controller.
        /// </summary>
        private SimpleController BackwardsController
        {
            get { return backwardsController; }
        }

        /// <summary>
        /// Debug access to the left animation controller.
        /// </summary>
        private SimpleController LeftController
        {
            get { return leftController; }
        }

        /// <summary>
        /// Debug access to the right animation controller.
        /// </summary>
        private SimpleController RightController
        {
            get { return rightController; }
        }

        /// <summary>
        /// Hack access to the open controller.
        /// </summary>
        private SimpleController OpenController
        {
            get
            {
                SimpleController c = null;
                if (openControllers != null && openControllers.Count > 0 && openControllers[0] != null)
                {
                    c = openControllers[0];
                }

                return c;
            }
        }

        /// <summary>
        /// Hack access to the close controller.
        /// </summary>
        private SimpleController CloseController
        {
            get
            {
                SimpleController c = null;
                if (closeControllers != null && closeControllers.Count > 0 && closeControllers[0] != null)
                {
                    c = closeControllers[0];
                }

                return c;
            }
        }

        /// <summary>
        /// Debug access to the inspect controller.
        /// </summary>
        private SimpleController InspectController
        {
            get { return inspectController; }
        }


        /// <summary>
        /// Debug access to the beam controller.
        /// </summary>
        private SimpleController BeamController
        {
            get { return beamController; }
        }

        /// <summary>
        /// Debug access to the scan controller.
        /// </summary>
        private SimpleController ScanController
        {
            get { return scanController; }
        }


        /// <summary>
        /// List of all one-shot controllers.
        /// </summary>
        private List<SimpleController> AllOneShotControllers
        {
            get { return allOneShotControllers; }
        }

        #endregion Internal Accessors

        #endregion

        #region Public

        /// <summary>
        /// Empty constructor creates a no-op animation set.
        /// </summary>
        public AnimationSet()
        {
        }

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="animator"></param>
        public AnimationSet(GameActor parent, FBXModel model)
        {
            AnimatorList animList = new AnimatorList(model);
            /// For the rest of this, the first animator will do as well as any.
            AnimationInstance animator = animList.Sample;

            this.parent = parent;

            // At a minimum we need to have an idle animation.
            if (animator != null && animator.HasAnimation("idle"))
            {
                blendController = new BlendController(animator, parent.GetType().ToString());

                // Set up looped animations.  Start them at a random spot 
                // in their cycle so all the bots don't look like clones.
                idleController = animator.TryMake("idle", null);
                idleController.SetToRandom();

                produceController = animator.TryMake("produce", "idle");
                produceController.SetToRandom();

                hasIdleWhileOpen = animator.HasAnimation("idlewhileopen");

                idleWhileOpenController = animator.TryMake("idlewhileopen", "idle");
                idleWhileOpenController.SetToRandom();

                produceWhileOpenController = animator.TryMake("producewhileopen", "idle");
                produceWhileOpenController.SetToRandom();

                // For forward, backward, left and right, if the animations don't exist just create another idle controller.
                forwardController = animator.TryMake("forward", "idle");
                forwardController.SetToRandom();

                backwardsController = animator.TryMake("backwards", "idle");
                backwardsController.SetToRandom();

                leftController = animator.TryMake("left", "idle");
                leftController.SetToRandom();

                rightController = animator.TryMake("right", "idle");
                rightController.SetToRandom();

                // Science-specific looping animations
                inspectController = animator.TryMake("inspect", "idle");
                beamController = animator.TryMake("beam", "idle");
                scanController = animator.TryMake("scan", "idle");

                // Init weights for looped animations and add them to the multiblend controller list.
                idleController.Weight = 1.0f;
                blendController.Add(idleController);
                idleWhileOpenController.Weight = 0.0f;
                blendController.Add(idleWhileOpenController);

                produceController.Weight = 0.0f;
                blendController.Add(produceController);
                produceWhileOpenController.Weight = 0.0f;
                blendController.Add(produceWhileOpenController);
                
                forwardController.Weight = 0.0f;
                blendController.Add(forwardController);
                backwardsController.Weight = 0.0f;
                blendController.Add(backwardsController);
                leftController.Weight = 0.0f;
                blendController.Add(leftController);
                rightController.Weight = 0.0f;
                blendController.Add(rightController);

                inspectController.Weight = 0.0f;
                blendController.Add(inspectController);
                beamController.Weight = 0.0f;
                blendController.Add(beamController);
                scanController.Weight = 0.0f;
                blendController.Add(scanController);

                // Set up lists for one-shot animations.
                allOneShotControllers = new List<SimpleController>();
                
                closeControllers = GetControllerList(animator, "close");
                openControllers = GetControllerList(animator, "open");
                idleCloseControllers = GetControllerList(animator, "idleclose");
                idleOpenControllers = GetControllerList(animator, "idleopen");
                produceCloseControllers = GetControllerList(animator, "produceclose");
                produceOpenControllers = GetControllerList(animator, "produceopen");

                jumpControllers = GetControllerList(animator, "jump");
                landControllers = GetControllerList(animator, "land");
                cursorReactControllers = GetControllerList(animator, "cursorreact");
                rocketControllers = GetControllerList(animator, "rocket");
                happyControllers = GetControllerList(animator, "happy");
                surpriseControllers = GetControllerList(animator, "surprise");
                fearControllers = GetControllerList(animator, "fear");
                excitementControllers = GetControllerList(animator, "excitement");
                angerControllers = GetControllerList(animator, "anger");
                sadnessControllers = GetControllerList(animator, "sadness");
                dizzinessControllers = GetControllerList(animator, "dizziness");
                rapidFireControllers = GetControllerList(animator, "rapidfire");
                eatControllers = GetControllerList(animator, "eat");
                kickControllers = GetControllerList(animator, "kick");
                grabbingControllers = GetControllerList(animator, "grabbing");
                throwControllers = GetControllerList(animator, "throw");
                giveControllers = GetControllerList(animator, "give");
                dropControllers = GetControllerList(animator, "drop");

                // Do these last so that they never interfere with "real" animations.
                entertainmentControllers = GetControllerList(animator, "entertainment");

                parent.SetAnimators(animList);
                parent.SetAnimation(blendController);

                lastNonIdleTime = Time.GameTimeTotalSeconds;
                waitTime = minWaitTime + deltaWaitTime * (float)BokuGame.bokuGame.rnd.NextDouble();
            }

        }   // end of c'tor


        public void InitDefaults()
        {
            IsOpen = true;
            OneShotAnimationActive = false;


            if (idleController != null)
            {
                idleController.Weight = 1.0f;
                idleController.Loop = true;
            }
            if (produceController != null)
            {
                produceController.Weight = 0.0f;
                produceController.Loop = true;
            }
            if (forwardController != null)
            {
                forwardController.Weight = 0.0f;
                forwardController.Loop = true;
            }
            if (backwardsController != null)
            {
                backwardsController.Weight = 0.0f;
                backwardsController.Loop = true;
            }
            if (leftController != null)
            {
                leftController.Weight = 0.0f;
                leftController.Loop = true;
            }
            if (rightController != null)
            {
                rightController.Weight = 0.0f;
                rightController.Loop = true;
            }

            if (inspectController != null)
            {
                inspectController.Weight = 0.0f;
                inspectController.Loop = true;
            }

            if (beamController != null)
            {
                beamController.Weight = 0.0f;
                beamController.Loop = true;
            }

            if (scanController != null)
            {
                scanController.Weight = 0.0f;
                scanController.Loop = true;
            }

            if (allOneShotControllers != null)
            {
                for (int i = 0; i < allOneShotControllers.Count; ++i)
                {
                    SimpleController controller = allOneShotControllers[i];
                    controller.Weight = 0.0f;
                    controller.Loop = false;
                }
            }
        }

        public void Update()
        {
            // Blend idle and move sounds.
            float loudness = parent.Movement.Speed / parent.Chassis.MaxSpeed;
            loudness = MathHelper.Clamp(loudness, 0.0f, 1.0f);
            if (parent.idleCue != null)
            {
                parent.idleCue.SetVolume(1.0f);
            }
            if (parent.moveCue != null)
            {
                parent.moveCue.SetVolume(loudness);
            }

            if (blendController != null)
            {
                // Check for any animation triggers from the chassis.
                if (parent.Chassis.StartJumpAnimation)
                {
                    StartJumpAnimation();
                    parent.Chassis.StartJumpAnimation = false;  // Clear the flag to acknowledge the animation has started.
                }

                if (parent.Chassis.StartLandAnimation)
                {
                    StartLandAnimation();
                    parent.Chassis.StartLandAnimation = false;  // Clear the flag to acknowledge the animation has started.
                }

				// Blend on/off science-specific animations.
                if (parent.CurrentScienceAction == GameActor.ScienceAction.Inspect)
                {
                    // Blend up to full inspect via accessor.
                    InspectWeight = 1.0f;
                }
                else if (BeamWeight > 0.0f)
                {
                    // Blend off.
                    InspectWeight = 0.0f;
                }

                if (parent.CurrentScienceAction == GameActor.ScienceAction.Beam)
                {
                    // Blend up to full beam via accessor.
                    BeamWeight = 1.0f;
                }
                else if (BeamWeight > 0.0f)
                {
                    // Blend off.
                    BeamWeight = 0.0f;
                }

                if (parent.CurrentScienceAction == GameActor.ScienceAction.Scan)
                {
                    // Blend up to full beam via accessor.
                    ScanWeight = 1.0f;
                }
                else if (ScanWeight > 0.0f)
                {
                    // Blend off.
                    ScanWeight = 0.0f;
                }

                bool prevActive = oneShotActive;
                oneShotActive = false;

                if (allOneShotControllers != null)
                {
                    // Zero out all weights to start.
                    for (int i = 0; i < allOneShotControllers.Count; i++)
                    {
                        SimpleController a = allOneShotControllers[i];
                        a.Weight = 0.0f;
                    }

                    // This is kind of a hack for characters that don't have an
                    // idleWhileOpen.  After closing, we want to stick 
                    // with the last frame of the close animation rather than 
                    // going back to the idle animation.
                    if (!isOpen && closeControllers.Count > 0 && !hasIdleWhileOpen)
                    {
                        closeControllers[0].Weight = 1.0f;
                        oneShotActive = true;
                    }
                    else
                    {
                        // Loop over all the one-shot animations and find first active one.
                        // We can only have one active at a time.
                        for (int i = 0; i < allOneShotControllers.Count; i++)
                        {
                            SimpleController a = allOneShotControllers[i];
                            if (!a.AtEnd)
                            {
                                a.Weight = 1.0f;
                                oneShotActive = true;

                                break;
                            }
                        }
                    }

                    // If we're transitioning from a one-shot animation to none...
                    if (!oneShotActive && prevActive)
                    {
                        // Restart all looping animations at their beginning
                        // and set so idle has full weighting.
                        idleController.Weight = 1.0f;
                        idleWhileOpenController.Weight = 0.0f;
                        produceController.Weight = 0.0f;
                        produceWhileOpenController.Weight = 0.0f;
                        forwardController.Weight = 0.0f;
                        backwardsController.Weight = 0.0f;
                        leftController.Weight = 0.0f;
                        rightController.Weight = 0.0f;
                        inspectController.Weight = 0.0f;
                        beamController.Weight = 0.0f;
                        scanController.Weight = 0.0f;

                        idleController.SetToBegin();
                        idleWhileOpenController.SetToBegin();
                        produceController.SetToBegin();
                        produceWhileOpenController.SetToBegin();
                        forwardController.SetToBegin();
                        backwardsController.SetToBegin();
                        leftController.SetToBegin();
                        rightController.SetToBegin();
                        inspectController.SetToBegin();
                        beamController.SetToBegin();
                        scanController.SetToBegin();
                    }

                }   // end if we have any one-shot animations.

                if (oneShotActive)
                {
                    // Zero out all looping weights.
                    idleController.Weight = 0.0f;
                    idleWhileOpenController.Weight = 0.0f;
                    produceController.Weight = 0.0f;
                    produceWhileOpenController.Weight = 0.0f;
                    forwardController.Weight = 0.0f;
                    backwardsController.Weight = 0.0f;
                    leftController.Weight = 0.0f;
                    rightController.Weight = 0.0f;
                    inspectController.Weight = 0.0f;
                    beamController.Weight = 0.0f;
                    scanController.Weight = 0.0f;
                }
                else
                {
                    // Get weights for looped animations from chassis.
                    parent.Chassis.SetLoopedAnimationWeights(this, parent.Movement, parent.DesiredMovement);

                    // Damp wind animation if we're about to go into an entertainment animation to prevent popping.
                    // Add extra weighting to idle.
                    float w = backwardsController.Weight;
                    backwardsController.Weight *= backwardsAttenuation;
                    idleController.Weight += MathHelper.Clamp(w - backwardsController.Weight, 0.0f, 1.0f);

                    // Normalize weighting of looped animations.
                    float sum = idleController.Weight + forwardController.Weight + 
                                    backwardsController.Weight + 
                                    rightController.Weight + 
                                    leftController.Weight + 
                                    inspectController.Weight +
                                    beamController.Weight + 
                                    scanController.Weight;

                    if (sum != 1.0f)
                    {
                        idleController.Weight /= sum;
                        forwardController.Weight /= sum;
                        backwardsController.Weight /= sum;
                        leftController.Weight /= sum;
                        rightController.Weight /= sum;
                        inspectController.Weight /= sum;
                        beamController.Weight /= sum;
                        scanController.Weight /= sum;
                    }
                }

                // Are we in idle?  Backwards is used by some chassis for blended wind animation so we also consider it "idle" time.
                bool isIdle = (idleController.Weight + backwardsController.Weight) > 0.999f;

                if (idleValueAtEntStart != 0)
                {
                    if (idleController.CurrentTicks < idleValueAtEntStart)
                    {
                        backwardsAttenuation = 0.0f;
                    }
                    else
                    {
                        backwardsAttenuation = (float)(idleController.Duration - idleController.CurrentTicks) / (float)(idleController.Duration - idleValueAtEntStart);
                        backwardsAttenuation = MathHelper.Clamp(backwardsAttenuation, 0.0f, 1.0f);
                    }
                }
                else
                {
                    // Return the wind strength to 1 once the entertainment has finished.
                    if (isIdle)
                    {
                        backwardsAttenuation = MyMath.Lerp(backwardsAttenuation, 1.0f, Time.GameTimeFrameSeconds);
                    }
                }

                // Time-wise, we've waited long enough to start an entertainment 
                // but now we want to wait until the idle animation loops so that
                // we don't get a pop in the animation.  For bots with a backwards
                // animation used by wind we also want to take this time to 
                // attenuate the wind animation since it too will cause a pop.
                float elapsed = (float)(Time.GameTimeTotalSeconds - lastNonIdleTime);
                if (isIdle && elapsed > waitTime && idleValueAtEntStart == 0)
                {
                    idleValueAtEntStart = idleController.CurrentTicks;
                }

                // See if we want to trigger an entertainment animation.
                if (isIdle)
                {
                    float elapsedTime = (float)(Time.GameTimeTotalSeconds - lastNonIdleTime);
                    if (elapsedTime > waitTime && idleTicks > idleController.CurrentTicks)
                    {
                        StartEntertainmentAnimation();
                        idleValueAtEntStart = 0;
                        waitTime = minWaitTime + deltaWaitTime * (float)BokuGame.bokuGame.rnd.NextDouble();
                    }
                }
                else
                {
                    lastNonIdleTime = Time.GameTimeTotalSeconds;
                }
                idleTicks = idleController.CurrentTicks;

                // Finally, if we have specific idle animations for 'open' and 'closed' 
                // states, substitute those for the generic idle animation.
                // Also blend with 'produce' animations here.
                if (hasIdleWhileOpen)
                {
                    float weight = idleController.Weight;
                    
                    // Zero out everything to start.
                    idleController.Weight = 0.0f;
                    idleWhileOpenController.Weight = 0.0f;
                    produceController.Weight = 0.0f;
                    produceWhileOpenController.Weight = 0.0f;

                    if (weight > 0.0f)
                    {
                        if (IsOpen)
                        {
                            idleWhileOpenController.Weight = (1.0f - idleProduceBlend) * weight;
                            produceWhileOpenController.Weight = idleProduceBlend * weight;
                        }
                        else
                        {
                            idleController.Weight = (1.0f - idleProduceBlend) * weight;
                            produceController.Weight = idleProduceBlend * weight;
                        }
                    }
                    else
                    {
                        idleWhileOpenController.Weight = 0.0f;
                    }
                }

                /*
                // Debug for finding cases where multiple controllers have full weight.
                {
                    float t = 0.0f;
                        
                    t += idleController.Weight;
                    t += idleWhileOpenController.Weight;
                    t += produceController.Weight;
                    t += produceWhileOpenController.Weight;
                    t += forwardController.Weight;
                    t += backwardsController.Weight;
                    t += leftController.Weight;
                    t += rightController.Weight;

                    for (int i = 0; i < AllOneShotControllers.Count; i++)
                    {
                        t += allOneShotControllers[i].Weight;
                    }

                    if (t > 1.1f || t < 0.9f)
                    {
                        ;
                    }
                }
                */

                long advTicks = (long)(Time.GameTimeFrameSeconds * TimeSpan.TicksPerSecond);
                blendController.Update(advTicks);

            }   // end if blendController != null

        }   // end of Update()

        /// <summary>
        /// Starts an entertainment animation.
        /// </summary>
        public void StartEntertainmentAnimation()
        {
            if (entertainmentControllers != null && !oneShotActive && entertainmentControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(entertainmentControllers.Count);
                entertainmentControllers[index].SetToBegin();

                // Play any associated sound.
                string name = null;
                switch(index)
                {
                    case 0:
                        name = parent.Ent1SoundName;
                        break;
                    case 1:
                        name = parent.Ent2SoundName;
                        break;
                    case 2:
                        name = parent.Ent3SoundName;
                        break;
                    case 3:
                        name = parent.Ent4SoundName;
                        break;
                    case 4:
                        name = parent.Ent5SoundName;
                        break;
                    case 5:
                        name = parent.Ent6SoundName;
                        break;
                    case 6:
                        name = parent.Ent7SoundName;
                        break;
                    case 7:
                        name = parent.Ent8SoundName;
                        break;
                    case 8:
                        name = parent.Ent9SoundName;
                        break;
                    case 9:
                        name = parent.Ent10SoundName;
                        break;
                    case 10:
                        name = parent.Ent11SoundName;
                        break;
                    case 11:
                        name = parent.Ent12SoundName;
                        break;
                    default:
                        // this space intentionally left blank
                        break;
                }

                PlayCue(name);
            }
        }   // end of StartEntertainmentAnimation()

        /// <summary>
        /// Starts a jump animation.
        /// </summary>
        public void StartJumpAnimation()
        {
            if (jumpControllers != null && !oneShotActive && jumpControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(jumpControllers.Count);
                jumpControllers[index].SetToBegin();
                PlayCue(parent.JumpSoundName);
            }
        }   // end of StartJumpAnimation()

        /// <summary>
        /// Starts a land animation.
        /// </summary>
        public void StartLandAnimation()
        {
            if (landControllers != null && !oneShotActive && landControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(landControllers.Count);
                landControllers[index].SetToBegin();
            }
        }   // end of StartLandAnimation()

        /// <summary>
        /// Starts a CursorReact animation.
        /// </summary>
        public void StartCursorReactAnimation()
        {
            if (cursorReactControllers != null && !oneShotActive && cursorReactControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(cursorReactControllers.Count);
                cursorReactControllers[index].SetToBegin();
                PlayCue(parent.CursorSoundName);
            }
        }   // end of StartCursorReactAnimation()

        /// <summary>
        /// Starts a Rocket animation.  If one is already playing
        /// then trigger a RapidFire animation instead.
        /// </summary>
        public void StartRocketAnimation()
        {
            if (rocketControllers != null && rocketControllers.Count > 0)
            {
                if (!oneShotActive)
                {
                    int index = BokuGame.bokuGame.rnd.Next(rocketControllers.Count);
                    rocketControllers[index].SetToBegin();
                }
                else
                {
                    // Something is already playing.  If it's a rocket or rapidfire animation, stop
                    // it and start a rapidfire instead.  Loop through rocket controllers.
                    // If we find one that's playing, stop it and start a rapidfire animation.
                    bool found = false;
                    for (int i = 0; i < rocketControllers.Count; i++)
                    {
                        SimpleController a = rocketControllers[i];
                        if (!a.AtEnd)
                        {
                            a.SetToEnd();
                            oneShotActive = false;
                            StartRapidFireAnimation();
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        for (int i = 0; i < rapidFireControllers.Count; i++)
                        {
                            SimpleController a = rapidFireControllers[i];
                            if (!a.AtEnd)
                            {
                                // The rapidfire animation is already playing so
                                // set the flag to play it one more time.
                                a.OneMoreLoop = true;
                                break;
                            }
                        }
                    }
                }
                PlayCue(parent.ShootSoundName);
            }
        }   // end of StartRocketAnimation()

        /// <summary>
        /// Starts a Happy animation.
        /// </summary>
        public void StartHappyAnimation()
        {
            if (happyControllers != null && !oneShotActive && happyControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(happyControllers.Count);
                happyControllers[index].SetToBegin();
            }
        }   // end of StartHappyAnimation()

        /// <summary>
        /// Starts a Surprise animation.
        /// </summary>
        public void StartSurpriseAnimation()
        {
            if (surpriseControllers != null && !oneShotActive && surpriseControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(surpriseControllers.Count);
                surpriseControllers[index].SetToBegin();
            }
        }   // end of StartSurpriseAnimation()

        /// <summary>
        /// Starts a Fear animation.
        /// </summary>
        public void StartFearAnimation()
        {
            if (fearControllers != null && !oneShotActive && fearControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(fearControllers.Count);
                fearControllers[index].SetToBegin();
            }
        }   // end of StartFearAnimation()

        /// <summary>
        /// Starts a Excitement animation.
        /// </summary>
        public void StartExcitementAnimation()
        {
            if (excitementControllers != null && !oneShotActive && excitementControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(excitementControllers.Count);
                excitementControllers[index].SetToBegin();
            }
        }   // end of StartExcitementAnimation()

        /// <summary>
        /// Starts a Anger animation.
        /// </summary>
        public void StartAngerAnimation()
        {
            if (angerControllers != null && !oneShotActive && angerControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(angerControllers.Count);
                angerControllers[index].SetToBegin();
            }
        }   // end of StartAngerAnimation()

        /// <summary>
        /// Starts a Sadness animation.
        /// </summary>
        public void StartSadnessAnimation()
        {
            if (sadnessControllers != null && !oneShotActive && sadnessControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(sadnessControllers.Count);
                sadnessControllers[index].SetToBegin();
            }
        }   // end of StartSadnessAnimation()

        /// <summary>
        /// Starts a Dizziness animation.
        /// </summary>
        public void StartDizzinessAnimation()
        {
            if (dizzinessControllers != null && !oneShotActive && dizzinessControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(dizzinessControllers.Count);
                dizzinessControllers[index].SetToBegin();
            }
        }   // end of StartDizzinessAnimation()

        /// <summary>
        /// Starts a RapidFire animation.
        /// </summary>
        public void StartRapidFireAnimation()
        {
            if (rapidFireControllers != null && rapidFireControllers.Count > 0)
            {
                if (oneShotActive)
                {
                    // Check if the currently active one-shot is a rapidFire, if so
                    // then set it's flag to play another loop.
                    for (int i = 0; i < rapidFireControllers.Count; i++)
                    {
                        SimpleController ac = rapidFireControllers[i];
                        if (ac.Weight == 1.0f)
                        {
                            ac.OneMoreLoop = true;
                        }
                    }
                }
                else
                {
                    // Nothing is active so just start a rapidFire animation.
                    int index = BokuGame.bokuGame.rnd.Next(rapidFireControllers.Count);
                    rapidFireControllers[index].SetToBegin();
                }
                PlayCue(parent.RapidFireSoundName);
            }
        }   // end of StartRapidFireAnimation()

        /// <summary>
        /// Starts a Eat animation.
        /// </summary>
        public void StartEatAnimation()
        {
            if (eatControllers != null && !oneShotActive && eatControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(eatControllers.Count);
                eatControllers[index].SetToBegin();
            }
        }   // end of StartEatAnimation()

        /// <summary>
        /// Starts a Kick animation.
        /// </summary>
        public void StartKickAnimation()
        {
            if (kickControllers != null && !oneShotActive && kickControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(kickControllers.Count);
                kickControllers[index].SetToBegin();
            }
        }   // end of StartKickAnimation()

        /// <summary>
        /// Starts a Grabbing animation.
        /// </summary>
        public void StartGrabbingAnimation()
        {
            if (grabbingControllers != null && !oneShotActive && grabbingControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(grabbingControllers.Count);
                grabbingControllers[index].SetToBegin();
            }
        }   // end of StartGrabbingAnimation()

        /// <summary>
        /// Starts a Throw animation.
        /// </summary>
        public void StartThrowAnimation()
        {
            if (throwControllers != null && !oneShotActive && throwControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(throwControllers.Count);
                throwControllers[index].SetToBegin();
            }
        }   // end of StartThrowAnimation()

        /// <summary>
        /// Starts a Give animation.
        /// </summary>
        public void StartGiveAnimation()
        {
            if (giveControllers != null && !oneShotActive && giveControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(giveControllers.Count);
                giveControllers[index].SetToBegin();
            }
        }   // end of StartGiveAnimation()

        /// <summary>
        /// Starts a Drop animation.
        /// </summary>
        public void StartDropAnimation()
        {
            if (dropControllers != null && !oneShotActive && dropControllers.Count > 0)
            {
                int index = BokuGame.bokuGame.rnd.Next(dropControllers.Count);
                dropControllers[index].SetToBegin();
            }
        }   // end of StartDropAnimation()

        /// <summary>
        /// Starts an open animation.  Allowed to override other animations.
        /// </summary>
        public void StartOpenAnimation()
        {
            if (openControllers != null && openControllers.Count > 0)
            {
                // If something else is already running.  Find it and stop it.
                StopAnyOneShotAnimations();

                int index = BokuGame.bokuGame.rnd.Next(openControllers.Count);
                openControllers[index].SetToBegin();
                
                isOpen = true;
                PlayCue(parent.OpenSoundName);
            }

            if (idleProduceBlend < 0.5f)
            {
                if (idleOpenControllers != null && idleOpenControllers.Count > 0)
                {
                    // If something else is already running.  Find it and stop it.
                    StopAnyOneShotAnimations();

                    int index = BokuGame.bokuGame.rnd.Next(idleOpenControllers.Count);
                    idleOpenControllers[index].SetToBegin();

                    isOpen = true;
                    PlayCue(parent.OpenSoundName);
                }
            }
            else
            {
                if (produceOpenControllers != null && produceOpenControllers.Count > 0)
                {
                    // If something else is already running.  Find it and stop it.
                    StopAnyOneShotAnimations();

                    int index = BokuGame.bokuGame.rnd.Next(produceOpenControllers.Count);
                    produceOpenControllers[index].SetToBegin();

                    isOpen = true;
                    PlayCue(parent.OpenSoundName);
                }
            }
        }   // end of StartOpenAnimation()

        /// <summary>
        /// Starts a close animation.
        /// </summary>
        public void StartCloseAnimation()
        {
            if (closeControllers != null && closeControllers.Count > 0)
            {
                // If something else is already running.  Find it and stop it.
                StopAnyOneShotAnimations();

                int index = BokuGame.bokuGame.rnd.Next(closeControllers.Count);
                closeControllers[index].SetToBegin();
                
                isOpen = false;
                PlayCue(parent.CloseSoundName);
            }

            if (idleProduceBlend < 0.5f)
            {
                if (idleCloseControllers != null && idleCloseControllers.Count > 0)
                {
                    // If something else is already running.  Find it and stop it.
                    StopAnyOneShotAnimations();

                    int index = BokuGame.bokuGame.rnd.Next(idleCloseControllers.Count);
                    idleCloseControllers[index].SetToBegin();

                    isOpen = false;
                    PlayCue(parent.CloseSoundName);
                }
            }
            else
            {
                if (produceCloseControllers != null && produceCloseControllers.Count > 0)
                {
                    // If something else is already running.  Find it and stop it.
                    StopAnyOneShotAnimations();

                    int index = BokuGame.bokuGame.rnd.Next(produceCloseControllers.Count);
                    produceCloseControllers[index].SetToBegin();

                    isOpen = false;
                    PlayCue(parent.CloseSoundName);
                }
            }
        }   // end of StartcloseAnimation()

        /// <summary>
        /// Starts a one-shot animation based on the passed 
        /// in index.  Used by the anim debug mode.
        /// </summary>
        /// <param name="i"></param>
        public void StartOneShotAnimation(int i)
        {
            if (allOneShotControllers != null && !oneShotActive && i < allOneShotControllers.Count)
            {
                // If something else is already running.  Find it and stop it.
                StopAnyOneShotAnimations();

                allOneShotControllers[i].SetToBegin();
            }
        }   // end of StartOneShotAnimation()

        /// <summary>
        /// Set the open or close controller to the end of the animation.
        /// </summary>
        /// <param name="open"></param>
        public void SetOpenCloseToEnd(bool open)
        {
            SimpleController c = open ? OpenController : CloseController;
            if (c != null)
            {
                c.SetToEnd();
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// Simple wrapper around playing cues which gives us
        /// some protection against bad or null cue names.
        /// </summary>
        /// <param name="name"></param>
        public void PlayCue(string name)
        {
            if (BokuGame.Audio.Enabled)
            {
                try
                {
                    if (name != null && !parent.Mute)
                    {
                        AudioEmitter emitter = new AudioEmitter();
                        emitter.Forward = parent.Movement.Facing;
                        emitter.Position = parent.Movement.Position;
                        emitter.Up = Vector3.UnitZ;
                        emitter.Velocity = Vector3.Zero;

                        AudioListener listener = new AudioListener();
                        listener.Forward = InGame.inGame.Camera.ViewDir;
                        listener.Position = InGame.inGame.Camera.ActualFrom;
                        listener.Up = InGame.inGame.Camera.ViewUp;
                        listener.Velocity = Vector3.Zero;

#if NETFX_CORE
                        BokuGame.Audio.SoundBank.PlayCue(name);
#else
                        BokuGame.Audio.SoundBank.PlayCue(name, listener, emitter);
#endif
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
        }   // end of PlayCue()

        /// <summary>
        /// Create a list of one-shot controllers for a given animation.
        /// </summary>
        /// <param name="name">The name of the animation(s) we're looking for.</param>
        /// 
        /// <returns></returns>
        private List<SimpleController> GetControllerList(AnimationInstance animator, string name)
        {
            List<SimpleController> controllers = new List<SimpleController>();

            Animation anim = animator.FindAnimation(name);
            if (anim != null)
            {
                SimpleController ac = new SimpleController(animator, anim);
                ac.SetToEnd();
                ac.Loop = false;
                controllers.Add(ac);
                allOneShotControllers.Add(ac);
                ac.Weight = 0.0f;

                blendController.Add(ac);

                // Check if there are any additional animations for this event.
                int i = 2;
                do
                {
                    string fullName = name + i.ToString();
                    anim = animator.FindAnimation(fullName);

                    if (anim != null)
                    {
                        ac = new SimpleController(animator, anim);
                        ac.SetToEnd();
                        ac.Loop = false;
                        controllers.Add(ac);
                        allOneShotControllers.Add(ac);
                        ac.Weight = 0.0f;

                        blendController.Add(ac);
                    }
                    ++i;
                } while (anim != null);
            }

            return controllers;
        }   // end of GetControllerList()

        /// <summary>
        /// Calculates the new animation weight by lerping from the current value to the target value.
        /// 
        /// While the lerp produces smooth transitions it has the downside that when going
        /// to 0 it never quite gets there.  Kind of a Zeno's Paradox kind of thing.  For 
        /// perf reasons it would be nice to have the weightings actually go to 0 so these
        /// animations can be skipped in update.  So, in an attempt to fix this when an
        /// animation's target weight is 0 we will use a slightly negative number in the 
        /// lerp.  We can then clamp the resulting value to 0 when it goes negative.
        /// </summary>
        /// <param name="curWeight"></param>
        /// <param name="targetWeight"></param>
        /// <returns></returns>
        private float CalcWeight(float curWeight, float targetWeight)
        {
            float secs = Time.GameTimeFrameSeconds;
            float kSlightlyNegative = -0.01f;
            float result = 0.0f;
            float kBlendSpeed = 10.0f;

            // If we're paused, secs==0 and this causes issues, so use
            // the current frame rate for lerping the animation weights.
            if (secs == 0)
            {
                secs = 1.0f / Time.FrameRate;
            }

            float t = kBlendSpeed * secs;

            if (t >= 1.0)
            {
                result = targetWeight;
            }
            else
            {
                if (targetWeight == 0.0f)
                {
                    result = MyMath.Lerp(curWeight, kSlightlyNegative, t);
                    result = Math.Max(result, 0.0f);
                }
                else
                {
                    result = MyMath.Lerp(curWeight, targetWeight, t);
                }
            }

            return result;
        }   // end of CalcWeight()

        /// <summary>
        /// Stops any one-shot animation that is playing.
        /// </summary>
        private void StopAnyOneShotAnimations()
        {
            if (oneShotActive)
            {
                for (int i = 0; i < allOneShotControllers.Count; i++)
                {
                    SimpleController a = allOneShotControllers[i];
                    a.SetToEnd();
                }
                oneShotActive = false;
            }
        }   // end of StopAnyOneShotAnimation()

        #region Alex's Debug Section
        public static void AnimationDebug()
        {
#if ANIMATION_DEBUG
            {
                // Figure out which bot we're focused on.
                GameActor actor = null;
                if (CameraInfo.MergedFollowList.Count > 0)
                {
                    actor = CameraInfo.MergedFollowList[0];
                }
                else
                {
                    // No actor...
                    return;
                }
                AnimationSet anims = null;
                Keys key = KeyboardInput.GetKeyPressed();

                if (actor != null)
                {
                    anims = actor.AnimationSet;

                    if (key != Keys.None)
                    {
                        // Handle any input.

                        // Trigger a one-shot?
                        if (key >= Keys.A && key <= Keys.Z)
                        {
                            recentOneShot = (int)(key - Keys.A);
                            anims.StartOneShotAnimation(recentOneShot);
                        }
                        else
                        {
                            switch (key)
                            {
                                case Keys.Space:
                                    animMode = !animMode;
                                    if (animMode == false)
                                    {
                                        Time.Paused = false;
                                        Time.ClockRatio = 1.0f;
                                        InGame.inGame.Cursor3D.Hidden = false;
                                    }
                                    else
                                    {
                                        InGame.inGame.Cursor3D.Hidden = true;
                                    }
                                    break;
                                case Keys.D1:
                                    lockRotation = !lockRotation;
                                    break;
                                case Keys.D2:
                                    lockPosition = !lockPosition;
                                    break;
                                case Keys.Add:
                                    clockRatio += clockRatio >= 0.095f ? 0.1f : 0.01f;
                                    break;
                                case Keys.Subtract:
                                    if (clockRatio > 0.15f)
                                    {
                                        clockRatio -= 0.1f;
                                    }
                                    else if (clockRatio > 0.015f)
                                    {
                                        clockRatio -= 0.01f;
                                    }
                                    break;
                                case Keys.Enter:
                                    Time.Paused = !Time.Paused;
                                    if (Time.Paused)
                                    {
                                        // Align active animation with frame boundaries.
                                        // TODO (scoy) fill this in.
                                        if (anims.OneShotAnimationActive)
                                        {
                                            for (int i = 0; i < anims.AllOneShotControllers.Count; i++)
                                            {
                                                SimpleController ac = anims.AllOneShotControllers[i];
                                                ac.Align(ticksPerFrame);
                                            }
                                        }
                                    }
                                    break;
                                case Keys.Up:
                                    if (Time.Paused)
                                    {
                                        // Tick forward the active animstion.
                                        if (anims.OneShotAnimationActive)
                                        {
                                            if (recentOneShot != -1)
                                            {
                                                SimpleController ac = anims.AllOneShotControllers[recentOneShot];
                                                ac.Update(ticksPerFrame);
                                                long elapsed = ac.CurrentTicks + ticksPerFrame;
                                                // See if we've gone too far, if so, activate the idle animation.
                                                if (elapsed >= ac.Duration)
                                                {
                                                    ac.CurrentTicks = ac.Duration;
                                                    ac.Weight = 0.0f;
                                                    anims.OneShotAnimationActive = false;

                                                    ac = anims.IdleController;
                                                    ac.SetToBegin();
                                                    ac.Weight = 1.0f;
                                                }
                                                else
                                                {
                                                    ac.CurrentTicks = elapsed;
                                                }

                                            }
                                        }
                                        else
                                        {
                                            // Tick forward the idle animation.
                                            SimpleController ac = anims.IdleController;
                                            long elapsed = ac.CurrentTicks + ticksPerFrame;
                                            // See if we've gone too far, if so, activate the one-shot.
                                            if (elapsed >= ac.Duration)
                                            {
                                                ac.SetToBegin();
                                                if (recentOneShot != -1)
                                                {
                                                    ac.Weight = 0.0f;

                                                    ac = anims.AllOneShotControllers[recentOneShot];
                                                    ac.SetToBegin();
                                                    ac.Weight = 1.0f;
                                                    anims.OneShotAnimationActive = true;
                                                }
                                            }
                                            else
                                            {
                                                ac.CurrentTicks = elapsed;
                                            }
                                        }
                                        anims.BlendController.Update(0);
                                    }
                                    break;
                                case Keys.Down:
                                    if (Time.Paused)
                                    {
                                        // We can't go back a single frame by giving the clock a negative 
                                        // number without breaking all kinds of other stuff.  So, just tick
                                        // back the active animation.
                                        if (anims.OneShotAnimationActive)
                                        {
                                            if (recentOneShot != -1)
                                            {
                                                SimpleController ac = anims.AllOneShotControllers[recentOneShot];
                                                long elapsed = ac.CurrentTicks - ticksPerFrame;
                                                // See if we've gone too far, if so, activate the idle animation.
                                                if (elapsed < 0)
                                                {
                                                    ac.CurrentTicks = ac.Duration;
                                                    ac.Weight = 0.0f;
                                                    anims.OneShotAnimationActive = false;

                                                    ac = anims.IdleController;
                                                    ac.CurrentTicks = ac.Duration - ticksPerFrame;
                                                    ac.Weight = 1.0f;
                                                }
                                                else
                                                {
                                                    ac.CurrentTicks = elapsed;
                                                }

                                            }
                                        }
                                        else
                                        {
                                            // Tick back the idle animation.
                                            SimpleController ac = anims.IdleController;
                                            long elapsed = ac.CurrentTicks - ticksPerFrame;
                                            // See if we've gone too far, if so, activate the one-shot.
                                            if (elapsed < 0)
                                            {
                                                ac.CurrentTicks = ac.Duration;
                                                if (recentOneShot != -1)
                                                {
                                                    ac.Weight = 0.0f;

                                                    ac = anims.AllOneShotControllers[recentOneShot];
                                                    ac.CurrentTicks = ac.Duration - ticksPerFrame;
                                                    ac.Weight = 1.0f;
                                                    anims.OneShotAnimationActive = true;
                                                }
                                            }
                                            else
                                            {
                                                ac.CurrentTicks = elapsed;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                if (animMode)
                {
                    GraphicsDevice device = KoiLibrary.GraphicsDevice;
                    batch = KoiLibrary.SpriteBatch;

                    // Load resources if needed.
                    if (font == null)
                    {
                        font = KoiLibrary.LoadSpriteFont(@"Fonts\AnimDebug");
                    }

                    if (!Time.Paused)
                    {
                        Time.ClockRatio = clockRatio;
                    }

                    // Display overlay.
                    Color color = new Color(30, 30, 30);
                    Color title = new Color(220, 220, 200);
                    Color highlight = Color.Yellow;
                    textPosition = new Vector2(50, 50);

                    batch.Begin();

#if !ANIMATION_DEBUG_MINIMAL
                    Display(@"Alex's Animation Debug Mode", title);
                    textPosition.Y += 10;

                    Display(@"    1 : lock rotation -- " + lockRotation.ToString(), color);
                    Display(@"    2 : lock position -- " + lockPosition.ToString(), color);

                    textPosition.Y += 10;
#endif

                    InGame.inGame.shared.cursorPosition = lockedPosition;

                    if (actor != null)
                    {
                        if (lockPosition)
                        {
                            if (lockedPosition == Vector3.Zero)
                                lockedPosition = actor.Movement.Position;
                            else
                            {
                                Vector3 pos = lockedPosition;
                                pos.Z = actor.Movement.Position.Z;
                                actor.Movement.Position = pos;
                            }
                        }
                        if (lockRotation)
                        {
                            if (lockedRotation == 0.0f)
                                lockedRotation = actor.Movement.Rotation;
                            else
                                actor.Movement.Rotation = lockedRotation;
                        }

                        // Force off camera folling for this actor so the 
                        // camera is freed up to be controlled by the user.
                        //actor.Parameters.cameraMode = GameActor.CameraFollowModes.Never;


#if !ANIMATION_DEBUG_MINIMAL
                        // List this actor's animation set.
                        Display(@"Blended Animations", title);
                        Display(@"    idle      " + anims.IdleWeight.ToString("F2"), anims.IdleWeight > 0.0f ? highlight : color);
                        Display(@"    forward   " + anims.ForwardWeight.ToString("F2"), anims.ForwardWeight > 0.0f ? highlight : color);
                        Display(@"    backwards " + anims.BackwardsWeight.ToString("F2"), anims.BackwardsWeight > 0.0f ? highlight : color);
                        Display(@"    left      " + anims.LeftWeight.ToString("F2"), anims.LeftWeight > 0.0f ? highlight : color);
                        Display(@"    right     " + anims.RightWeight.ToString("F2"), anims.RightWeight > 0.0f ? highlight : color);

                        textPosition.Y += 10;
#endif

                        if (anims.AllOneShotControllers == null)
                        {
                            Display(@"No One-Shot Animations", title);
                        }
                        else
                        {
                            Display(@"One-Shot Animations", title);
                            int active = -1;
                            for (int i = 0; i < anims.AllOneShotControllers.Count; i++)
                            {
                                string str = @" " + (char)('A' + i) + @" : " + anims.AllOneShotControllers[i].AnimName;
                                bool on = anims.AllOneShotControllers[i].Weight > 0.0f;
                                Display(str, on ? highlight : color);
                                if (on)
                                {
                                    active = i;
                                }
                            }
                            Display("wind " + anims.backwardsAttenuation.ToString("F2"), highlight);

#if !ANIMATION_DEBUG_MINIMAL
                            //
                            // Clock/Frame info
                            //

                            textPosition = new Vector2(BokuGame.ScreenSize.X - 400, 50);
                            Display(@"Clock Ratio (numpad +-) : " + clockRatio.ToString("F2"), clockRatio == 1.0f ? color : highlight);
                            Display(@"Pause (numpad enter)", Time.Paused ? highlight : color);
                            if (active != -1)
                            {
                                SimpleController ac = anims.AllOneShotControllers[active];
                                double duration = ac.Duration / ticksPerSecond;
                                double elapsed = ac.CurrentTicks / ticksPerSecond;
                                int totalFrames = 1 + (int)(duration * 30.0);
                                int curFrame = 1 + (int)(elapsed * 30.0);
                                if (Time.Paused)
                                {
                                    Display(@"Frame (up / down) : " + curFrame.ToString() + @" / " + totalFrames.ToString(), highlight);
                                }
                                else
                                {
                                    Display(@"Frame : " + curFrame.ToString() + @" / " + totalFrames.ToString(), highlight);
                                }
                            }
                            else
                            {
                                Display(@"Frame : 0 / 0", color);
                            }
#endif
                        }

                        bool verbose = false;
                        if (verbose)
                        {
                            // Display current idle frame.
                            SimpleController ac = anims.IdleController;
                            double duration = ac.Duration / ticksPerSecond;
                            double elapsed = ac.CurrentTicks / ticksPerSecond;
                            int totalFrames = 1 + (int)(duration * 30.0);
                            int curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Idle frame      : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.IdleWeight > 0.1f ? highlight : color);

                            // Display current forward frame.
                            ac = anims.ForwardController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Forward frame   : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.ForwardWeight > 0.1f ? highlight : color);

                            // Display current backwards frame.
                            ac = anims.BackwardsController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Backwards frame : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.BackwardsWeight > 0.1f ? highlight : color);

                            // Display current left frame.
                            ac = anims.LeftController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Left frame      : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.LeftWeight > 0.1f ? highlight : color);

                            // Display current right frame.
                            ac = anims.RightController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Right frame     : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.RightWeight > 0.1f ? highlight : color);

                            // Display current inspect frame.
                            ac = anims.InspectController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Inspect frame     : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.InspectWeight > 0.1f ? highlight : color);

                            // Display current beam frame.
                            ac = anims.BeamController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Beam frame     : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.BeamWeight > 0.1f ? highlight : color);

                            // Display current scan frame.
                            ac = anims.ScanController;
                            duration = ac.Duration / ticksPerSecond;
                            elapsed = ac.CurrentTicks / ticksPerSecond;
                            totalFrames = 1 + (int)(duration * 30.0);
                            curFrame = 1 + (int)(elapsed * 30.0);
                            Display(@"Scan frame     : " + curFrame.ToString() + @" / " + totalFrames.ToString(), anims.ScanWeight > 0.1f ? highlight : color);


                        }
                    }
                    else
                    {
                        Display(@"No user-controlled actor.", highlight);
                    }

                    batch.End();
                }
            }
#endif  // ANIMATION_DEBUG
        }

#if ANIMATION_DEBUG
        static bool animMode = false;
        static bool lockPosition = true;
        static bool lockRotation = false;

        static Vector3 lockedPosition = Vector3.Zero;
        static float lockedRotation = 0.0f;

        static float clockRatio = 1.0f;
        static double ticksPerSecond = 10000000.0;
        static long ticksPerFrame = 333333;
        static int recentOneShot = -1;         // Index of most recent one shot animation.

        static SpriteFont font = null;
        static SpriteBatch batch = null;
        static Vector2 textPosition = new Vector2(50, 50);

        public static void Display(string str, Color color)
        {
            TextHelper.DrawString(Font, str, textPosition, color);
            textPosition.Y += Font().LineSpacing;
        }

#endif  // ANIMATION_DEBUG
        #endregion Alex's Debug Section


        #endregion

    }   // end of class AnimationSet

}   // end of namespace Boku.Common
