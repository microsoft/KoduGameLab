// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


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

namespace Boku.Base
{
    public interface ITransform
    {
        Transform Local
        {
            get;
            set;
        }
        Matrix World
        {
            get;
        }
        /// <summary>
        /// This must be called after finishing all calls on the ITransform.Local.  This rebuilds
        /// the local and world matrices.  The return value is true if the matrices have been
        /// rebuilt, false if they weren't dirty and didn't need rebuilding.
        /// </summary>
        bool Compose();

        /// <summary>
        /// Generally called by the parent object when its transform has changed.  This should
        /// trigger the recalc of the world matrix and then should be called on any children
        /// if needed.
        /// </summary>
        void Recalc( ref Matrix parentMatrix );

        /// <summary>
        /// This is the parent that the object will request an updated world matrix from
        /// </summary>
        ITransform Parent
        {
            get;
            set;
        }
    }

    public class Transform
    {
        /// <summary>
        /// Order to apply the rotation
        /// </summary>
        public enum RotationVectorOrder
        {
            ZYX,
            ZXY,
            XYZ,
            YXZ,
        };
        private static int[,] RotOrderLookup = 
        {
            {2,1,0}, // RotationVectorOrder.ZYX
            {2,0,1}, // RotationVectorOrder.ZXY
            {0,1,2}, // RotationVectorOrder.XYZ
            {1,0,2}, // RotationVectorOrder.YXZ
        };
        private bool dirty;
        private Matrix matrix;
        private Vector3 translation;
        private Vector3 scale;
        private Vector3 rotation;
        private RotationVectorOrder rotationOrder;
        private Vector3 translationOrigin;
        
        private bool opaque;

        public Transform()
        {
            Clear();
            this.dirty = false;
            this.opaque = false;
        }
        public Transform(Transform dupe)
        {
            this.matrix = dupe.matrix;
            this.translation = dupe.translation;
            this.scale = dupe.scale;
            this.rotation = dupe.rotation;
            this.rotationOrder = dupe.rotationOrder;
            this.translationOrigin = dupe.translationOrigin;
            this.dirty = true; // duplicating causes it to be dirty
            this.opaque = dupe.opaque;
        }
        public void Clear()
        {
            this.matrix = Matrix.Identity;
            this.translation = Vector3.Zero;
            this.scale = Vector3.One;
            this.rotation = Vector3.Zero;
//            this.rotationOrder = RotationVectorOrder.ZYX;
            this.translationOrigin = Vector3.Zero;
            this.dirty = true;
        }
        public void ForceDirty()
        {
            this.dirty = true;
        }

        public Matrix Matrix
        {
            get
            {
                if (!this.opaque)
                {
                    Compose();
                }
                return this.matrix;
            }
            set
            {
                this.opaque = true;
                this.matrix = value;
                this.dirty = true;
            }
        }
        public Vector3 Translation
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.translation;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.translation = value;
                this.dirty = true;
            }
        }
        public Vector3 OriginTranslation
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.translationOrigin;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.translationOrigin = value;
                this.dirty = true;
            }
        }
        public Vector3 Scale
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.scale;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.scale = value;
                this.dirty = true;
            }
        }
        public Vector3 Rotation
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.rotation;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.rotation = value;
                this.dirty = true;
            }
        }
        public float RotationX
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.rotation.X;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.rotation.X = value;
                this.dirty = true;
            }
        }
        public float RotationY
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.rotation.Y;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.rotation.Y = value;
                this.dirty = true;
            }
        }
        public float RotationZ
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.rotation.Z;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.rotation.Z = value;
                this.dirty = true;
            }
        }
        public RotationVectorOrder RotationOrder
        {
            get
            {
                Debug.Assert(!this.opaque);
                return this.rotationOrder;
            }
            set
            {
                Debug.Assert(!this.opaque);
                this.rotationOrder = value;
                this.dirty = true;
            }
        }

        public bool Compose()
        {
            // HACK:  temporary work around for problem where dirty is not staying set
            this.dirty = true;
            // HACK END

            bool composed = this.dirty;
            if (this.dirty)
            {
                this.dirty = false;
                if (!this.opaque)
                {
                    Matrix local = Matrix.Identity;
                    if (this.translationOrigin != Vector3.Zero)
                    {
                        local.Translation += this.translationOrigin;
                    }

                    for (int indexOrder = 0; indexOrder < 3; indexOrder++)
                    {
                        int indexRotation = RotOrderLookup[(int)this.rotationOrder, indexOrder];
                        switch (indexRotation)
                        {
                            case 2:
                                if (this.rotation.Z != 0.0)
                                {
                                    local *= Matrix.CreateRotationZ(this.rotation.Z);
                                }
                                break;
                            case 1:
                                if (this.rotation.Y != 0.0)
                                {
                                    local *= Matrix.CreateRotationY(this.rotation.Y);
                                }
                                break;
                            case 0:
                                if (this.rotation.X != 0.0)
                                {
                                    local *= Matrix.CreateRotationX(this.rotation.X);
                                }
                                break;
                        }
                    }

                    if (this.scale != Vector3.One)
                    {
                        local *= Matrix.CreateScale(this.scale);
                    }
                    if (this.translation != Vector3.Zero)
                    {
                        local.Translation += this.translation;
                    }
                    this.matrix = local;
                }
            }
            return composed;
        }
    }

    
}
