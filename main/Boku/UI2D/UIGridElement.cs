// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using KoiX;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.Fx;

namespace Boku.UI2D
{
    /// <summary>
    /// The basic, renderable element that is used by the UIGrid.  This 
    /// object is assumed to be centered at 0,0,0.
    /// </summary>
    public abstract class UIGridElement : ArbitraryComparable, INeedsDeviceReset
    {
        /// <summary>
        /// This is a helper class that can be used when creating several UIGrid*Element objects.
        /// By using the ParamBlob class you can fill it out once and then use the short-form c'tor.
        /// </summary>
        public class ParamBlob
        {
            public float width;
            public float height;
            public float edgeSize;

            public Color unselectedColor;               // Colors that show through where the texture is transparent.
            public Color selectedColor;
            public float alpha = 1.0f;
            public bool altShader = false;              // Treats the texture as a decal overlay which is fully lit.
            public bool altPreMultAlphaShader = false;
            public bool greyFlatShader = false;         // Uses a "float grey" value to lerp between being fully colored and greey scale.
                                                        // Doesn't do any lighting.  Uses the normal map's alpha for a mask and ignores the rest.

            public GetFont Font = null;     // Delegate which will return the font to use for this UI element.  This font returned
                                                        // by this should not be held onto since a device reset may change it.

            public Color textColor;
            public Color selectedTextColor;
            public Color unselectedTextColor;
            public Color dropShadowColor;
            public bool useDropShadow = false;
            public bool invertDropShadow = false;   // Puts the drop shadow above the regular letter instead of below.
            public TextHelper.Justification justify = TextHelper.Justification.Center;
            public bool ignorePowerOf2 = false;     // Don't do any fixup to cards that require power of 2 textures.

            public string normalMapName = null;

            public string elementName = null;
        }   // end of class ParamBlob

        #region Members

        protected Matrix localMatrix = Matrix.Identity;
        protected Matrix worldMatrix = Matrix.Identity;
        protected Matrix invWorldMatrix = Matrix.Identity;

        public Vector3 position = Vector3.Zero;
        protected float scale = 1.0f;
        private float targetScale = 1.0f;   // The value toward which the scale is twitching.
        protected Vector3 rotation = Vector3.Zero;

        public Point gridCoords = Point.Zero; // Location in the container grid.

        protected float alpha = 1.0f;       // Overall alpha applied to complete element.
        private float targetAlpha = 1.0f;   // If the above alpha is changing due to a twitch, 
                                            // this is the value is is going toward.

        protected float grey = 1.0f;        // Overall grey applied to complete element.
        private float targetGrey = 1.0f;    // If the above grey is changing due to a twitch, 
                                            // this is the value is is going toward.

        protected const float kDim = 0.6f;  // How much to dim the element when not selected.  Used by Modular* elements.
        protected float dim = kDim;         // Current amount of dimming.

        protected bool dirty = true;        // Does this element need to be refreshed.

        protected GetFont Font = null;  // Delegate which will return the font to use for this UI element.  This font returned
                                                    // by this should not be held onto since a device reset may change it.

        protected Object tag = null;        // User defined object ref.
        protected string helpID = null;     // Optional ref used by help system.
        protected bool showHelpButton = false;  // Show the Y button in the lower right for more help.
        protected bool setHelpOverlay = true;   // If set to false, elements will not set help overlay.  The default is true since we're adding this kind of late.

        private bool visible = true;
        protected string elementName = null;

        #endregion

        #region Accessors

        /// <summary>
        /// Elements can be assigned names so they can referred to by the outside world to modify
        /// values such as visibility.
        /// </summary>
        public string ElementName
        {
            get { return elementName; }
        }

        /// <summary>
        /// The size of the UIElement used for adjusting its container.
        /// </summary>
        public abstract Vector2 Size
        {
            get;
            set;
        }
        /// <summary>
        /// Controls how the element is rendered.
        /// </summary>
        public abstract bool Selected
        {
            get;
            set;
        }
        public Vector3 Position
        {
            get { return position; }
            set 
            {
                position = value; 
            }
        }
        public float Scale
        {
            get { return scale; }
            set { scale = value; }
        }
        public Vector3 Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }
        /// <summary>
        /// Overall alpha applied to complete element.
        /// </summary>
        public float Alpha
        {
            get { return alpha; }
            set { alpha = value; targetAlpha = value; }
        }
        /// <summary>
        /// Overall grey scale amount applied to complete element.
        /// </summary>
        public float Grey
        {
            get { return grey; }
            set { grey = value; targetGrey = value; }
        }
        /// <summary>
        /// Does this element need to be refreshed? 
        /// </summary>
        public bool Dirty
        {
            get { return dirty; }
            set { dirty = value; }
        }

