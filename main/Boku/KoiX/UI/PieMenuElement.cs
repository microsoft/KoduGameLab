// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using KoiX;
using KoiX.Geometry;
using KoiX.Input;
using KoiX.Managers;
using KoiX.Text;
using KoiX.UI;
using KoiX.UI.Dialogs;

using Boku.Common;
using Boku.Programming;

namespace KoiX.UI
{
    public class PieMenuElement : BaseWidget
    {
        #region Members

        string labelId;
        string textureName;
        StaticActor staticActor;
        BasePieMenuDialog child;    // Submenu to be launched by picking this slice.

        // Geometry, all in camera coordinates.
        public Texture2D Texture;
        public RectangleF Rectangle;
        public Vector2 Center;
        public float InnerRadius;
        public float OuterRadius;
        public Vector2 EdgeNormal0;
        public Vector2 EdgeNormal1;
        public Vector2 EdgeIntersect;
        public float CenterAngle;       // Angle relative to center of element.

        Twitchable<Color> bodyColor;
        Twitchable<Color> outlineColor;

        #endregion

        #region Accessors

        public StaticActor StaticActor
        {
            get { return staticActor; }
            set { staticActor = value; }
        }

        public BasePieMenuDialog Child
        {
            get { return child; }
            set { child = value; }
        }

        #endregion

        #region Public

        public PieMenuElement(BaseDialog parent, string labelId, string textureName, StaticActor staticActor = null, BasePieMenuDialog child = null, ThemeSet theme = null)
            : base(parentDialog: parent)
        {
            Debug.Assert(!(staticActor != null && child != null), "There can be only one.");

            if (theme == null)
            {
                theme = Theme.CurrentThemeSet;
            }

            bodyColor = new Twitchable<Color>(0.2f, TwitchCurve.Shape.EaseInOut, startingValue: theme.PieMenuNormal.BodyColor);
            outlineColor = new Twitchable<Color>(0.2f, TwitchCurve.Shape.EaseInOut, startingValue: theme.PieMenuNormal.OutlineColor);

            this.labelId = labelId;
            this.textureName = textureName;
            this.staticActor = staticActor;
            this.child = child;
        }   // end of c'tor

        public override void Update(SpriteCamera camera, Vector2 parentPosition)
        {
            if (Active)
            {
                if (InFocus)
                {
                    bodyColor.Value = theme.PieMenuNormalFocused.BodyColor;
                    outlineColor.Value = theme.PieMenuNormalFocused.OutlineColor;
                }
                else
                {
                    bodyColor.Value = theme.PieMenuNormal.BodyColor;
                    outlineColor.Value = theme.PieMenuNormal.OutlineColor;
                }
            }

            base.Update(camera, parentPosition);

        }   // end of Update()

        public void Render(SpriteCamera camera)
        {
            PieSlice.Render(camera, Rectangle,
                Center, InnerRadius, OuterRadius,
                EdgeNormal0, EdgeNormal1, 
                EdgeIntersect,
                bodyColor.Value,
                outlineWidth: 5.0f, outlineColor: outlineColor.Value,
                texture: Texture, texturePadding: padding
                );
        }   // end of Render()

        public void RenderShadow(SpriteCamera camera)
        {
            // If this slice launches a sub-menu we need to draw the
            // underlying slice segments as part of the shadow.
            if (Child != null)
            {
                float offsetScale = 6.0f;
                Vector2 offset = offsetScale * new Vector2((float)Math.Cos(CenterAngle), (float)Math.Sin(CenterAngle));

                RectangleF r = Rectangle;
                r.Position += 2.0f * offset;

                float layerScale = 6.0f;    // This is used to scale the size of the underlying slices so
                                            // they look smaller than the top one.  1.0 == same size, larger
                                            // numbers get smaller.

                PieSlice.RenderShadow(camera, r,
                    Center + 2.0f * offset, InnerRadius, OuterRadius,
                    EdgeNormal0, EdgeNormal1, EdgeIntersect + layerScale * 2.0f * offset,
                    ShadowStyle.Outer, shadowOffset: new Vector2(6, 6), shadowSize: 10.0f, shadowAttenuation: 0.8f
                    );

                PieSlice.Render(camera, r,
                    Center + 2.0f * offset, InnerRadius, OuterRadius,
                    EdgeNormal0, EdgeNormal1,
                    EdgeIntersect + layerScale * 2.0f * offset,
                    theme.PieMenuNormal.BodyColor,
                    outlineWidth: 5.0f, outlineColor: outlineColor.Value
                    );

                r.Position -= offset;

                PieSlice.RenderShadow(camera, r,
                    Center + offset, InnerRadius, OuterRadius,
                    EdgeNormal0, EdgeNormal1, EdgeIntersect + layerScale * offset,
                    ShadowStyle.Outer, shadowOffset: new Vector2(6, 6), shadowSize: 10.0f, shadowAttenuation: 0.8f
                    );

                PieSlice.Render(camera, r,
                    Center + offset, InnerRadius, OuterRadius,
                    EdgeNormal0, EdgeNormal1,
                    EdgeIntersect + layerScale * offset,
                    theme.PieMenuNormal.BodyColor,
                    outlineWidth: 5.0f, outlineColor: outlineColor.Value
                    );

            }

            PieSlice.RenderShadow(camera, Rectangle, 
                Center, InnerRadius, OuterRadius, 
                EdgeNormal0, EdgeNormal1, EdgeIntersect,
                ShadowStyle.Outer, shadowOffset: new Vector2(6, 6), shadowSize: 10.0f, shadowAttenuation: 0.8f
                );



        }   // end of RenderShadow()

        #endregion


        #region InputEventHandler

        // NOTE All input is handled at the PieMenu level but we still want to look
        // at the mouse for hover/focus info.

        public override void RegisterForInputEvents()
        {
            KoiLibrary.InputEventManager.RegisterForEvent(this, InputEventManager.Event.MouseMove);

            base.RegisterForInputEvents();
        }   // end of RegisterForInputEvents()

        public override bool ProcessMouseMoveEvent(MouseInput input)
        {
            Debug.Assert(Active);

            Vector2 mouseHit = input.Position;
            BasePieMenuDialog p = parentDialog as BasePieMenuDialog;
            mouseHit = p.LocalCamera.ScreenToCamera(mouseHit);
            if (HitTest(mouseHit) != null)
            {
                SetFocus();
            }

            return base.ProcessMouseMoveEvent(input);
        }   // end of ProcessMouseMoveEvent()

        public override InputEventHandler HitTest(Vector2 hitLocation)
        {
            Vector2 dir = hitLocation - Center;
            // Test against edges.
            if (Vector2.Dot(dir, EdgeNormal0) > 0 && Vector2.Dot(dir, EdgeNormal1) > 0)
            {
                // Test radius.
                float dist = (hitLocation - Center).Length();
                if (dist >= InnerRadius && dist <= OuterRadius)
                {
                    return this;
                }
            }
            
            return base.HitTest(hitLocation);
        }   // end of HitTest()

        #endregion

        #region Internal

        public override void LoadContent()
        {
            Texture = CardSpace.Cards.CardFaceTexture(textureName);
            if (Texture == null)
            {
                Texture = KoiLibrary.LoadTexture2D(textureName);
            }
        }   // end of LoadContent()

        public override void UnloadContent()
        {
            DeviceResetX.Release(ref Texture);
        }   // end of UnloadContent()

        #endregion

    }   // end of class PieMenuElement

}   // end of namespace KoiX.UI
