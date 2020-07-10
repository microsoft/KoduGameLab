// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace TouchHook
{
    class WM_GestureHook : WindowsHook
    {
        public PanGestureEventHandler PanEventHandler = null;
        public ZoomGestureEventHandler ZoomEventHandler = null;
        public RotateGestureEventHandler RotateEventHandler = null;
        public TwoFingerTapGestureEventHandler TwoFingerTapEventHandler = null;

        private readonly int mdGestureConfigSize;
        private readonly int mdGestureInfoSize;

        private struct GestureUpdate
        {
            public Win32.GESTURECONFIG config;
            public bool dirty;
        }

        // Helper struct for remembering last gesture state
        private struct LastGestureInfo
        {
            public POINT ptFirst;
            public POINT ptSecond;
            public int argument;
        }
        private LastGestureInfo mPanInfo;
        private LastGestureInfo mZoomInfo;

        private GestureUpdate mAllGestures;
        private GestureUpdate mZoom;
        private GestureUpdate mPan;
        private GestureUpdate mRotate;
        private GestureUpdate mTwoFingerTap;
        private GestureUpdate mPressAndTap;
        private int mdNumConfigs = 5; // Does not include all gestures since it is mutually exclusive

        public WM_GestureHook(IntPtr hWnd)
            : base(hWnd, HookType.WH_GETMESSAGE)
        {
            HookInvoked += new HookEventHandler(GestureHookInvoked);

            mdGestureConfigSize = Marshal.SizeOf(new Win32.GESTURECONFIG());
            mdGestureInfoSize = Marshal.SizeOf(new Win32.GESTUREINFO());

            // Configure to accept all gestures initially.
            mAllGestures.config.dwID = 0;
            mAllGestures.config.dwWant = Win32.GC_ALLGESTURES;
            mAllGestures.config.dwBlock = 0;

            mZoom.config.dwID = Win32.GID_ZOOM;
            mPan.config.dwID = Win32.GID_PAN;
            mRotate.config.dwID = Win32.GID_ROTATE;
            mTwoFingerTap.config.dwID = Win32.GID_TWOFINGERTAP;
            mPressAndTap.config.dwID = Win32.GID_PRESSANDTAP;

            ClearGestureConfigs();
        }

        private void ClearGestureConfigs()
        {
            mZoom.config.dwWant = 0;
            mZoom.config.dwBlock = 0;
            mZoom.dirty = false;
            mPan.config.dwWant = 0;
            mPan.config.dwBlock = 0;
            mPan.dirty = false;
            mRotate.config.dwWant = 0;
            mRotate.config.dwBlock = 0;
            mRotate.dirty = false;
            mTwoFingerTap.config.dwWant = 0;
            mTwoFingerTap.config.dwBlock = 0;
            mTwoFingerTap.dirty = false;
            mPressAndTap.config.dwWant = 0;
            mPressAndTap.config.dwBlock = 0;
            mPressAndTap.dirty = false;
        }

        public int GestureHookInvoked(object sender, HookEventArgs e)
        {
            switch (e.message.msg)
            {
                case Win32.WM_GESTURENOTIFY:
                    {
                        ConfigureGesturesForSubmission();
                    }
                    break;
                case Win32.WM_GESTURE:
                    {
                        DecodeGesture(ref e.message);
                    }
                    break;
                default:
                    break;
            }
            return 0;
        }

        private void DecodeGesture(ref Message message)
        {
            Win32.GESTUREINFO gestureInfo = new Win32.GESTUREINFO();

            gestureInfo.cbSize = mdGestureInfoSize;
            if (!Win32.GetGestureInfo(message.lparam, ref gestureInfo))
            {
                return;
            }

            switch (gestureInfo.dwID)
            {
                case Win32.GID_BEGIN:
                case Win32.GID_END:
                    Win32.DefWindowProc(this.hWnd, message.msg, message.lparam, message.wparam);
                    break;
                case Win32.GID_PAN:
                    {
                        DecodePanGesture(ref gestureInfo);
                    }
                    break;
                case Win32.GID_ZOOM:
                    {
                        DecodeZoomGesture(ref gestureInfo);
                    }
                    break;
                case Win32.GID_ROTATE:
                    {
                        DecodeRotateGesture(ref gestureInfo);
                    }
                    break;
                case Win32.GID_TWOFINGERTAP:
                    {
                        DecodeTwoFingerTapGesture(ref gestureInfo);
                    }
                    break;
                case Win32.GID_PRESSANDTAP:
                    {
                        DecodePressAndTapGesture(ref gestureInfo);
                    }
                    break;
                default:
                    break;
            }
            // Close the gesture info handle to avoid leaking memory.
            Win32.CloseGestureInfoHandle(message.lparam);
        }

        private void DecodePressAndTapGesture(ref Win32.GESTUREINFO gestureInfo)
        {
            PressAndTapGestureEventArgs args = new PressAndTapGestureEventArgs();
            switch (gestureInfo.dwFlags)
            {
                case Win32.GF_BEGIN:
                    {
                        args.state = GestureState.Begin;
                    }
                    break;
                case Win32.GF_END:
                    {
                        args.state = GestureState.End;
                    }
                    break;
            }
            // Nothing is being done here with the ULLArguments or the gestureInfo.ptsLocation, since Microsoft's
            // Documentation seems to be innacurate. The docs suggest that the ptsLocation contains the
            // position of the first finger, and the delta between it and the second( tapped ) finger is stored in
            // the ULLArgument... however it specifically says that it is stored as a POINT structure in the 
            // lower 32 bits... but a points structure is 2 ints. So either their documents are wrong or they packed
            // it as two shorts knowing that delta screen space coords wouldn't blow that limit. We can find this out
            // with some testing
        }

        private void DecodeTwoFingerTapGesture(ref Win32.GESTUREINFO gestureInfo)
        {
            TwoFingerTapGestureEventArgs args = new TwoFingerTapGestureEventArgs();
            switch( gestureInfo.dwFlags)
            {
                case Win32.GF_BEGIN:
                    {
                        args.state = GestureState.Begin;
                    }
                    break;
                case Win32.GF_END:
                    {
                        args.state = GestureState.End;
                    }
                    break;
            }
            args.distance = (int)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            args.ptCenter.X = gestureInfo.ptsLocation.X;
            args.ptCenter.Y = gestureInfo.ptsLocation.Y;

            if (TwoFingerTapEventHandler != null)
            {
                TwoFingerTapEventHandler(this, args);
            }
        }

        private void DecodeRotateGesture(ref Win32.GESTUREINFO gestureInfo)
        {
            RotateGestureEventArgs args = new RotateGestureEventArgs();
            if (gestureInfo.dwFlags == Win32.GF_BEGIN)
            {
                args.state = GestureState.Begin;
                args.initialRotation = Win32.ArgToRadians(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
                args.rotation = 0;
            }
            else 
            {
                if (gestureInfo.dwFlags == Win32.GF_END)
                {
                    args.state = GestureState.End;
                }
                else
                {
                    args.state = GestureState.Move;
                }
                args.rotation = Win32.ArgToRadians(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            }
            args.ptCenter.X = gestureInfo.ptsLocation.X;
            args.ptCenter.Y = gestureInfo.ptsLocation.Y;

            if (RotateEventHandler != null)
            {
                RotateEventHandler(this, args);
            }
        }

        private void DecodeZoomGesture(ref Win32.GESTUREINFO gestureInfo)
        {
            ZoomGestureEventArgs args = new ZoomGestureEventArgs();
            switch (gestureInfo.dwFlags)
            {
                case Win32.GF_BEGIN:
                    {
                        args.state = GestureState.Begin;
                        mZoomInfo.ptFirst.X = gestureInfo.ptsLocation.X;
                        mZoomInfo.ptFirst.Y = gestureInfo.ptsLocation.Y;
                    }
                    break;
                default:
                    {
                        if (args.state == GestureState.End)
                        {
                            args.state = GestureState.End;
                        }
                        else
                        {
                            args.state = GestureState.Move;
                        }
                        // Read the second point of the gesture, this is the middle point between the fingers
                        mZoomInfo.ptSecond.X = gestureInfo.ptsLocation.X;
                        mZoomInfo.ptSecond.Y = gestureInfo.ptsLocation.Y;
                        // Calculate the zoom center point
                        POINT ptZoomCenter = new POINT((mZoomInfo.ptFirst.X + mZoomInfo.ptSecond.X) / 2,
                                                        (mZoomInfo.ptFirst.Y + mZoomInfo.ptSecond.Y) / 2);
                        args.zoom = (double)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK) /
                                    (double)(mZoomInfo.argument);
                    }
                    break;
            }

            args.distance = (int)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            args.ptFirst.X = mZoomInfo.ptFirst.X;
            args.ptFirst.Y = mZoomInfo.ptFirst.Y;
            args.ptSecond.X = mZoomInfo.ptSecond.X;
            args.ptSecond.Y = mZoomInfo.ptSecond.Y;
            if (ZoomEventHandler != null)
            {
                ZoomEventHandler(this, args);
            }

            // Store the new information as a starting point for the next step in the gesture
            mZoomInfo.argument = (int)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            if (gestureInfo.dwFlags != Win32.GF_BEGIN)
            {
                mZoomInfo.ptFirst.X = mZoomInfo.ptSecond.X;
                mZoomInfo.ptFirst.Y = mZoomInfo.ptSecond.Y;
            }
        }

        private void DecodePanGesture(ref Win32.GESTUREINFO gestureInfo)
        {
            PanGestureEventArgs args = new PanGestureEventArgs();
            switch (gestureInfo.dwFlags)
            {
                case Win32.GF_BEGIN:
                    {
                        args.state = GestureState.Begin;
                        mPanInfo.ptFirst.X = gestureInfo.ptsLocation.X;
                        mPanInfo.ptFirst.Y = gestureInfo.ptsLocation.Y;
                        mPanInfo.ptSecond.X = 0;
                        mPanInfo.ptSecond.Y = 0;
                    }
                    break;
                case Win32.GF_END:
                    {
                        args.state = GestureState.End;
                        mPanInfo.ptSecond.X = gestureInfo.ptsLocation.X;
                        mPanInfo.ptSecond.Y = gestureInfo.ptsLocation.Y;
                    }
                    break;
                case Win32.GF_INERTIA:
                    {
                        args.state = GestureState.Inertia;
                        mPanInfo.ptSecond.X = gestureInfo.ptsLocation.X;
                        mPanInfo.ptSecond.Y = gestureInfo.ptsLocation.Y;
                    }
                    break;
                default: // Pan Move
                    {
                        args.state = GestureState.Move;
                        mPanInfo.ptSecond.X = gestureInfo.ptsLocation.X;
                        mPanInfo.ptSecond.Y = gestureInfo.ptsLocation.Y;
                    }
                    break;
            }

            // Copy the pan coordinates into the event args and fire the event
            args.distance = (int)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            args.ptFirst.X = mPanInfo.ptFirst.X;
            args.ptFirst.Y = mPanInfo.ptFirst.Y;
            args.ptSecond.X = mPanInfo.ptSecond.X;
            args.ptSecond.Y = mPanInfo.ptSecond.Y;
            if (PanEventHandler != null)
            {
                PanEventHandler(this, args);
            }

            mPanInfo.argument = (int)(gestureInfo.ullArguments & Win32.ULL_ARGUMENTS_BIT_MASK);
            if (gestureInfo.dwFlags != Win32.GF_BEGIN)
            {
                // Set the first point to the second point, so we can have it as first next time
                mPanInfo.ptFirst.X = mPanInfo.ptSecond.X;
                mPanInfo.ptFirst.Y = mPanInfo.ptSecond.Y;
            }
        }

        private void ConfigureGesturesForSubmission()
        {
            // Here is where gesture support is configured.
            int count = 0;
            Win32.GESTURECONFIG[] configs;
            if (mAllGestures.dirty)
            {
                count = 1;
                configs = new Win32.GESTURECONFIG[count];
                configs[0] = mAllGestures.config;
            }
            else
            {
                // Go through the gesture configs and count up how many have changed
                if (mPan.dirty)
                {
                    count++;
                }
                if (mZoom.dirty)
                {
                    count++;
                }
                if (mRotate.dirty)
                {
                    count++;
                }
                if (mPressAndTap.dirty)
                {
                    count++;
                }
                if (mTwoFingerTap.dirty)
                {
                    count++;
                }

                // Allocate the array and go through the gesture configs again to assign them
                configs = new Win32.GESTURECONFIG[count];
                int i = 0;
                if (mPan.dirty)
                {
                    configs[i++] = mPan.config;
                }
                if (mZoom.dirty)
                {
                    configs[i++] = mZoom.config;
                }
                if (mRotate.dirty)
                {
                    configs[i++] = mRotate.config;
                }
                if (mPressAndTap.dirty)
                {
                    configs[i++] = mPressAndTap.config;
                }
                if (mTwoFingerTap.dirty)
                {
                    configs[i++] = mTwoFingerTap.config;
                }
            }

            // Set the gesture configs
            bool result = Win32.SetGestureConfig(this.hWnd, 0, count, ref configs, mdGestureConfigSize);
            if (!result)
            {
                throw new Exception("Error in execution of SetGestureConfig");
            }
        }

        /// <summary>
        /// This implicitly sets a gesture to be allowed when a GESTURENOTIFY message is hooked
        /// NOTE! Passing GestureId.All clears ALL the flags from ALL other gestures
        /// </summary>
        /// <param name="gesture">Gesture to allow, setting GestureId.All turns on all gestures</param>
        public void ConfigureGesture( GestureId gesture )
        {
            ConfigureGesture(gesture, true);
        }

        /// <summary>
        /// Explicitly set a gesture to be allowed or blocked. The gesture is not configured until a 
        /// GESTURENOTIFY message is hooked
        /// NOTE! Passing GestureId.All clears ALL the flags from ALL other gestures
        /// </summary>
        /// <param name="gesture">Gesture to either allow or block, use GestureId.All to configure all gestures</param>
        /// <param name="allow">if true, gesture is added to allow flags. if false it is removed from allow flags
        /// and added to the block flags</param>
        public void ConfigureGesture(GestureId gesture, bool allow)
        {
            switch (gesture)
            {
                case GestureId.All:
                {
                    SetFlag(ref mAllGestures.config, Win32.GC_ALLGESTURES, allow);
                }
                break;
                case GestureId.Zoom:
                {
                    SetFlag(ref mZoom.config, Win32.GC_ZOOM, allow);
                    mZoom.dirty = true;
                }
                break;
                case GestureId.Pan:
                {
                    SetFlag(ref mPan.config, Win32.GC_PAN, allow);
                    mPan.dirty = true;
                }
                break;
                case GestureId.Pan_Single_Hor:
                {
                    SetFlag(ref mPan.config, Win32.GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY, allow);
                    mPan.dirty = true;
                }
                break;
                case GestureId.Pan_Single_Vert:
                {
                    SetFlag(ref mPan.config, Win32.GC_PAN_WITH_SINGLE_FINGER_VERTICALLY, allow);
                    mPan.dirty = true;
                }
                break;
                case GestureId.Pan_With_Gutter:
                {
                    SetFlag(ref mPan.config, Win32.GC_PAN_WITH_GUTTER, allow);
                    mPan.dirty = true;
                }
                break;
                case GestureId.Pan_With_Inertia:
                {
                    SetFlag(ref mPan.config, Win32.GC_PAN_WITH_INERTIA, allow);
                    mPan.dirty = true;
                }
                break;
                case GestureId.PressAndTap:
                {
                    SetFlag(ref mPressAndTap.config, Win32.GC_PRESSANDTAP, allow);
                    mPressAndTap.dirty = true;
                }
                break;
                case GestureId.Rotate:
                {
                    SetFlag(ref mRotate.config, Win32.GC_ROTATE, allow);
                    mRotate.dirty = true;
                }
                break;
                case GestureId.TwoFingerTap:
                {
                    SetFlag(ref mTwoFingerTap.config, Win32.GC_TWOFINGERTAP, allow);
                    mTwoFingerTap.dirty = true;
                }
                break;
                default:
                break;
            }

            // If setting all gestures, clear everything else
            if (gesture == GestureId.All)
            {
                mAllGestures.dirty = true;
                ClearGestureConfigs();
            }
            else
            {
                mAllGestures.dirty = false;
            }
        }

        private void SetFlag(ref Win32.GESTURECONFIG config, int flag, bool on)
        {
            if (on)
            {
                config.dwWant |= flag;
                config.dwBlock &= ~flag;
            }
            else
            {
                config.dwWant &= ~flag;
                config.dwBlock |= flag;
            }
        }
    }

    public enum GestureId
    {
        All,
        Zoom,
        Pan,
        Pan_Single_Vert,
        Pan_Single_Hor,
        Pan_With_Gutter,
        Pan_With_Inertia,
        Rotate,
        TwoFingerTap,
        PressAndTap
    }
}
