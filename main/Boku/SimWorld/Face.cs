using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;

namespace Boku.SimWorld
{
    abstract public class Face : INeedsDeviceReset
    {
        public delegate void FaceChangeEvent(FaceState newFace);

        public class FaceParams : BokuShared.XmlData<FaceParams>
        {
            #region Members
            private Vector2 pupilCenter = Vector2.Zero;
            private float pupilSize = 1.0f;
            private float lidDistance = 0.0f;
            private float eyeSpeed = 1.0f;
            private Vector2 blinkRange = new Vector2(3.0f, 5.0f);
            private float maxAsymmetry = 0.1f;
            private Vector3 backgroundColor = new Vector3(75.0f / 255.0f, 82.0f / 255.0f, 62.0f / 255.0f);

            private string pupilsName = @"Textures\FloatbotFaceEyePupil";
            private string pupilsCrossName = @"Textures\BokuFaceEyePupilCross";

            public enum FaceType
            {
                Boku,
                Wide,
                Two
            };
            private FaceType typeFace = FaceType.Boku;
            #endregion Members

            #region Accessors
            public Vector2 PupilCenter
            {
                get { return pupilCenter; }
                set { pupilCenter = value; }
            }
            public float PupilSize
            {
                get { return pupilSize; }
                set { pupilSize = value; }
            }
            public float LidDistance
            {
                get { return lidDistance; }
                set { lidDistance = value; }
            }
            public float EyeSpeed
            {
                get { return eyeSpeed; }
                set { eyeSpeed = value; }
            }
            public Vector2 BlinkRange
            {
                get { return blinkRange; }
                set { blinkRange = value; }
            }
            public float MaxAsymmetry
            {
                get { return maxAsymmetry; }
                set { maxAsymmetry = value; }
            }
            public Vector3 BackgroundColor
            {
                get { return backgroundColor; }
                set { backgroundColor = value; }
            }
            public string PupilsName
            {
                get { return pupilsName; }
                set { pupilsName = value; }
            }
            public string PupilsCrossName
            {
                get { return pupilsCrossName; }
                set { pupilsCrossName = value; }
            }

            public FaceType TypeFace
            {
                get { return typeFace; }
                set { typeFace = value; }
            }
            #endregion Accessors
        }

        // These may be too Boku-centric, but they might be
        // general enough for all our characters, if they have
        // a chance to pick out their own textures and mappings.
        // The real base interface here is SetupForRender (and FaceChange),
        // those are all the owning LRO really needs to talk to.

        /// <summary>
        /// Components of an expression
        /// </summary>
        public enum EyeShape
        {
            Open,
            Squint
        }

        public enum BrowPosition
        {
            Up,
            Down,
            Normal
        }

        public enum GazeState
        {
            Fixed,
            Scanning,   // default
            Directed
        }
        /// <summary>
        /// The underlying emotional state.
        /// </summary>
        public enum FaceState
        {
            None,       // default
            Happy,
            Sad,
            Mad,
            Crazy,
            Squint,
            Remember,
            Dead,
            NotApplicable,
        }

        #region Members
        // Parameters which control the facial expression.
        protected EyeShape eyeShapeLeft = EyeShape.Open;
        protected EyeShape eyeShapeRight = EyeShape.Open;
        protected BrowPosition browPositionLeft = BrowPosition.Normal;
        protected BrowPosition browPositionRight = BrowPosition.Normal;
        protected Vector2 pupilOffsetLeft = new Vector2();
        protected Vector2 pupilOffsetRight = new Vector2();
        protected float pupilSizeLeft = 1.0f;
        protected float pupilSizeRight = 1.0f;
        protected Vector2 browOffsetLeft = Vector2.Zero;
        protected Vector2 browOffsetRight = Vector2.Zero;
        protected float browSizeLeft = 1.0f;
        protected float browSizeRight = 1.0f;

        protected Vector2 pupilOffsetTargetLeft = new Vector2();
        protected Vector2 pupilOffsetTargetRight = new Vector2();
        protected double pupilOffsetTime = 0.0f;  // If past this time, pick new offsets for pupils.

        protected FaceState emotionalState = FaceState.None;

