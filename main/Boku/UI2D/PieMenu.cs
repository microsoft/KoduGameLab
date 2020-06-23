
using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.Programming;
using Boku.Fx;
using Boku.Audio;
using Boku.Common.TutorialSystem;
using Boku.Common.Gesture;

namespace Boku.UI
{
    /// <summary>
    ///  This class provides a generic use of a pie wheele UI menu that can support Icons, text, and 
    ///  attached objects for the purpose of user activated "return items" <<ie: user chose this.>>
    ///  This menu sytem fully suports rich hierarchy as well as use as a flat pie where the
    ///  caller can add one slice at a time.
    ///  NOTE: **This Class is currently only implemented for "Touch Use"
    ///  Additional hit testing and general inpout will have to be added for mouse/game controller use.
    /// </summary>
    public class PieMenu
    {
        /// <summary>
        /// To build a pie menu with hierarchy depth a "Recipie" must be constructed
        /// consisting of a list of these "PieRecipeItem"'s 
        /// Note: you must include a (texture) for the icon OR (font & text) for display
        /// The "menuItem" is Any object you choose to associate with the indevidual menu item
        /// and will be returned on ching if an item was chosen by the user.
        /// </summary>
        public class PieRecipeItem
        {
            public UI2D.Shared.GetFont font = null; // Optional Font (Label) display
            public string label;                    // Optional text display (Font required)
            public Texture2D texture = null;        // Optional Icon display
            public Object menuItem = null;
            public List<PieRecipeItem> subList = null;

            public PieRecipeItem()
            {
                this.subList = new List<PieRecipeItem>();
            }
            public PieRecipeItem(Object item, Texture2D texture, List<PieRecipeItem>  subList, UI2D.Shared.GetFont font, string label)
            {
                this.menuItem =item;
                this.texture = texture;
                this.subList = subList;
                this.font = font;
                this.label = label;
            }
        }

        /// <summary>
        /// Detail for indevidual pie slice backdrop which is a 3d construct of primatives.
        /// </summary>
        public class RenderPieSlice : PieSelector.IndexPrimitiveSlice, ITransform
        {
            public Transform localTransform = new Transform();
            public Matrix worldMatrix = Matrix.Identity;
            public ITransform transformParent;

            public RenderPieSlice(float innerRadius, float outerRadius, float arcLength, PieSelector.SliceType sliceType)
                : base(innerRadius, outerRadius, arcLength, sliceType)
            {
                CreateGeometry(BokuGame.bokuGame.GraphicsDevice);
            }

            // ITransform
            Transform ITransform.Local
            {
                get
                {
                    return this.localTransform;
                }
                set
                {
                    this.localTransform = value;
                }
            }
            Matrix ITransform.World
            {
                get
                {
                    return this.worldMatrix;
                }
            }
            bool ITransform.Compose()
            {
                bool changed = this.localTransform.Compose();
                if (changed)
                {
                    RecalcMatrix();
                }
                return changed;
            }
            void ITransform.Recalc(ref Matrix parentMatrix)
            {
                this.worldMatrix = this.localTransform.Matrix * parentMatrix;
                /*
                if (renderObj != null)
                {
                    foreach (ITransform transformChild in renderObj.renderList)
                    {
                        transformChild.Recalc(ref worldMatrix);
                    }
                }
                 */
            }
            ITransform ITransform.Parent
            {
                get
                {
                    return this.transformParent;
                }
                set
                {
                    this.transformParent = value;
                }
            }
            protected void RecalcMatrix()
            {
                ITransform transformThis = this as ITransform;
                Matrix parentWorldMatrix;
                if (transformParent != null)
                {
                    parentWorldMatrix = transformParent.World;
                }
                else
                {
                    parentWorldMatrix = Matrix.Identity;
                }
                transformThis.Recalc(ref parentWorldMatrix);
            }

        }

        /// <summary>
        ///  The offset of the pie slice in relevance to its default location after the appropriate rotation
        ///  and placement within the pie.
        ///  Note: the given starting offset here provides the gap between itself and its neighbor
        ///  by pushing it slightly away from the center.
        /// </summary>
        static protected Vector3 SliceOffsetDefault = new Vector3(0.05f, 0.0f, 0.0f);