        /// <summary>
        /// Use defined object referenced.  Can be used
        /// to associate any object with this element.
        /// </summary>
        public Object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        /// <summary>
        /// If not visible, an element won't render.
        /// In order to get the layout to adjust for the changed state
        /// of the element you must set the Dirty flag on the grid
        /// after changing element states.  This will cause the grid
        /// to recalculate the positions of all the enabled elements.
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set { if (visible != value) { visible = value; dirty = true; } }
        }

        /// <summary>
        /// Optional id used by help system.  This is not a localized string.  Rather it's the
        /// string used to match tweakable entities with their help in the TweakScreenHelp.Xml file.
        /// </summary>
        public string HelpID
        {
            get { return helpID; }
            set { helpID = value; }
        }

        /// <summary>
        /// Returns the label associated with this element or null if no label exits.
        /// </summary>
        public virtual string Label
        {
            get { return null; }
            set { }
        }

        public bool ShowHelpButton
        {
            get { return showHelpButton; }
            set { showHelpButton = value; dirty = true; }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set
            {
                worldMatrix = value;
                invWorldMatrix = Matrix.Invert(worldMatrix);
            }
        }

        public Matrix InvWorldMatrix
        {
            get { return invWorldMatrix; }
        }

        /// <summary>
        /// If set to false, elements will not set help overlay.  
        /// The default is true since we're adding this kind of late
        /// and don't want to break existing behaviour.
        /// </summary>
        public bool SetHelpOverlay
        {
            get { return setHelpOverlay; }
            set { setHelpOverlay = value; }
        }

        #endregion

        public virtual void Update(ref Matrix parentMatrix)
        {
            localMatrix = Matrix.Identity;
            if (rotation.X != 0.0f)
            {
                localMatrix = Matrix.CreateRotationX(rotation.X);
            }
            if (rotation.Y != 0.0f)
            {
                localMatrix = Matrix.CreateRotationY(rotation.Y);
            }
            if (rotation.Z != 0.0f)
            {
                localMatrix = Matrix.CreateRotationZ(rotation.Z);
            }
            localMatrix *= Matrix.CreateScale(scale);
            localMatrix.Translation = position;
            WorldMatrix = localMatrix * parentMatrix;
        }   // end of UIElement Update()

        /// <summary>
        /// Change the position value using a twitch.
        /// </summary>
        /// <param name="finalValue">Destination value.</param>
        /// <param name="twitchTime">Time in seconds for twitch.</param>
        /// <param name="twitchShape">Animation curve shape to apply to twitch.</param>
        public void TwitchPosition(Vector3 finalValue, float twitchTime, TwitchCurve.Shape twitchShape)
        {
            if (twitchTime > 0.0f)
            {
                TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param) { Position = value; };
                TwitchManager.CreateTwitch<Vector3>(Position, finalValue, set, twitchTime, twitchShape);
            }
            else
            {
                // No time, just set the result.
                position = finalValue;
            }

        }   // end of UIGridElement TwitchPosition()

        /// <summary>
        /// Change the alpha value using a twitch.
        /// </summary>
        /// <param name="finalValue">Destination value.</param>
        /// <param name="twitchTime">Time in seconds for twitch.</param>
        /// <param name="twitchShape">Animation curve shape to apply to twitch.</param>
        public void TwitchAlpha(float finalValue, float twitchTime, TwitchCurve.Shape twitchShape)
        {
            if (twitchTime > 0.0f)
            {
                if (targetAlpha != finalValue)
                {
                    // Create a twitch to change alpha in baseColor
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Alpha = value; };
                    TwitchManager.CreateTwitch<float>(Alpha, finalValue, set, twitchTime, twitchShape);

                    targetAlpha = finalValue;
                }
            }
            else
            {
                // No time, just set the result.
                alpha = finalValue;
                targetAlpha = finalValue;
            }
        }   // end of UIGridElement TwitchAlpha()

        /// <summary>
        /// Change the scale value using a twitch.
        /// </summary>
        /// <param name="finalValue">Destination value.</param>
        /// <param name="twitchTime">Time in seconds for twitch.</param>
        /// <param name="twitchShape">Animation curve shape to apply to twitch.</param>
        public void TwitchScale(float finalValue, float twitchTime, TwitchCurve.Shape twitchShape)
        {
            if (twitchTime > 0.0f)
            {
                if (targetScale != finalValue)
                {
                    // Create a twitch to change Scale in baseColor
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Scale = value; };
                    TwitchManager.CreateTwitch<float>(Scale, finalValue, set, twitchTime, twitchShape);

                    targetScale = finalValue;
                }
            }
            else
            {
                // No time, just set the result.
                Scale = finalValue;
                targetScale = finalValue;
            }
        }   // end of UIGridElement TwitchScale()

        /// <summary>
        /// Change the grey value using a twitch.
        /// </summary>
        /// <param name="finalValue">Destination value.</param>
        /// <param name="twitchTime">Time in seconds for twitch.</param>
        /// <param name="twitchShape">Animation curve shape to apply to twitch.</param>
        public void TwitchGrey(float finalValue, float twitchTime, TwitchCurve.Shape twitchShape)
        {
            if (twitchTime > 0.0f)
            {
                if (targetGrey != finalValue)
                {
                    // Create a twitch to change grey in baseColor
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Grey = value; };
                    TwitchManager.CreateTwitch<float>(Grey, finalValue, set, twitchTime, twitchShape);

                    targetGrey = finalValue;
                }
            }
            else
            {
                // No time, just set the result.
                grey = finalValue;
                targetGrey = finalValue;
            }
        }   // end of UIGridElement TwitchGrey()

        public abstract void HandleMouseInput(Vector2 hitUV);

        public abstract void HandleTouchInput(TouchContact touch, Vector2 hitUV);

        public abstract void Render(Camera camera);

        public abstract void LoadContent(bool immediate);

        public abstract void InitDeviceResources(GraphicsDevice device);

        public virtual void UnloadContent()
        {
            UnloadInstanceContent();
        }

        public virtual void UnloadInstanceContent()
        {
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public abstract void DeviceReset(GraphicsDevice device);


    }   // end of class UIElement

}   // end of namespace Boku.UI2D