        private FaceState reactiveState = FaceState.None;
        private GazeState gazeState = GazeState.Scanning;
        private Vector3 gazeTarget = new Vector3();

        // Amound of time left for each of the current states.
        private float emotionalDuration = 0.0f;
        private float reactiveDuration = 0.0f;
        private float gazeDuration = 0.0f;

        private float defaultEmotionalDuration = 10.0f;
        private float defaultReactiveDuration = 1.0f;
        private const float defaultGazeDuration = 2.0f;

        private Vector4 backgroundColor = new Vector4(75.0f / 255.0f, 82.0f / 255.0f, 62.0f / 255.0f, 1.0f);
        private readonly GetModelInstance getBaseModel = null;

        private bool newState = true;

        private Vector2 pupilCenter = Vector2.Zero;
        private float pupilSize = 1.0f;
        private float lidDistance = 0.0f;
        private float eyeSpeed = 1.0f;
        private Vector2 blinkRange = new Vector2(3.0f, 5.0f);
        private float maxAsymmetry = 0.1f;

        private Vector2 browCenter = Vector2.Zero;
        private float browSize = 1.0f;

        private string pupilsName = @"Textures\BokuFaceEyePupil";
        private string pupilsCrossName = @"Textures\BokuFaceEyePupilCross";
        protected Texture2D faceEyesPupils = null;
        protected Texture2D faceEyesPupilsCross = null;

        #endregion Members

        #region Accessors
        protected FBXModel BaseModel
        {
            get { return getBaseModel(); }
        }

        protected Vector4 BackgroundColor
        {
            get { return backgroundColor; }
        }

        protected Vector2 PupilCenter
        {
            get { return pupilCenter; }
            set { pupilCenter = value; }
        }
        protected float PupilSize
        {
            get { return pupilSize; }
            set { pupilSize = value; }
        }
        protected float LidDistance
        {
            get { return lidDistance; }
            set { lidDistance = value; }
        }
        protected float EyeSpeed
        {
            get { return eyeSpeed; }
            set { eyeSpeed = value; }
        }
        protected Vector2 BlinkRange
        {
            get { return blinkRange; }
            set { blinkRange = value; }
        }
        protected float MaxAsymmetry
        {
            get { return maxAsymmetry; }
            set { maxAsymmetry = value; }
        }
        protected Vector2 BrowCenter
        {
            get { return browCenter; }
            set { browCenter = value; }
        }
        protected float BrowSize
        {
            get { return browSize; }
            set { browSize = value; }
        }
        public FaceState EmotionalState
        {
            get { return emotionalState; }
        }
        #endregion Accessors

        #region Public
        public Face(GetModelInstance getModel)
        {
            getBaseModel = getModel;
        }

        static public float DefaultGazeDuration
        {
            get { return defaultGazeDuration; }
        }

        //
        // Public methods which allow the setting of the face state.
        //
        /// <summary>
        /// Callback (to owner) when this face changes expression on its own.
        /// </summary>
        public event FaceChangeEvent FaceChange;

        /// <summary>
        /// Lower priority facial display.  Usually longer duration than DisplayReactiveState()
        /// </summary>
        public void DisplayEmotionalState(FaceState emotionalState)
        {
            DisplayEmotionalState(emotionalState, defaultEmotionalDuration);
        }   // end of Face DispalyEmotionalState()

        /// <summary>
        /// Lower priority facial display.  Usually longer duration than DisplayReactiveState()
        /// </summary>
        public void DisplayEmotionalState(FaceState emotionalState, float duration)
        {
            this.emotionalState = emotionalState;
            this.emotionalDuration = duration;
            newState = true;
        }   // end of Face DispalyEmotionalState()

        /// <summary>
        /// Higher priority facial display.  Usually shorter duration than DisplayEmotionalState()
        /// </summary>
        public void DisplayReactiveState(FaceState reactiveState)
        {
            DisplayReactiveState(reactiveState, defaultReactiveDuration);
        }   // end of Face DispalyReactiveState()

        /// <summary>
        /// Higher priority facial display.  Usually shorter duration than DisplayEmotionalState()
        /// </summary>
        public void DisplayReactiveState(FaceState reactiveState, float duration)
        {
            this.reactiveState = reactiveState;
            this.reactiveDuration = duration;
            newState = true;
        }   // end of Face DispalyReactiveState()