        /// <summary>
        /// Complete data for any pie slice including Lable text and icon details
        /// </summary>
        public class PieMenuSlice : Fugly
        {
            #region Members & Constants
            public static float DEFAULT_TILE_SIZE = 93;
            public Vector4 DiffuseColor = PieSelector.RenderObjSlice.ColorNormal;
            public RenderPieSlice mySlice;
            public PieDisk subDisk = null;
            public PieSelector.SliceType sliceType;
            private Texture2D texture;
            public UI2D.Shared.GetFont font;
            public string label;
            public TextBlob textBlob = null;
            private Object menuItem = null;
            private bool isFocused = false;
            private Vector2 positionOffest; // from the pies center
            
            #endregion

            public float IconSize
            {
                get {
                    if (BokuGame.bokuGame.GraphicsDevice.Viewport.Height < 1024)
                        return 64;
                    else
                        return DEFAULT_TILE_SIZE;
                }
            }

            public Vector2 WhenBlockSize
            {
                get { return new Vector2(IconSize,IconSize); }
            }

            public void OnDestroy()
            {

            }

            public Vector2 Position
            {
                get { return positionOffest; }
                set {  positionOffest = value; }
            }           

            public Texture2D IconTexture
            {
                get { return texture; }
                set { texture = value; }
            }

            public UI2D.Shared.GetFont SliceFont
            {
                get { return font; }
                set { font = value; }
            }

            public bool Focused
            {
                get { return isFocused;  }
                set 
                {
                    if (isFocused != value)
                    {
                        isFocused = value;
                        SetFocused();
                    }  
                }
            }

            public Object MenuItem
            {
                get { return menuItem; }
                set { menuItem = value; }
            }

            // constructor
            // @params: item icon, optional text
            public PieMenuSlice(Object menuItem, Texture2D iconTexture, UI2D.Shared.GetFont font, string displayText)
            {
                int textWidth = 80;
                Debug.Assert(iconTexture != null || font != null);
                this.menuItem = menuItem;
                this.texture = iconTexture;
                this.font = font;
                this.label = displayText;
                this.textBlob = new TextBlob(font, displayText, textWidth);
                this.sliceType = PieSelector.SliceType.single;
            }

            public PieMenuSlice(PieRecipeItem recipe)
            {
                this.menuItem = recipe.menuItem;
                this.texture = recipe.texture;
                this.font = recipe.font;
                this.label = recipe.label;
                if (recipe.subList ==null || recipe.subList.Count == 0)
                {
                    this.sliceType = PieSelector.SliceType.single;
                }
                else
                {
                    this.sliceType = PieSelector.SliceType.group;
                }
            }

            private void SetFocused()
            {
                ///  change color and move it out from the center radius a tad....
                if (isFocused)
                {

                }
                ///  change TO DEFAULT color and move it back to normal location
                else
                {

                }
            }
        }

        //  a disk consist of one to many pie slices
        public class PieDisk
        {
            #region Members and Constants
            public List<PieMenuSlice> slices = new List<PieMenuSlice>();
            public float iconScaling = 0.6f;
            public bool visible = false;
            private Object parent = null;
            private PieRecipeItem rootRecipe;
            private bool sliceActivated = false;
            protected float radiusAtItems;
            protected float innerRadius;
            protected float outerRadius;
            protected Vector3 originalTranslation;
            protected bool gotTouchBegan = false;

            int indexCurrentHoverItem = -1;
            int indexLastHoverItem = -1;
            int indexPickedItem = -1;

            Vector2 screenPosition;
            public List<PieDisk> subList = new List<PieDisk>();

            public Matrix worldMatrix = Matrix.Identity;

            #endregion

            public Object Parent
            {
                get {return parent;}
            }
            public PieMenuSlice SelectedSlice
            {
                get {
                    if (sliceActivated)
                        return slices[indexPickedItem];
                    else
                        return null;
                }
            }

