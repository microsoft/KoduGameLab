using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchHook
{
    public enum GestureState
    {
        Begin,
        Move,
        Inertia,
        End
    }

    public delegate int PanGestureEventHandler(object sender, PanGestureEventArgs e);

    public class PanGestureEventArgs : EventArgs
    {
        public POINT ptFirst;
        public POINT ptSecond;
        public int distance;
        public GestureState state;
    }

    public delegate int ZoomGestureEventHandler(object sender, ZoomGestureEventArgs e);

    public class ZoomGestureEventArgs : EventArgs
    {
        public POINT ptFirst;
        public POINT ptSecond;
        public int distance;
        public double zoom;
        public GestureState state;
    }

    public delegate int RotateGestureEventHandler(object sender, RotateGestureEventArgs e);

    public class RotateGestureEventArgs : EventArgs
    {
        public POINT ptCenter;
        public double initialRotation;
        public double rotation;
        public GestureState state;
    }

    public delegate int TwoFingerTapGestureEventHandler(object sender, TwoFingerTapGestureEventArgs e);

    public class TwoFingerTapGestureEventArgs : EventArgs
    {
        public POINT ptCenter;
        public int distance;
        public GestureState state;
    }

    public delegate int PressAndTapGestureEventHandler(object sender, PressAndTapGestureEventArgs e);

    public class PressAndTapGestureEventArgs : EventArgs
    {
        public POINT ptFirst;
        public POINT ptDelta;
        public GestureState state;
    }
}