        public void DirectGaze(Vector3 target)
        {
            DirectGaze(target, defaultGazeDuration);
        }   // end of Face DirectGaze()

        public void DirectGaze(Vector3 target, float duration)
        {
            this.gazeTarget = target;
            this.gazeDuration = duration;
            this.gazeState = GazeState.Directed;
        }   // end of Face DirectGaze()

        public void Update(Movement movement)
        {
            float deltaTime = Time.GameTimeFrameSeconds;

            // Decrement the timers and check for any state changes.
            reactiveDuration -= deltaTime;
            emotionalDuration -= deltaTime;
            gazeDuration -= deltaTime;

            if (reactiveDuration < 0.0f && reactiveState != FaceState.None)
            {
                reactiveState = FaceState.None;
                newState = true;
            }

            if (emotionalDuration < 0.0f && emotionalState != FaceState.None)
            {
                emotionalState = FaceState.None;
                newState = true;
            }

            if (gazeDuration < 0.0f)
            {
                gazeState = GazeState.Scanning;
            }

            if (reactiveState != FaceState.None)
            {
                SetState(reactiveState);
            }
            else
            {
                SetState(emotionalState);
            }

            SetGaze(movement);
        }   // end of Face Update()

        public static Face MakeFace(GetModelInstance getModel, XmlGameActor xmlActor)
        {
            Face face = null;

            if (xmlActor != null)
            {
                FaceParams faceParams = xmlActor.FaceParams;
                if (faceParams != null)
                {
                    switch (faceParams.TypeFace)
                    {
                        case FaceParams.FaceType.Boku:
                            face = MakeBokuFace(getModel, faceParams);
                            break;
                        case FaceParams.FaceType.Wide:
                            face = MakeWideFace(getModel, faceParams);
                            break;
                        case FaceParams.FaceType.Two:
                            face = MakeTwoFace(getModel, faceParams);
                            break;
                        default:
                            Debug.Assert(false, "Unknown face type");
                            break;
                    }

                    face.PupilCenter = faceParams.PupilCenter;
                    face.PupilSize = faceParams.PupilSize;
                    face.LidDistance = faceParams.LidDistance;
                    face.EyeSpeed = faceParams.EyeSpeed;
                    face.BlinkRange = faceParams.BlinkRange;
                    face.MaxAsymmetry = faceParams.MaxAsymmetry;
                    face.backgroundColor = new Vector4(
                        faceParams.BackgroundColor,
                        1.0f);
                    face.pupilsName = faceParams.PupilsName;
                    face.pupilsCrossName = faceParams.PupilsCrossName;
                }
            }
            return face;
        }

        abstract public void SetupForRender(FBXModel model);

        #endregion Public

        #region Internal

        private static OneFace MakeBokuFace(GetModelInstance getModel, FaceParams faceParams)
        {
            OneFace face = new OneFace(getModel);

            return face;
        }

        private static WideFace MakeWideFace(GetModelInstance getModel, FaceParams faceParams)
        {
            WideFace face = new WideFace(getModel);

            return face;
        }

        private static TwoFace MakeTwoFace(GetModelInstance getModel, FaceParams faceParams)
        {
            TwoFace face = new TwoFace(getModel);

            return face;
        }

