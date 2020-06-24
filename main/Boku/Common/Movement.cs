
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;

using KoiX;

using Boku.Base;
using Boku.SimWorld;
using Boku.SimWorld.Path;


namespace Boku.Common
{
    /// <summary>
    /// Class which owns the position, orientation, velocities, 
    /// desired velocities and orientations of objects.
    /// 
    /// Units are meters, radians, seconds.
    /// Rotations are around the Z axis.
    /// 
    /// </summary>
    public class Movement : ICloneable
    {
        #region Members

        // Useful for debugging.
        private GameActor parent;

        // Current dynamic state.
        private float rotationZRate = 0.0f;
        private Vector3 velocity = Vector3.Zero;

        // A hint from the brain telling us that this bot is under user control.
        private bool userControlled = false;    
        
        // Internal values.
        // Note that keeping rotationZ as a seperate value can lead to it being out of sync with localMatrix.
        // So, be very careful when setting either of these to ensure that they are always in sync.
        private Matrix localMatrix = Matrix.Identity;
        // In 0..2pi range.
        private float rotationZ = 0;

        // Previous state.
        private Vector3 prevPosition = Vector3.Zero;
        private Vector3 prevVelocity = Vector3.Zero;
        private Vector3 prevFacingDirection = Vector3.Zero;

        #endregion

        #region Accessors

        /// <summary>
        /// A hint from the brain telling us that this bot is under user control.
        /// </summary>
        public bool UserControlled
        {
            get { return userControlled; }
            set { userControlled = value; }
        }
        
        /// <summary>
        /// Current velocity in meters per second.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Magnitude of velocity.
        /// </summary>
        public float Speed
        {
            get { return velocity.Length(); }
        }
        
        /// <summary>
        /// Current rate of rotation in radius per second around Z axis.
        /// </summary>
        public float RotationZRate
        {
            get { return rotationZRate; }
            set { rotationZRate = value; }
        }
        
        /// <summary>
        /// Current rotation around Z axis.
        /// 0..2pi range.
        /// </summary>
        public float RotationZ
        {
            get { return rotationZ; }
            set 
            {
                if (rotationZ != value)
                {
                    float deltaZ = value - rotationZ;   
                    rotationZ = value;
                    rotationZ = MyMath.Modulo(rotationZ, MathHelper.TwoPi);

                    // Keep localMatrix in sync.  Need to rotate around local Z axis, not global.
                    Matrix rot = Matrix.CreateRotationZ(deltaZ);
                    Matrix loc = localMatrix;
                    loc.Translation = Vector3.Zero;
                    loc *= rot;
                    loc.Translation = localMatrix.Translation;
                    localMatrix = loc;
                }
            }
        }
        
        /// <summary>
        /// Current position in the world.
        /// </summary>
        public Vector3 Position
        {
            get { return localMatrix.Translation; }
            set { localMatrix.Translation = value; }
        }

        /// <summary>
        /// A shortcut to the Z element of the position.
        /// </summary>
        public float Altitude
        {
            get { return localMatrix.M43; }
            set { localMatrix.M43 = value; }
        }

        /// <summary>
        /// Transform matrix associated with current position and rotation.
        /// 
        /// Note that setting the localMatrix will also reset the value of rotationZ
        /// but there is some error accumulation which can happen causing a static
        /// object to rotate to a fixed orientation.  So, if you already know the
        /// Z roation because you just used it to create the local matrix you should
        /// use SetLocalMatrixAndRotation() instead and pass both values.
        /// </summary>
        public Matrix LocalMatrix
        {
            get { return localMatrix; }
            // Used if the chassis wants to explicitly create the local matrix.
            set 
            {
                localMatrix = value;

                // Need to extract Z rotation and force into 0..2pi range.
                /*
                rotationZ = (float)Math.Atan2(localMatrix.M12, localMatrix.M11);
                if (rotationZ < 0)
                {
                    rotationZ += MathHelper.TwoPi;
                }
                */

                Vector3 up = localMatrix.Backward;
                up.Normalize();
                if (up.Z == 1.0f)
                {
                    // No pitch or roll, easy case.
                    rotationZ = (float)Math.Atan2(localMatrix.M12, localMatrix.M11);
                    if (rotationZ < 0)
                    {
                        rotationZ += MathHelper.TwoPi;
                    }
                }
                else
                {
                    //Debug.Assert(false, "You should be using SetLocalMatrixAndRotation() (below) instead of forcing this to try and calc rotation.");

                    // Need to "undo" pitch and roll before extracting rotationZ.
                    Vector3 right = Vector3.Cross(Vector3.UnitZ, up);
                    right.Normalize();
                    float theta = (float)Math.Acos(up.Z);   // up.Z is same as dot(up, unitZ)
                    Matrix rot = Matrix.CreateFromAxisAngle(right, -theta);
                    Matrix local = localMatrix * rot;

                    // Ok, should be able to extract Z rotation now.
                    rotationZ = (float)Math.Atan2(local.M12, local.M11);
                    if (rotationZ < 0)
                    {
                        rotationZ += MathHelper.TwoPi;
                    }
                }
            }
        }