            public Vector2 ScreenPosition
            {
                get { return screenPosition; }
                set
                {
                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                    Vector2 screenCenter = new Vector2(device.Viewport.Width / 2, device.Viewport.Height / 2);
                    Vector2 centeroffset = value - screenCenter;

                    screenPosition = value - centeroffset / 2.0f;
                }
            }

            public Vector2 ScreenOffset
            {
                get {

                    GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                    Vector2 screenCenter = new Vector2(device.Viewport.Width / 2, device.Viewport.Height / 2);
                    return screenPosition -screenCenter;
                }
            }

            public bool Visible
            {
                get { return visible; }
                set { 
                    visible = value;
                    if (visible)
                        indexPickedItem = -1;
                }
            }
            // constructor from a complete hierarchy set
            public PieDisk(Object parentPie, PieRecipeItem recipe)
            {
                this.parent = parentPie;
                this.rootRecipe = recipe;
            }
            // constructor 
            public PieDisk(PieDisk parentPie)
            {
                parent = parentPie;
            }

            public void OnCancel()
            {
                for (int i = 0; i < slices.Count; i++ )
                {
                    PieMenuSlice pms = slices[slices.Count - 1];
                    pms.OnDestroy();
                }
               // slices.Clear();
            }

            public void AddSlice(PieMenuSlice slice)
            {
                slices.Add(slice);
            }


            // builds either the sing disk or, if given the complete hierarchy
            public void BakePie(bool isVisible)
            {
                Visible = isVisible;
                if (rootRecipe.subList != null && (rootRecipe.subList.Count > 0))
                {
                    // build full pie!
                    for (int i = 0; i < rootRecipe.subList.Count; i++)
                    {
                        PieRecipeItem pieRecipeSlice = rootRecipe.subList[i];

                        PieMenuSlice slice = new PieMenuSlice(pieRecipeSlice);
                        AddSlice(slice);
                        if (pieRecipeSlice.subList!= null &&  pieRecipeSlice.subList.Count > 0) 
                        { // this is a "group node" with another pie structure below
                            PieDisk fullPie = new PieDisk(this, pieRecipeSlice);
                            slice.subDisk = fullPie;

                            subList.Add(fullPie);
                            fullPie.BakePie(false);
                            //subList[subList.Count - 1].BakePie(false);
                        }
                    }
                }
                else if (slices.Count == 0)
                {
                    // something whent wrong.. we have nothing to build!
                    return;
                }

                if (Visible)
                {
                    BuildSlices();
                }
            }