        /// <summary>
        /// Adjusts the gaze direction.
        /// </summary>
        private void SetGaze(Movement movement)
        {
            const float maxOffset = 0.15f;  // Max amount we can offset the pupil centers.

            // This shouldn't happen, but just to make sure. We do
            // pass in a null movement sometimes (from menu).
            if ((movement == null) && (gazeState == GazeState.Directed))
                gazeState = GazeState.Scanning;

            float tics = (float)Time.GameTimeTotalSeconds;
            switch (gazeState)
            {
                case GazeState.Directed:
                    {
                        Vector3 dir3d = gazeTarget - movement.Position;     // Direction we want to look.
                        Vector2 dir2d = new Vector2(dir3d.X, dir3d.Y);      // 2d version 
                        dir2d.Normalize();
                        // Get targetAngle in +- PI range
                        float targetAngle = dir2d.Y < 0.0f ? -(float)Math.Acos(dir2d.X) : (float)(Math.Acos(dir2d.X));
                        // If the target is directly on or below Boku, look straight ahead.
                        if (float.IsNaN(targetAngle))
                        {
                            targetAngle = movement.RotationZ;
                        }
                        float viewAngle = movement.RotationZ;
                        if (viewAngle > (float)Math.PI)
                        {
                            viewAngle = viewAngle - MathHelper.TwoPi;
                        }
                        float deltaAngle = viewAngle - targetAngle;

                        // Make sure delat angle is in +- PI range
                        if (deltaAngle > (float)Math.PI)
                        {
                            deltaAngle -= MathHelper.TwoPi;
                        }
                        else if (deltaAngle < -(float)Math.PI)
                        {
                            deltaAngle += MathHelper.TwoPi;
                        }

                        // Our FOV is about +- 0.5 radians.  Map this onto maxOffset for the pupils.
                        // The fudge factor is just to make things look right.
                        deltaAngle = MathHelper.Clamp(deltaAngle * 0.6f, -1.0f, 1.0f);

                        // Now handle the vertical offset.
                        dir3d.Normalize();
                        float verticalOffset = 4.0f * MathHelper.Clamp(dir3d.Z, -maxOffset, maxOffset);

                        Vector2 pupilOffsetTargetLeft = maxOffset * new Vector2(deltaAngle, verticalOffset);
                        Vector2 pupilOffsetTargetRight = pupilOffsetTargetLeft;

                        pupilOffsetLeft = Vector2.Lerp(pupilOffsetLeft, pupilOffsetTargetLeft, 0.05f * EyeSpeed);
                        pupilOffsetRight = Vector2.Lerp(pupilOffsetRight, pupilOffsetTargetRight, 0.05f * EyeSpeed);
                    }
                    break;
                case GazeState.Scanning:
                    {
                        if (Time.GameTimeTotalSeconds > pupilOffsetTime)
                        {
                            // Get new timeout value.
                            pupilOffsetTime = 0.3 + Time.GameTimeTotalSeconds + 2.0f * BokuGame.bokuGame.rnd.NextDouble();

                            // Get new target position.
                            pupilOffsetTargetLeft = new Vector2(maxOffset * (float)(BokuGame.bokuGame.rnd.NextDouble() - BokuGame.bokuGame.rnd.NextDouble()),
                                                                maxOffset * (float)(BokuGame.bokuGame.rnd.NextDouble() - BokuGame.bokuGame.rnd.NextDouble()));
                            pupilOffsetTargetRight = pupilOffsetTargetLeft;
                        }
                        pupilOffsetLeft = Vector2.Lerp(pupilOffsetLeft, pupilOffsetTargetLeft, 0.05f * EyeSpeed);
                        pupilOffsetRight = Vector2.Lerp(pupilOffsetRight, pupilOffsetTargetRight, 0.05f * EyeSpeed);
                    }
                    break;
                case GazeState.Fixed:
                    // Used internally.  Assumes that SetState() has moved/sized
                    // the pupils and doesn't want them move again for some time.
                    break;
            }
        }   // end of Face SetGaze()