        /// <summary>
        /// Since we keep both a matrix and a rotation, this makes it easier
        /// to set both at the same time instead of setting one and then
        /// extracting the other.
        /// </summary>
        /// <param name="local"></param>
        /// <param name="rotationZ"></param>
        public void SetLocalMatrixAndRotation(Matrix local, float rotationZ)
        {
            this.localMatrix = local;
            this.rotationZ = rotationZ;
        }

        /// <summary>
        /// This is the vector along which a character moves "forward".  Most
        /// of the time this will be the same as Facing.  This will not be true
        /// for actors that spin like saucer or puck.
        /// </summary>
        public Vector3 Heading
        {
            get
            {
                Matrix rot = Matrix.CreateRotationZ(RotationZ);
                Vector3 heading = Vector3.TransformNormal(Vector3.UnitX, rot);
                return heading;
            }
        }

        /// <summary>
        /// Returns a vector aligned with the direction the actor is facing.
        /// Note that for some actors (puck, saucer) this may not be the same
        /// as the direction the actor is heading since they are constantly
        /// spinning.  For that vector, use Heading.
        /// </summary>
        public Vector3 Facing
        {
            get { return LocalMatrix.Right; }
        }

        /// <summary>
        /// Position at the beginning of the frame.
        /// </summary>
        public virtual Vector3 PrevPosition
        {
            get { return prevPosition; }
        }
        /// <summary>
        /// Velocity at the beginning of the frame.
        /// </summary>
        public virtual Vector3 PrevVelocity
        {
            get { return prevVelocity; }
        }
        /// <summary>
        /// Facing direciton at the beginning of the frame.
        /// </summary>
        public virtual Vector3 PrevFacingDirection
        {
            get { return prevFacingDirection; }
        }

        #endregion

        /// <summary>
        /// Movement c'tor
        /// </summary>
        public Movement(GameThing parent)
        {
            this.parent = parent as GameActor;
        }   // end of c'tor

        public Object Clone()
        {
            return MemberwiseClone();
        }

        public void CopyTo(Movement other)
        {
            other.rotationZRate = this.rotationZRate;
            other.velocity = this.velocity;
            other.userControlled = this.userControlled;
            other.rotationZ = this.rotationZ;
            other.localMatrix = this.localMatrix;
            other.prevPosition = this.prevPosition;
            other.prevVelocity = this.prevVelocity;
            other.prevFacingDirection = this.prevFacingDirection;
        }

        /// <summary>
        /// Initialize any members that can change at runtime back to their
        /// initial values. DO NOT ALLOCATE ANYTHING IN THIS FUNCTION. This
        /// function exists so that ActorFactory can reinitialize recycled
        /// actors. ActorFactory exists so that we do not need to allocate
        /// new memory when the need for a new actor arises. Therefore
        /// allocating memory in this function would defeat the entire
        /// reason for its existence.
        /// </summary>
        public void InitDefaults()
        {
            rotationZRate = 0;
            velocity = Vector3.Zero;
            userControlled = false;

            // Do not reset positional information so that we can revive knocked-out actors.
            //position = Vector3.Zero;
            //rotation = 0;
            //localMatrix = Matrix.Identity;

            prevPosition = localMatrix.Translation;
            prevVelocity = Vector3.Zero;
            prevFacingDirection = Facing;
        }

        /// <summary>
        /// Copies the current position, velocity and facing direction to the prev values.
        /// </summary>
        public virtual void SetPreviousPositionVelocity()
        {
            prevPosition = localMatrix.Translation;
            prevVelocity = velocity;
            prevFacingDirection = Facing;        // Note this is only valid for chassis
                                        // that have a facing direction.
        }   // end of Movement  ResetPreviousPositionVelocity()

    }   // end of class Movement

}   // end of namespace Boku.Common