            private void BuildSlices()
            {
                ProgrammingElement el = null;

                ///---------------------
                ///
                float circumferenceAtItems = 5.0f + slices.Count / 6.0f;
                float maxItemRadius = 0.5f + slices.Count / 40.0f;
                const float radiusAtItemSpacing = 0.2f;

                // calculate layout information from items
                // depreciated....
                //CalcLayoutInfoFromItems(out circumferenceAtItems, out maxItemRadius);

                // calc radius of the pie
                this.radiusAtItems = circumferenceAtItems / MathHelper.TwoPi + radiusAtItemSpacing;
                float spacingCircumference = 0.0f; // spacing between items on the circumference

                // adjust spacing if radius is smaller than the inner radius (one item)
                if (this.radiusAtItems < maxItemRadius * 2.0f)
                {
                    // radius is too small, must increase 
                    this.radiusAtItems = maxItemRadius * 2.0f + radiusAtItemSpacing;
                    // and provide the extra spacing between items on the circumference
                    float newCircumference = MathHelper.TwoPi * this.radiusAtItems;
                    spacingCircumference = (newCircumference - circumferenceAtItems) / slices.Count;
                    circumferenceAtItems = newCircumference;
                }
                float radiusInside = maxItemRadius * 1.2f; // with a little spacing
                float radiusOutside = this.radiusAtItems + (maxItemRadius * 1.2f); // with a little spacing
                float arcLength = MathHelper.TwoPi / slices.Count;

                Vector2 posUV = Vector2.Zero;
                Matrix invWorld = Matrix.Invert(worldMatrix);
                 

                Fugly fuglyTransform = new Fugly();
                for (int indexItem = 0; indexItem < slices.Count; indexItem++)
                {
                    ReflexCard reflex = slices[indexItem].MenuItem as ReflexCard;
                    if (reflex != null)
                    {
                        el = reflex.Reflex.Sensor;
                    }

                    float rot = MathHelper.PiOver2 - indexItem * arcLength;
                    
                  //  slices[indexItem].mySlice = BuildPieSlice(el, arcLength, radiusInside, radiusOutside);
                    slices[indexItem].mySlice = BuildPieSlice(slices[indexItem].sliceType, arcLength, radiusInside, radiusOutside);
                    innerRadius = radiusInside;
                    outerRadius = radiusOutside;

      
                    // create a slice for every item

                    ITransform transformSlice = slices[indexItem].mySlice as ITransform;
                  
                    // rotate the slice into place
                    transformSlice.Local.OriginTranslation = SliceOffsetDefault; // move away from center to space them
                    transformSlice.Local.RotationZ = rot;
                    transformSlice.Compose();
                    
                    // setting position for the icon....
                    Matrix rotation = Matrix.CreateRotationZ( rot );
                    Vector3 fixRotation = new Vector3(radiusInside + (radiusOutside - radiusInside) * 0.6f, 0.0f, 0.0f);
           
                    slices[indexItem].Position = camera.WorldToScreenCoordsVector2( Vector3.Transform(fixRotation, rotation));
                    slices[indexItem].Position -= slices[indexItem].WhenBlockSize/2;
                    
                }
            }

            protected void CalcLayoutInfoFromItems(out float circumference, out float maxRadius)
            {
                circumference = 0.0f;
              //  UiSelector.ItemData itemData;
                maxRadius = float.NegativeInfinity;
                for (int indexItem = 0; indexItem < slices.Count; indexItem++)
                {
                    PieMenuSlice tmpItemData = slices[indexItem];
                    
                    float diagonal = (float)Math.Sqrt( Math.Pow(tmpItemData.IconTexture.Height,2) + 
                                                    Math.Pow(tmpItemData.IconTexture.Width,2));
                    diagonal *= iconScaling;
                    if (tmpItemData != null)
                    {
                        circumference += diagonal; //  boundingItem.BoundingSphere.Radius * 2.0f;
                        maxRadius = MathHelper.Max(maxRadius, diagonal/2); //boundingItem.BoundingSphere.Radius);
                    }
                }
            }

            public bool WithinPieRing(Vector2 pos)
            {
                //ScreenPosition
                float len = pos.Length();
                if (len > outerRadius || len < innerRadius)
                    return false;
                return true;
            }


            public int GetSliceAtAngle(Vector2 pos)
            {
                Vector2 dir = pos;
                dir.Normalize();
                int sliceIndexAtPos = -1;

                // Calc the angle of the stick.  Set this up so that directly up results 
                // in an angle of 0 and to the right results in pi/2 (clockwise).
                double angle = Math.Acos(dir.Y);
                if (dir.X < 0.0f)
                {
                    angle = MathHelper.TwoPi - angle;
                }

                // Calc the arc covered by each menu item.
                double arcItem = (MathHelper.TwoPi / (double)slices.Count);

                // Are we entering for the first time?
                if (sliceIndexAtPos == -1)
                {
                    // Add half this as an offset to angle since the 0th item is directly upward.
                    double fooAngle = (angle + arcItem / 2.0) % MathHelper.TwoPi;

                    sliceIndexAtPos = (int)(fooAngle / arcItem);
                }
                else
                {
                    // We already have a selected item, so we want to see if the selection has changed.

                    // Calc angle for center of selected item.
                    double selectedAngle = sliceIndexAtPos * arcItem;

                    // Calc the stick angle relative to this angle.  Positive is clockwise...
                    double relative = angle - selectedAngle;
                    if (relative > MathHelper.Pi)
                    {
                        relative = relative - MathHelper.TwoPi;
                    }
                    else if (relative < -MathHelper.Pi)
                    {
                        relative = relative + MathHelper.TwoPi;
                    }

                    // Calc max relative angle we need before switching to the 
                    // next item.  Use half the width of the pie segment plus 
                    // a little extra to provide some hysteresis.
                    double maxAngle = arcItem / 2.0f;
                    // Only apply hysteresis when we have more that 4 items in the
                    // pie.  Use 1/3 of the width of the segment.  If we're using
                    // the mouse, don't apply any hysteresis.
                    if (slices.Count > 4 )
                    {
                        maxAngle += arcItem / 3.0f;
                    }

                    if (relative > maxAngle)
                    {
                        sliceIndexAtPos = (sliceIndexAtPos + 1) % slices.Count;
                    }
                    else if (relative < -maxAngle)
                    {
                        sliceIndexAtPos = (sliceIndexAtPos - 1 + slices.Count) % slices.Count;
                    }

                }
                if (indexCurrentHoverItem >-1 && (sliceIndexAtPos != indexCurrentHoverItem))
                    indexLastHoverItem = indexCurrentHoverItem;
                return sliceIndexAtPos;
            }