        public virtual void LoadContent(bool immediate) 
        {
            if (faceEyesPupils == null)
            {
                faceEyesPupils = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + pupilsName);
            }
            if (faceEyesPupilsCross == null)
            {
                faceEyesPupilsCross = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + pupilsCrossName);
            }
        }

        public virtual void InitDeviceResources(GraphicsDevice device)
        {
        }

        public virtual void UnloadContent() 
        {
            faceEyesPupils = null;
            faceEyesPupilsCross = null;
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        /// <summary>
        /// Takes the current state and set the parameters to control the facial features.
        /// </summary>
        protected virtual void SetState(FaceState state)
        {
            float pupilSizeLeftTarget = 1.0f;
            float pupilSizeRightTarget = 1.0f;
            int milliseconds = (int)(Time.GameTimeTotalSeconds * 1000.0);

            switch (state)
            {
                case FaceState.Crazy:
                    eyeShapeLeft = EyeShape.Open;
                    eyeShapeRight = EyeShape.Squint;
                    pupilSizeLeftTarget = 0.3f;
                    pupilSizeRightTarget = 1.2f;
                    browPositionLeft = BrowPosition.Up;
                    browPositionRight = BrowPosition.Down;
                    int brow = ((int)(Time.GameTimeTotalSeconds * 1000.0)) % 400 / 100;
                    switch (brow)
                    {
                        case 0:
                            browPositionRight = BrowPosition.Normal;
                            break;
                        case 1:
                            browPositionRight = BrowPosition.Down;
                            break;
                        case 2:
                            browPositionRight = BrowPosition.Normal;
                            break;
                        case 3:
                            browPositionRight = BrowPosition.Up;
                            break;
                    }
                    break;

                case FaceState.Happy:
                    eyeShapeLeft = eyeShapeRight = EyeShape.Open;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 1.2f;
                    // Wiggle the brows.
                    if ((milliseconds % 200) > 100)
                    {
                        browPositionLeft = BrowPosition.Normal;
                    }
                    else
                    {
                        browPositionLeft = BrowPosition.Up;
                    }
                    if ((milliseconds % 200) > 120)
                    {
                        browPositionRight = BrowPosition.Normal;
                    }
                    else
                    {
                        browPositionRight = BrowPosition.Up;
                    }
                    break;

                case FaceState.Mad:
                    eyeShapeLeft = eyeShapeRight = EyeShape.Squint;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 0.4f;
                    browPositionLeft = browPositionRight = BrowPosition.Down;
                    break;

                case FaceState.Sad:
                    eyeShapeLeft = eyeShapeRight = EyeShape.Open;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 1.6f;
                    browPositionLeft = browPositionRight = BrowPosition.Up;
                    break;

                case FaceState.Remember:
                    // Boku looks up and to the left like he's remembering.
                    eyeShapeLeft = eyeShapeRight = EyeShape.Open;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 0.9f;
                    browPositionLeft = browPositionRight = BrowPosition.Up;
                    gazeState = GazeState.Fixed;
                    gazeDuration = 0.01f;
                    pupilOffsetLeft = new Vector2(-0.10f, 0.15f);
                    pupilOffsetRight = pupilOffsetLeft;
                    break;

                case FaceState.Squint:
                    eyeShapeLeft = eyeShapeRight = EyeShape.Squint;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 0.9f;
                    browPositionLeft = browPositionRight = BrowPosition.Up;
                    break;

                case FaceState.Dead:
                    eyeShapeLeft = EyeShape.Open;
                    eyeShapeRight = EyeShape.Squint;
                    pupilSizeLeftTarget = 0.5f;
                    pupilSizeRightTarget = 0.5f;
                    browPositionLeft = BrowPosition.Normal;
                    browPositionRight = BrowPosition.Normal;
                    pupilOffsetLeft = pupilOffsetRight = new Vector2();
                    gazeState = GazeState.Fixed;
                    gazeDuration = float.MaxValue;
                    break;

                default:
                    eyeShapeLeft = eyeShapeRight = EyeShape.Open;
                    pupilSizeLeftTarget = pupilSizeRightTarget = 1.0f;
                    browPositionLeft = browPositionRight = BrowPosition.Normal;
                    break;
            }

            // fire the change event so others can react
            if (newState && FaceChange != null)
            {
                FaceChange(state);
            }

            // A change of state may change the size of the pupils.  If so, 
            // launch a twitch to smoothly change the size.
            if (newState)
            {
                newState = false;
                if (pupilSizeLeft != pupilSizeLeftTarget)
                {
                    TwitchManager.Set<float> set = delegate(float val, Object param) { pupilSizeLeft = val; };
                    TwitchManager.CreateTwitch<float>(pupilSizeLeft, pupilSizeLeftTarget, set, 0.2f, TwitchCurve.Shape.EaseInOut, null, null, true);
                }
                if (pupilSizeRight != pupilSizeRightTarget)
                {
                    TwitchManager.Set<float> set = delegate(float val, Object param) { pupilSizeRight = val; };
                    TwitchManager.CreateTwitch<float>(pupilSizeRight, pupilSizeRightTarget, set, 0.2f, TwitchCurve.Shape.EaseInOut, null, null, true);
                }
            }
        }   // end of Face SetState()


        #endregion Internal
    }
}
