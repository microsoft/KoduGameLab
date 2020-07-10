// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Boku.Common.Gesture
{
    public enum TouchGestureType
    {
        //Discrete
        Tap,
        DoubleTap,
        TapHold,
        Swipe,

        //Continuous
        Drag,
        DoubleDrag,
        Pinch,
        Rotate,
        
        COUNT,
        INVALID, //INVALID MUST BE AFTER COUNT

        CONTINUOUS_START = Drag,
        CONTINUOUS_END = Rotate,
    }

    public class TouchGestureManager
    {

        //----------------------------------------------
        //SINGLETON PATTERN (Static Instance)
        private static readonly TouchGestureManager s_instance = new TouchGestureManager();
        public static TouchGestureManager Get()
        {
            return s_instance;
        }

        private TouchGestureManager()
        {
            m_gestures[(int)TouchGestureType.Tap] = m_tapGesture;
            m_gestures[(int)TouchGestureType.DoubleTap] = m_doubleTapGesture;
            m_gestures[(int)TouchGestureType.TapHold] = m_touchHoldGesture;
            
            m_gestures[(int)TouchGestureType.Drag] = m_dragGesture;
            m_gestures[(int)TouchGestureType.DoubleDrag] = m_doubleDragGesture;

            m_gestures[(int)TouchGestureType.Pinch] = m_pinchGesture;
            m_gestures[(int)TouchGestureType.Rotate] = m_rotateGesture;
            m_gestures[(int)TouchGestureType.Swipe] = m_swipeGesture;
            
        }
        //----------------------------------------------

        private GestureRecognizer[] m_gestures = new GestureRecognizer[(int)TouchGestureType.COUNT];
        private List<GestureRecognizer> m_ActiveGestureList = new List<GestureRecognizer>();

        private PinchGestureRecognizer m_pinchGesture = new PinchGestureRecognizer();
        public PinchGestureRecognizer PinchGesture
        {
            get { return m_pinchGesture; }
        }

        private RotationGestureRecognizer m_rotateGesture = new RotationGestureRecognizer();
        public RotationGestureRecognizer RotateGesture
        {
            get { return m_rotateGesture; }
        }

        private DoubleDragGestureRecognizer m_doubleDragGesture = new DoubleDragGestureRecognizer();
        public DoubleDragGestureRecognizer DoubleDragGesture
        {
            get { return m_doubleDragGesture; }
        }


        private DragGestureRecognizer m_dragGesture = new DragGestureRecognizer();
        public DragGestureRecognizer DragGesture
        {
            get { return m_dragGesture; }
        }

        private TapGestureRecognizer m_tapGesture = new TapGestureRecognizer();
        public TapGestureRecognizer TapGesture
        {
            get { return m_tapGesture; }
        }

        private DoubleTapGestureRecognizer m_doubleTapGesture = new DoubleTapGestureRecognizer();
        public DoubleTapGestureRecognizer DoubleTapGesture
        {
            get { return m_doubleTapGesture; }
        }

        private SwipeGestureRecognizer m_swipeGesture = new SwipeGestureRecognizer();
        public SwipeGestureRecognizer SwipeGesture
        {
            get { return m_swipeGesture; }
        }

        private TouchHoldGestureRecognizer m_touchHoldGesture = new TouchHoldGestureRecognizer();
        public TouchHoldGestureRecognizer TouchHoldGesture
        {
            get { return m_touchHoldGesture; }
        }


        public void Update()
        {
            TouchContact[] touches = TouchInput.Touches;

            for (int i = 0; i < m_gestures.Length; ++i)
            {
                m_gestures[i].Update(touches);
            }

            //---------------------------------------
            //Manage Active Gesture Queue.

            //Invalidate gestures in list
            for( int i=0; i<m_ActiveGestureList.Count; ++i )
            {
                if( !m_ActiveGestureList[i].IsValidated )
                {
                    m_ActiveGestureList.RemoveAt(i);
                    --i;
                }
            }

            //Look for a new valid gesture for the queue and place at end if valid.
            for (int i = (int)TouchGestureType.CONTINUOUS_START; i<=(int)TouchGestureType.CONTINUOUS_END; ++i)
            {
                Debug.Assert( i < m_gestures.Length );
                if( null != m_gestures[i] && m_gestures[i].IsValidated && !m_ActiveGestureList.Contains( m_gestures[i] ) )
                {
                    m_ActiveGestureList.Add(m_gestures[i]);
                }
            }
        }

        //This function will return the gesture if Active and ahead of the ifBeforeTheseTypes gestures in the active queue.
        public GestureRecognizer GetActiveGesture(TouchGestureType type, params TouchGestureType[] ifBeforeTheseTypes )
        {
            Debug.Assert(type < TouchGestureType.COUNT);
            
            GestureRecognizer retGesture = null;
            if( TouchGestureType.CONTINUOUS_START <= type  && type <= TouchGestureType.CONTINUOUS_END )
            {
                //Continuous Gestures need to be tested through the queue.
                for (int i = 0; i < m_ActiveGestureList.Count; ++i)
                {
                    TouchGestureType activeType = GetGestureType(m_ActiveGestureList[i]);

                    bool foundUnwanted = false;
                    for (int j = 0; !foundUnwanted && j < ifBeforeTheseTypes.Length; ++j)
                    {
                        foundUnwanted |= ifBeforeTheseTypes[j] == activeType;
                    }

                    if (foundUnwanted)
                    {
                        break;
                    }
                    else if (type == activeType)
                    {
                        retGesture = m_ActiveGestureList[i];
                        break;
                    }
                }
            }
            else
            {
                //User Requested a discrete gesture. Don't use the active gesture list.  Returning if active.
                retGesture = m_gestures[(int)type].IsValidated ? m_gestures[(int)type] : null;
            }
            return retGesture;
        }

        public TouchGestureType GetGestureType( GestureRecognizer gesture )
        {
            TouchGestureType retGestureType = TouchGestureType.INVALID;
            if (null != gesture)
            {
                for (int i = 0; i < m_gestures.Length; ++i)
                {
                    if (gesture == m_gestures[i])
                    {
                        retGestureType = (TouchGestureType)i;
                        Debug.Assert(retGestureType < TouchGestureType.COUNT);
                        break;
                    }
                }
            }
            return retGestureType;
        }
    }
}