            public int SliceCount()
            {
                return slices.Count;
            }

            private RenderPieSlice BuildPieSlice(PieSelector.SliceType type, float arcLen, float innnerRad, float outerRad)
            {
                RenderPieSlice pieSlice = new RenderPieSlice(innnerRad, outerRad, arcLen, type);
                return pieSlice;
            }

            public void SetFocus(int indexNew)
            {
                float twitchTime = 0.1f;

                // Undo previous slice selection state.
                if ((this.indexLastHoverItem >-1) && 
                    (this.indexLastHoverItem != indexNew))
                {
                    PieMenuSlice menuSlice = this.slices[indexLastHoverItem];
                    menuSlice.DiffuseColor = PieSelector.RenderObjSlice.ColorNormal;

                    if (slices.Count > 2) // dont move them if only two
                    {
                        ITransform transformSlice = menuSlice.mySlice as ITransform;
                        TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                        {
                            transformSlice.Local.OriginTranslation = value;
                            transformSlice.Compose();
                        };
                        TwitchManager.CreateTwitch<Vector3>(
                            transformSlice.Local.OriginTranslation, 
                            SliceOffsetDefault, 
                            set, 
                            twitchTime, 
                            TwitchCurve.Shape.EaseInOut);
                    }
                }

                // apply new slice selection state
               // if (indexNew != indexCenteredItem)
                if ((indexNew >-1) && 
                    (indexNew != indexCurrentHoverItem))
                {
                    PieMenuSlice menuSlice = this.slices[indexNew];
                    menuSlice.DiffuseColor = PieSelector.RenderObjSlice.ColorSelectedBright;

                    Foley.PlayClick();
                    if (slices.Count > 2) // don't move them if only two
                    {
                        ITransform transformSlice = menuSlice.mySlice as ITransform;
                        {
                            TwitchManager.Set<Vector3> set = delegate(Vector3 value, Object param)
                            {
                                transformSlice.Local.OriginTranslation = value;
                                transformSlice.Compose();
                            };
                            TwitchManager.CreateTwitch<Vector3>(
                                transformSlice.Local.OriginTranslation, 
                                new Vector3(0.20f, 0.0f, 0.0f), 
                                set, 
                                twitchTime, 
                                TwitchCurve.Shape.EaseInOut);
                        }
                    }
                    indexCurrentHoverItem = indexNew;
                }
            }

            public void Update(out bool cancelRing)
            {
                sliceActivated = false;
                cancelRing = false;
                Vector2 hitUV = Vector2.Zero;
                Matrix invWorld = Matrix.Invert(worldMatrix);
                // user clicked on me?

                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;
                // Center on screen and just high enough to clear bottom help overlay text.
                Vector2 screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                
                int sliceIndex = -1;

                for (int i = 0; i < TouchInput.TouchCount; i++)
                {
                    TouchContact touch = TouchInput.GetTouchContactByIndex(i);
                    Vector2 touchPos = touch.position;

                    touchPos = touch.position - ScreenOffset;
                    hitUV = TouchInput.GetHitOrtho(touchPos, camera, ref invWorld, useRtCoords: false);

                    if (gotTouchBegan)
                    {
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            if (WithinPieRing(hitUV))
                            {
                                sliceIndex = GetSliceAtAngle(hitUV);
                                SetFocus(sliceIndex);
                                indexLastHoverItem = indexCurrentHoverItem;
                            }
                        }
                        else if (TouchGestureManager.Get().TapGesture.WasTapped())  //cancel?
                        {
                            float len = hitUV.Length();
                            if (len > outerRadius)
                            {
                                cancelRing = true;
                                sliceActivated = false;
                            }
                        }

                        // is this a group slice? open its pie!
                        if (!cancelRing && (touch.phase == TouchPhase.Ended))
                        {
                            if (WithinPieRing(hitUV))
                            {
                                sliceIndex = GetSliceAtAngle(hitUV);
                                if (sliceIndex > -1)
                                {
                                    PieDisk nextDisk = slices[sliceIndex].subDisk;
                                    if (nextDisk != null &&
                                        nextDisk.SliceCount() > 0)
                                    {
                                        Object rootParent = Parent;
                                        while ((rootParent as PieMenu) == null)
                                        {
                                            rootParent = (rootParent as PieDisk).Parent;
                                        }
                                        (rootParent as PieMenu).activeDisk = nextDisk;
                                        (rootParent as PieMenu).focusedDiskNo++;

                                        nextDisk.Visible = true;
                                        nextDisk.ScreenPosition = touch.position;
                                        nextDisk.BuildSlices();
                                    }
                                    else
                                    {
                                        indexPickedItem = sliceIndex;
                                        sliceActivated = true;
                                    }
                                }
                                else
                                {
                                    Debug.Assert(false);// && "something unexpected failed here.");
                                }
                            }
                        }
                        else
                        {
                            sliceActivated = false;
                        }
                    }

                    if (touch.phase == TouchPhase.Began)
                    {
                        gotTouchBegan = true;
                    }
                    else if (touch.phase == TouchPhase.Ended)
                    {
                        gotTouchBegan = false;
                    }
                }
                if (TouchInput.TouchCount == 0)
                {
                    indexCurrentHoverItem = -1;
                    SetFocus(sliceIndex);
                }
                if ((sliceIndex == -1))
                {
                    indexCurrentHoverItem = -1;
                    SetFocus(sliceIndex);
                    indexLastHoverItem = -1;
                }
            }
        }

#region Members and Constants
        private bool active=false;
        private Effect effect = null; 
        //private ReflexCard rootCardNode = null;
        private PieRecipeItem rootRecipe;
        static private UiCamera camera = new UiCamera();
        private Vector2 desiredLocation;
        private Vector2 offsetLocation;
        private PieDisk rootDisk = null;
        private PieDisk activeDisk = null;

        private int focusedDiskNo = 0;
#endregion    

#region Accessors
        public bool Active
        {
            get {return active;}
            set {
                if (rootDisk != null &&
                    (
                    (rootDisk.SliceCount() > 0)||
                    (rootRecipe.subList.Count > 0))
                    )
                {
                    active = value;
                    if (active)
                    {
                        effect = Editor.Effect;
                        if (effect == null)
                        {
                            Editor.Effect = BokuGame.Load<Effect>(BokuGame.Settings.MediaPath + @"Shaders\UI");
                            effect = Editor.Effect;
                            ShaderGlobals.RegisterEffect("UI", effect);
                        }
                        SetActivePie();
                    }
                }
                else if (focusedDiskNo < 0)
                {
                    active = false;
                }

            }
        }

        public int CurrentDiskNo
        {
            get { return focusedDiskNo; }
        }

        public Object SelectedObject
        {
            get {
                PieMenuSlice activatingSlice = activeDisk.SelectedSlice;
                if ( activatingSlice != null )
                {
                    return activatingSlice.MenuItem;
                }
                return null;
            }
        }

        public Camera Camera
        {
            get { return camera; }
        }
#endregion

        void SetLocation()
        {
            ///  \TODO:  check that the desired location FITS with the full circumfrence within the screen
            ///  if not move it to fit etc....
            offsetLocation = desiredLocation;
        }
        // constructor auto builds pie from a complete heirarchy
        public PieMenu(PieRecipeItem rootRecipe, Vector2 desiredLocation)
        {
            this.desiredLocation = desiredLocation;
            this.rootRecipe = rootRecipe; // remember this one....
            this.rootDisk = new PieDisk(this, rootRecipe);
            this.rootDisk.ScreenPosition = desiredLocation;
     
        }

        // constructor EMPTY PIE!
        public PieMenu(Vector2 desiredLocation)
        {
            this.desiredLocation = desiredLocation;
            this.rootDisk = new PieDisk(null);
        }

        /// <summary>
        /// Test to see it the users Touch focus is over a pie menu element
        /// </summary>
        /// <param name="touch"></param>
        /// <param name="ignoreOnDrag"></param>
        /// <returns></returns>
        public bool IsOverUIButton(TouchContact touch, bool ignoreOnDrag)
        {
            /// For now, always return true is a pie is visible as the user should ONLY be
            /// effecting the pie when it is up.
            bool elementFound = true;
            if (Active == false)
                elementFound = false;

            return elementFound;
        }
        // Add a simple slice to the main pie
        public void AddSlice(Object menuItem, Texture2D iconTexture, UI2D.Shared.GetFont font, string displaytext)
        {
            PieMenuSlice slice = new PieMenuSlice(menuItem, iconTexture, font, displaytext);            
            this.rootDisk.AddSlice(slice);
        }

        // required after setting up / adding pie slices
        public void SetActivePie()
        {
            BuildAllSlices();
        }

        private void BuildAllSlices()
        {
            bool drawPie = false;
            Debug.Assert(rootDisk != null);

            if (CurrentDiskNo == 0)
                drawPie = true;
            activeDisk = rootDisk;

            // build pie slices
            rootDisk.BakePie(drawPie);
        }

        public void Update()
        {
            camera.Update();
            if (Active)
            {
                bool canceled = false;
                activeDisk.Update( out canceled );
                if (canceled && CurrentDiskNo >= 0)
                {
                    if (CurrentDiskNo == 0)
                    { // we are done. no more menu
                        Active = false;
                        if (activeDisk != null)
                        {
                            activeDisk.OnCancel();
                        }
                    }
                    else
                    {
                        focusedDiskNo--;
                        activeDisk.Visible = false;
                        activeDisk.OnCancel();
                        activeDisk = (activeDisk.Parent as PieDisk);
                    }
                    //activeDisk.OnCancel();
                }
            }
            // TODO check for user UI interation...
        }

        public void Render()
        {
            ///  render entire pie
            if (Active)
            {
                ShaderGlobals.SetValues(effect);
                ShaderGlobals.SetCamera(effect, camera);

                RenderDisk(rootDisk);
            }
        }

        private void RenderDisk(PieDisk disk)
        {
            RenderDisk(disk, 0);
        }

        private void RenderDisk(PieDisk disk, int depth)
        {
            Matrix invWorld = Matrix.Invert(disk.worldMatrix);
            Vector2 orthoPos = TouchInput.GetHitOrtho(disk.ScreenPosition, camera, ref invWorld, useRtCoords: false);
            for (int i = 0; i < disk.slices.Count; i++)
            {
                PieMenuSlice pieSlice = disk.slices[i];
                RenderBastardPieSlice(pieSlice, orthoPos, disk.ScreenOffset, depth);
            }
            for (int i = 0; i < disk.slices.Count; i++)
            {
                PieMenuSlice pieSlice = disk.slices[i];
                if (pieSlice.sliceType == PieSelector.SliceType.group)
                {
                    PieDisk subDisk = pieSlice.subDisk;
                    if (subDisk.Visible)
                    {
                        RenderDisk(subDisk, depth+1);
                    }
                }
            }
        }

        private void RenderBastardPieSlice(PieMenuSlice pieSlice, Vector2 orthPos, Vector2 screenOffset, int depth)
        {
            if (pieSlice.mySlice == null)
            {
                return;
            }
            float alphaLevel = 1.0f;
            bool fadeAlpha = false;
            if (depth != CurrentDiskNo)
                fadeAlpha = true;
            
            if (fadeAlpha)
            {
                alphaLevel = 0.82f;
                if (CurrentDiskNo < 7)
                    alphaLevel -= (CurrentDiskNo - depth) / 10.0f;
            }

            GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;    
            Matrix viewMatrix = camera.ViewMatrix;
            Matrix projMatrix = camera.ProjectionMatrix;
            Matrix worldMatrix = pieSlice.mySlice.localTransform.Matrix;
            worldMatrix.Translation += new Vector3(orthPos.X, orthPos.Y, 0.0f);  

            Matrix worldViewProjMatrix = worldMatrix * viewMatrix * projMatrix;

            effect.Parameters["WorldViewProjMatrix"].SetValue(worldViewProjMatrix);
            effect.Parameters["WorldMatrix"].SetValue(worldMatrix);


            Vector4 baseColor = PieSelector.RenderObjSlice.ColorNormal;
            // used for debugging
            /*
            if (pieSlice.Focused)
            {
                baseColor = PieSelector.RenderObjSlice.ColorSelectedBright;
            }
            */
            baseColor = pieSlice.DiffuseColor;
            if (fadeAlpha)
                baseColor.W = alphaLevel;


            effect.Parameters["DiffuseColor"].SetValue(baseColor);
            effect.Parameters["SpecularColor"].SetValue(new Vector4(0.12f, 0.12f, 0.12f, alphaLevel));
            effect.Parameters["EmissiveColor"].SetValue(new Vector4(0.01f, 0.01f, 0.01f, alphaLevel));
            effect.Parameters["SpecularPower"].SetValue(4.0f);
            effect.Parameters["Shininess"].SetValue(0.0f);

            if (InGame.inGame.renderEffects != InGame.RenderEffect.Normal)
            {
                effect.CurrentTechnique = effect.Techniques[InGame.inGame.renderEffects.ToString()];
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["NoTextureColorPass"];
            }

            pieSlice.mySlice.Render(device, effect);


            // render icon if there is one
            if (pieSlice.IconTexture != null)
            {
                ScreenSpaceQuad ssquad = ScreenSpaceQuad.GetInstance();
                Vector4 myBaseColor = new Vector4(1.0f, 1.0f, 1.0f, alphaLevel);

                ssquad.Render(pieSlice.IconTexture,
                    myBaseColor,
                    pieSlice.Position + screenOffset,
                    pieSlice.WhenBlockSize,
                    "TexturedRegularAlpha");
            }
            if ( pieSlice.SliceFont != null )
            {
                // Render text!

                float textWidth =  pieSlice.font().MeasureString(pieSlice.label).X;
                Color greyTextColor = new Color(1.0f, 1.0f, 1.0f, alphaLevel);
                Color blackTextColor = new Color(1.0f, 1.0f, 1.0f, alphaLevel);
                Color renderColor = blackTextColor;
                if (fadeAlpha)
                    renderColor = greyTextColor;
                // Correct for icon and pie position offset.
                Vector2 textPosition = pieSlice.Position + screenOffset + (pieSlice.WhenBlockSize)/2;
                if (pieSlice.IconTexture != null) // move text below icon
                {
                    textPosition += new Vector2(0.0f, pieSlice.IconSize);
                    textPosition.Y -= (float)(pieSlice.WhenBlockSize.Y / 2.0f);
                }
                textPosition.X -= (textWidth / 2);

                TextHelper.DrawStringNoBatch(pieSlice.font, pieSlice.label, textPosition, renderColor);
            }

        }
    }
}
