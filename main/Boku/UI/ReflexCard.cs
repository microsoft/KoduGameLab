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

using KoiX;
using KoiX.Input;
using KoiX.Text;

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.Audio;
using Boku.Common.Xml;

namespace Boku.UI
{
    public delegate void ReflexCardChangeEvent(ReflexCard reflexCard, ProgrammingElement pendingCard);

    public class ReflexCard : GameObject, IControl, ITransform
    {
        /// <summary>
        /// The most recent card selected via pie menu.  We need
        /// this here because endingCard sometimes gets overwritten
        /// before we can check it in Update().  Yes, this is a bit 
        /// of a hack.
        /// Actually this is worse than I thought.  This works ok
        /// for editing existing patterns cards but fails when new
        /// cards are added.  The problem is that at the time this
        /// set, it is the prototype card from the pie menu.  Before
        /// it gets to the reflex it gets cloned.
        /// </summary>
        public static ProgrammingElement LastPickedCard;

        public const string idSensor = "sensor";
        public const string idFilter = "filter";
        
        public const string idSelector = "selector";
        public const string idModifier = "modifier";
        public const string idActuator = "actuator";
        public const string idNoFrame = "null_tile";

        public const string idPlus = "plus";
        protected const int indexNoChange = -2;
        protected const int indexNullCard = -1;
        private const int kMaxMicrobitSayLength = 45;//Max chars. TODO. Increase when we fix long text clipping bug.

        public class UpdateObjEditCards : UpdateObject
        {
            private ReflexCard parent = null;
            public List<UpdateObject> updateList = null; // Children's update list.

            private CommandMap commandMap;

            public UpdateObjEditCards(ReflexCard parent /*, ref Shared shared*/)
            {
                this.parent = parent;

                commandMap = new CommandMap(@"ReflexCard");

                updateList = new List<UpdateObject>();
            }

            /// <summary>
            /// Used to catch transitions back from pie menu.
            /// </summary>
            private static bool inPieMenu = false;

            public override void Update()
            {
                // Update the parent's list of objects.
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = updateList[i] as UpdateObject;
                    Debug.Assert(obj != null);
                    obj.Update();
                }

                // Check for input focus.
                if (commandMap == CommandStack.Peek())
                {
                    GamePadInput pad = GamePadInput.GetGamePad0();

                    // If true that means we've just come back from the pie menu.
                    if (inPieMenu)
                    {
                        // This whole sectyion os too much of a hack.  Basically what we're doing
                        // is after selecting a tile via the pie menu we then try and figure out
                        // what that tile is and if it needs any special treatment.  For instance,
                        // selecting the Say tile should bring up the text editor.  Selecting one
                        // of the Settings tiles should bring up the slider/selector for that value.

                        VerbActuator verbActuator = parent.pendingCard as VerbActuator;
                        if (verbActuator == null)
                        {
                            verbActuator = ReflexCard.LastPickedCard as VerbActuator;
                        }
                        Modifier modifier = ReflexCard.LastPickedCard as Modifier;
                        Filter filter = parent.pendingCard as Filter;

                        // If the tile we just chose was the say verb or the microbit say verb then 
                        // automatically activate the text editor.
                        if (verbActuator != null && (verbActuator.upid == "actuator.say" && prevUpid == "actuator.say"))
                        {
                            InGame.inGame.shared.textEditor.Activate(parent.reflex.Data, "say", useRtCoords: false);
                        }
                        if (verbActuator != null && (verbActuator.upid == "actuator.microbit.say" && prevUpid == "actuator.microbit.say"))
                        {
                            TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                            {
                                if (!canceled && newText.Length > 0)
                                {
                                    parent.reflex.Data.sayString = newText;
                                }
                            };
                            TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                            {
                                //Deterimine if text will fit.
                                bool valid = textBlob.RawText.Length <= kMaxMicrobitSayLength;
                                return valid;
                            };
                            InGame.inGame.shared.textLineDialog.Activate(callback, parent.reflex.Data.sayString, validateCallback);
                        }
                        else if (modifier != null && modifier.upid == "modifier.microbit.pattern")
                        {
                            // Adding a new tile.  Pass in null to indicate it's the last one we care about since modifier is actually the one from the pie menu.
                            InGame.inGame.shared.microbitPatternEditor.Activate(parent.reflex.Data, null);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.rescale" && prevUpid == "actuator.rescale")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.ReScale);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.rescaleinstant" && prevUpid == "actuator.rescaleinstant")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.ReScale);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.holddistance" && prevUpid == "actuator.holddistance")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.HoldDistance);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.holddistanceinstant" && prevUpid == "actuator.holddistanceinstant")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.HoldDistance);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.maxhitpoints" && prevUpid == "actuator.maxhitpoints")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MaxHitPoints);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.blipdamage" && prevUpid == "actuator.blipdamage")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamInt = InGame.inGame.Editor.GameActor.BlipDamage;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipDamage);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.missiledamage" && prevUpid == "actuator.missiledamage")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamInt = InGame.inGame.Editor.GameActor.MissileDamage;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileDamage);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.movementspeedmodify" && prevUpid == "actuator.movementspeedmodify")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MovementSpeedModifier);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.turningspeedmodify" && prevUpid == "actuator.turningspeedmodify")
                        {
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.TurningSpeedModifier);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.worldlightingchange" && prevUpid == "actuator.worldlightingchange")
                        {
                            InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.LightRig);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.worldlightingchangeinstant" && prevUpid == "actuator.worldlightingchangeinstant")
                        {
                            InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.LightRig);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.worldskychange" && prevUpid == "actuator.worldskychange")
                        {
                            InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.Sky);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.worldskychangeinstant" && prevUpid == "actuator.worldskychangeinstant")
                        {
                            InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.Sky);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.nextlevel" && prevUpid == "actuator.nextlevel")
                        {
                            InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.NextLevel);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.blipreloadtime" && prevUpid == "actuator.blipreloadtime")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.BlipReloadTime;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipReloadTime);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.bliprange" && prevUpid == "actuator.bliprange")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.BlipRange;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipRange);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.missilereloadtime" && prevUpid == "actuator.missilereloadtime")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.MissileReloadTime;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileReloadTime);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.missilerange" && prevUpid == "actuator.missilerange")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.MissileRange;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileRange);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.closebyrange" && prevUpid == "actuator.closebyrange")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.NearByDistance;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.NearByDistance);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.farawayrange" && prevUpid == "actuator.farawayrange")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.FarAwayDistance;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.FarAwayDistance);
                        }
                        else if (verbActuator != null && verbActuator.upid == "actuator.hearingrange" && prevUpid == "actuator.hearingrange")
                        {
                            // Adding tile, start with existing value.
                            parent.reflex.ParamFloat = InGame.inGame.Editor.GameActor.Hearing;
                            InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.Hearing);
                        }

                        // If the tile we just chose was the say verb then 
                        // automatically activate the text editor.
                        if (filter != null && filter.upid == "filter.said" && prevUpid == "filter.said")
                        {
                            InGame.inGame.shared.textEditor.Activate(parent.reflex.Data, "said", useRtCoords: false);
                        }

                        // Another "delayed refresh" hack.  The problem is that even after the
                        // pie menu is activated this object still gets a couple of update calls
                        // before the pie menu takes control.  So, we don't clear the inPieMenu
                        // flag until the pie menu is truly gone.
                        if (parent.cardSelector == null)
                        {
                            inPieMenu = false;
                        }
                    }

                    if (Actions.Select.WasPressed)
                    {
                        Actions.Select.ClearAllWasPressedState();
                        // TODO (****) Roll the frame delay into the above call?
                        GamePadInput.ClearAllWasPressedState(3);

                        ActivatePieSelector();
                    }

                    // Cut
                    if (Actions.ProgrammingEditorCut.WasPressed)
                    {
                        Actions.ProgrammingEditorCut.ClearAllWasPressedState();
                        GamePadInput.ClearAllWasPressedState(3);

                        // If the pending card is the NullSelector or NullSelector or NullActuator or NullSensor
                        // that means we're on one of the + signs.  So, move cursor left to valid card before deleting.
                        if (parent.pendingCard is NullSelector || parent.pendingCard is NullFilter || parent.pendingCard is NullActuator || parent.pendingCard is NullSensor)
                        {
                            InGame.inGame.Editor.NavCardPrev(null, null);
                        }

                        // NOTE: if the above path is take and NavCardPrev is called then the following call has no effect.
                        // I'm not sure why and I'm probably happier not knowing.

                        parent.ClearCard(null, null);
                    }

                    /*
                    // Replaced w/ auto blank line feature.
                    if(pad.ButtonY.WasPressed)
                    {
                        parent.InsertReflex(null, null);

                        GamePadInput.ClearAllWasPressedState(3);
                    }
                    */

                }

            }   // end of Update()

            /// <summary>
            /// Brings up the pie menu for a programming card.  
            /// Normally when you select an existing tile
            /// in the programming UI and press A the pie menu comes up and you
            /// can then replace the existing tile.  However, for the 'say'
            /// verb and 'said filter tiles we want it to bring up the text editor 
            /// so that the text associated with the verb can be edited by the user.
            /// Also, for the Micro Bit Pattern tiles we want to bring up the pattern editor.
            /// </summary>
            public void ActivatePieSelector()
            {
                ReflexCard.LastPickedCard = null;

                Actuator actuator = parent.pendingCard as Actuator;
                Filter filter = parent.pendingCard as Filter;
                Modifier modifier = parent.Card as Modifier;

                if (actuator != null && actuator is VerbActuator && (((actuator as VerbActuator).Verb == GameThing.Verbs.Say)))
                {
                    InGame.inGame.shared.textEditor.Activate(parent.reflex.Data, "say", useRtCoords: false);
                }
                else if (actuator != null && actuator is VerbActuator && ((actuator as VerbActuator).Verb == GameThing.Verbs.MicrobitSay))
                {
                    TextLineDialog.OnDialogDone callback = delegate(bool canceled, string newText)
                    {
                        if (!canceled && newText.Length > 0)
                        {
                            parent.reflex.Data.sayString = newText;
                        }
                    };
                    TextLineEditor.ValidateText validateCallback = delegate(TextBlob textBlob)
                    {
                        //Deterimine if text will fit.
                        bool valid = textBlob.RawText.Length <= kMaxMicrobitSayLength;
                        return valid;
                    };
                    InGame.inGame.shared.textLineDialog.Activate(callback, parent.reflex.Data.sayString, validateCallback);
                }
                else if (filter != null && filter.upid == "filter.said")
                {
                    InGame.inGame.shared.textEditor.Activate(parent.reflex.Data, "said", useRtCoords: false);
                }
                else if (modifier != null && modifier.upid == "modifier.microbit.pattern")
                {
                    // We have no easy way to figure out which tile was clicked.  Crap.
                    int index = ((ReflexPanel)(parent.parent)).ActiveCard;
                    // This "-3" relies on knowing the internal layout of the reflex.  Kind of a hack.
                    index -= 3;
                    // Account for sensor.
                    if (parent.reflex.Sensor != null)
                    {
                        --index;
                    }
                    // Account for any filter tiles.
                    index -= parent.reflex.Data.GetNonHiddenDefaultFilterCount();
                    modifier = parent.reflex.Modifiers[index];
                    InGame.inGame.shared.microbitPatternEditor.Activate(parent.reflex.Data, modifier);
                }
                else
                {
                    // Handle tile which bring up an options slider or selector.
                    if(actuator != null && actuator is VerbActuator)
                    {
                        VerbActuator va = actuator as VerbActuator;
                        switch(va.Verb)
                        {
                            case GameThing.Verbs.ReScale:
                            case GameThing.Verbs.ReScaleInstant:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.ReScale);
                                break;

                            case GameThing.Verbs.HoldDistance:
                            case GameThing.Verbs.HoldDistanceInstant:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.HoldDistance);
                                break;

                            case GameThing.Verbs.MaxHitpointsChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MaxHitPoints);
                                break;

                            case GameThing.Verbs.BlipDamageChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipDamage);
                                break;

                            case GameThing.Verbs.MissileDamageChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileDamage);
                                break;

                            case GameThing.Verbs.MovementSpeedModify:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MovementSpeedModifier);
                                break;
                            
                            case GameThing.Verbs.TurningSpeedModify:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.TurningSpeedModifier);
                                break;

                            case GameThing.Verbs.WorldLightingChange:
                            case GameThing.Verbs.WorldLightingChangeInstant:
                                InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.LightRig);
                                break;
                            
                            case GameThing.Verbs.WorldSkyChange:
                            case GameThing.Verbs.WorldSkyChangeInstant:
                                InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.Sky);
                                break;

                            case GameThing.Verbs.NextLevel:
                                InGame.inGame.shared.editWorldParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditWorldParameters.Control.NextLevel);
                                break;

                            case GameThing.Verbs.BlipReloadTimeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipReloadTime);
                                break;

                            case GameThing.Verbs.BlipRangeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.BlipRange);
                                break;

                            case GameThing.Verbs.MissileReloadTimeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileReloadTime);
                                break;

                            case GameThing.Verbs.MissileRangeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.MissileRange);
                                break;

                            case GameThing.Verbs.CloseByRangeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.NearByDistance);
                                break;
                            
                            case GameThing.Verbs.FarAwayRangeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.FarAwayDistance);
                                break;

                            case GameThing.Verbs.HearingRangeChange:
                                InGame.inGame.shared.editObjectParameters.Activate(parent.reflex.Data, parent.reflex.Task.GameActor, EditObjectParameters.Control.Hearing);
                                break;

                            default:
                                // Activate pie menu.
                                parent.EditCard(null, null);
                                inPieMenu = true;
                                break;
                        }
                    }
                    else
                    {
                        // Activate pie menu.
                        parent.EditCard(null, null);
                        inPieMenu = true;
                    }
                }

            }   // end of ActivatePieSelector()

            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
            }
        }


        public CardSpace.CardType cardType;
        public bool morphable = false;
        public ControlRenderObj renderClause;
        public event ReflexCardChangeEvent Change;
        public Matrix Anchor()
        {
            return Anchor(false);
        }
        public Matrix Anchor( bool inverted )
        {
            Matrix anchor;
            if (!renderObj.GetPositionTransform("tile anchor", out anchor, inverted))
            {
                Debug.Assert(false);
            }
            return anchor;
        }
        public ControlRenderObj renderObj;

        // this is coded to support multiple update objects
        // even though its not really used.  It is being left in place
        // for future use rather than removing it
        public UpdateObject updateObj; // active update object
        protected UpdateObject updateObjPending;
        public UpdateObjEditCards updateObjEditCards;

        private enum States
        {
            Inactive,
            Active,
            Hot,
            Disabled,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        #region Accessors
        public Reflex Reflex
        {
            get { return reflex; }
        }
        private States State
        {
            get { return state; }
            set { state = value; }
        }

        private States PendingState
        {
            get { return pendingState; }
            set 
            { 
                pendingState = value;
                if (pendingState == States.Hot && InGame.inGame.Editor.IndexActivePanel != -1)
                {
                    UiCursor.ActiveCursor.Parent = this;
                }
            }
        }
        #endregion


        private Reflex reflex;
        private ProgrammingElement card;
        
        private ControlCollection controls;
        private UiSelector cardSelector;
        private ProgrammingElement pendingCard;
        private Object parent;

        public PieSelector CardSelector
        {
            get { return cardSelector as PieSelector; }
        }

        public ReflexCard(Object parent, Reflex reflex, ProgrammingElement card, CardSpace.CardType cardType, ControlCollection controls)
        {
            this.parent = parent;
            this.reflex = reflex;
            this.card = card;
            this.pendingCard = card;
            this.controls = controls;
            this.cardType = cardType;

            renderObj = InstanceRenderCard(parent, controls, cardType, card.upid, null);

            updateObjEditCards = new UpdateObjEditCards(this);

            updateObjPending = updateObjEditCards;
        }

        public ProgrammingElement Card
        {
            get { return this.card; }
        }

        protected ControlRenderObj InstanceRenderCard(Object parent, ControlCollection controls, CardSpace.CardType cardType, string id, string idFrame )
        {
            ControlRenderObj renderObj = null;
            if (id == ProgrammingElement.upidNull)
            {
                renderObj = controls.InstanceControlRenderObj(parent, idPlus);
            }
            else
            {
                // once we have a good model with a null frame then uncomment this
                // if (idFrame == null)
                {
                    if (cardType == CardSpace.CardType.Sensor)
                    {
                        idFrame = idSensor;
                    }
                    else if (cardType == CardSpace.CardType.Filter)
                    {
                        idFrame = idFilter;
                    }
                    else if (cardType == CardSpace.CardType.Selector)
                    {
                        idFrame = idSelector;
                    }
                    else if (cardType == CardSpace.CardType.Modifier)
                    {
                        idFrame = idModifier;
                    }
                    else if (cardType == CardSpace.CardType.Actuator)
                    {
                        idFrame = idActuator;
                    }
                }
                renderObj = controls.InstanceControlRenderObj(parent, idFrame);
                // fixup part infos lists for custom textures
                if (renderObj != null)
                {
                    AffixCardFaceToStaticRenderCard(renderObj, id);
                }
            }
            return renderObj;
        }

        protected void AffixCardFaceToStaticRenderCard(ControlRenderObj renderObj, string id)
        {
            // instance the list
            renderObj.listStaticPartInfos = renderObj.ListStaticPartInfos;
            // if we have a static part info
            if (renderObj.listStaticPartInfos != null)
            {
                Texture2D cardFace = CardSpace.Cards.CardFaceTexture(id);

                // walk it and update it
                // the first mesh (one and only)
                List<PartInfo> meshPartInfos = renderObj.listStaticPartInfos[0];
                // the second part is the face, replace the texture
                meshPartInfos[1].OverlayTexture = cardFace;
            }
        }

        protected void AffixCardFaceToCurrentStateRenderCard(ControlRenderObj renderObj, string id)
        {
            // instance the list
            renderObj.listActivePartInfos = renderObj.ListActivePartInfos;
            if (renderObj.listActivePartInfos != null)
            {
                Texture2D cardFace = CardSpace.Cards.CardFaceTexture(id);

                // walk it and update it
                // the first mesh (one and only)
                List<PartInfo> meshPartInfos = renderObj.listActivePartInfos[0];
//                int indexMeshPart = Math.Min( 1, meshPartInfos.Count - 1 );
                // the second part is the face, replace the texture
                meshPartInfos[1].OverlayTexture = cardFace;
            }
        }

        // reflect ITransform into the renderObj
        Transform ITransform.Local
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.Local;
            }
            set
            {
                ITransform transform = renderObj as ITransform;
                transform.Local = value;
            }
        }

        Matrix ITransform.World
        {
            get
            {
                ITransform transform = renderObj as ITransform;
                return transform.World;
            }
        }

        bool ITransform.Compose()
        {
            ITransform transform = renderObj as ITransform;
            return transform.Compose();
        }

        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            ITransform transform = renderObj as ITransform;
            transform.Recalc(ref parentMatrix);
            if (UiCursor.ActiveCursor != null && UiCursor.ActiveCursor.Parent == this)
            {
                ITransform transformCursor = UiCursor.ActiveCursor as ITransform;
                Matrix world = transform.World;
                transformCursor.Recalc(ref world);
            }
        }

        ITransform ITransform.Parent
        {
            get { return this.parent as ITransform; }
            set { this.parent = value; }
        }

        public void EditCards(UiSelector selector)
        {
            this.cardSelector.Deactivate();
            UiCursor.ActiveCursor.Activate();

            // adjust our state
            IControl cardControl = this as IControl;
            cardControl.Hot = true;
        }

        protected RenderObject ComposeDefault(UiSelector selector, Object item, Object param, int indexItem)
        {
            ControlRenderObj renderObjCard;
            string id;
            if (item == null)
            {
                id = this.card.upid;
            }
            else
            {
                ProgrammingElement progElement = param as ProgrammingElement;
                id = progElement.upid;
            }
            renderObjCard = InstanceRenderCard(cardSelector, controls, cardType, id, idNoFrame);
            renderObjCard.State = ControlRenderObj.idStateNormal;
            AffixCardFaceToCurrentStateRenderCard(renderObjCard, id);
            return renderObjCard;
        }

        static List<ProgrammingElement> cardPieces = new List<ProgrammingElement>();
        public static bool HasSelectableElements(Reflex reflex, CardSpace.CardType cardType)
        {
            CardSpace.Cards.Pieces(cardType, cardPieces);
            int hiddenDefaultsPresent = 0;
            int supportedCards = 0;
            for (int iCard = 0; iCard < cardPieces.Count; iCard++)
            {
                ProgrammingElement progElement = cardPieces[iCard] as ProgrammingElement;
                if (!progElement.archived)
                {
                    if (progElement.ReflexCompatible(reflex, null, false))
                    {
                        if (progElement != null && !progElement.ActorCompatible(reflex.Task.GameActor))
                        {
                            // skip it 
                        }
                        else
                        {
                            if (progElement.hiddenDefault)
                            {
                                hiddenDefaultsPresent++;
                            }
                            
                            supportedCards++;
                        }
                    }
                }
            }

            // check if the only available card(s) is the hidden defaults ("nearest" & "camera relative") card and remove it if it is
            //
            if (supportedCards == 0 || (supportedCards == hiddenDefaultsPresent))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Used as an intermediate object when constructing pie menus.
        /// </summary>
        private class PieNode
        {
            // A sequence for assigning indices to nodes when adding as children.
            public int buildIndex;
            // So we know the order in which the child nodes were added to a parent.
            public int myIndex;
            // The level of sub-menu this node represents.
            public int depth;
            // The UI Selector we're bulding up at this level.
            public UiSelector groupSelector;
            // The CardSpace group the selector we're building represents.
            public CardSpace.Group groupObj;
            // The data thing that is drawn, or something. I'm not sure. Legacy, but not depricated.
            public UiSelector.ItemData item;
            // Our set of child nodes.
            public Dictionary<string, PieNode> children = new Dictionary<string, PieNode>();

            /// <summary>
            /// True if this node is a menu that could be absorbed into a parent menu if needed.
            /// </summary>
            public bool IsBalancable
            {
                get
                {
                    // it's a leaf node
                    if (IsLeaf)
                        return false;

                    // not a pie selector group
                    if (groupObj.notPieSelector)
                        return false;

                    // not available for balancing based on current number of items.
                    if (children.Count > groupObj.minExpandElements)
                        return false;

                    return true;
                }
            }

            public bool IsLeaf
            {
                get { return item != null; }
            }

            public PieNode(CardSpace.Group groupObj, UiSelector.ItemData item)
            {
                this.groupObj = groupObj;
                this.item = item;
            }
        }

        public void EditCard(Object sender, EventArgs args)
        {
            EditCardUsingPieMenu(sender, args);
        }

        private void BuildCompatibleNodeTree(CardSpace.CardType effectiveCardType, Dictionary<string, PieNode> groupNodeTable)
        {
            CardSpace.Cards.Pieces(effectiveCardType, cardPieces);

            for (int iCard = 0; iCard < cardPieces.Count; iCard++)
            {
                ProgrammingElement progElement = cardPieces[iCard] as ProgrammingElement;

                // Don't show archived elements.
                if (progElement.archived)
                    continue;

                // Don't show hidden elements.
                if (progElement.hiddenDefault)
                    continue;

                // Don't show elements incompatible with the actor we're programming.
                if (!progElement.ActorCompatible(this.reflex.Task.GameActor))
                    continue;

                // Don't show elements incompatible with this one.
                if (!progElement.ReflexCompatible(this.reflex, this.card, false))
                    continue;

#if !NETFX_CORE                
                // Don't show microbit tiles if settings disallows it.
                if (!XmlOptionsData.ShowMicrobitTiles && MicrobitExtras.IsMicrobitTile(progElement))
                    continue;
#else
                if (MicrobitExtras.IsMicrobitTile(progElement))
                    continue;
#endif 

                // If this is an inline, we need to do a recursion check to not allow
                // any pages that could cause a loop.
                if (reflex.actuatorUpid == "actuator.inlinetask")
                {
                    // Start with array of false for each task.
                    bool[] touched = new bool[Brain.kCountDefaultTasks];
                    for (int i = 0; i < Brain.kCountDefaultTasks; i++)
                    {
                        touched[i] = false;
                    }

                    Brain brain = reflex.Task.Brain;

                    TaskModifier tm = progElement as TaskModifier;
                    if (tm != null)
                    {
                        int target = (int)tm.taskid;
                        int cur = brain.ActiveTaskId;

                        bool valid = IsValidInline(brain, cur, target, touched);
                        if (!valid)
                            continue;
                    }
                }

                // See if we've already created a group node matching the group the program element is in.
                PieNode groupNode;
                if (!groupNodeTable.TryGetValue(progElement.group, out groupNode))
                {
                    // Group doesn't exist, create it.
                    groupNode = new PieNode(progElement.groupObj, null);
                    groupNodeTable.Add(progElement.group, groupNode);

                    // Ensure the group node's heirarchy exists back to the root group.
                    PieNode parentGroupNode;
                    string parentGroupNodeName = groupNode.groupObj.group;
                    while (!groupNodeTable.TryGetValue(parentGroupNodeName, out parentGroupNode))
                    {
                        parentGroupNode = new PieNode(CardSpace.Cards.GetGroup(parentGroupNodeName), null);
                        groupNodeTable.Add(parentGroupNodeName, parentGroupNode);
                        parentGroupNodeName = parentGroupNode.groupObj.group;
                        groupNode.myIndex = parentGroupNode.buildIndex++;
                        parentGroupNode.children.Add(groupNode.groupObj.group, groupNode);
                        groupNode = parentGroupNode;
                    }

                    // Re-fetch the new group, and also its parent group.
                    groupNode = groupNodeTable[progElement.group];
                    parentGroupNode = groupNodeTable[groupNode.groupObj.group];
                    if (!parentGroupNode.children.ContainsKey(groupNode.groupObj.upid))
                    {
                        // Ensure this group is linked to its parent group.
                        groupNode.myIndex = parentGroupNode.buildIndex++;
                        parentGroupNode.children.Add(groupNode.groupObj.upid, groupNode);
                    }
                }

                // Create the render thing.
                ControlRenderObj renderObjCard = InstanceRenderCard(cardSelector, controls, effectiveCardType, progElement.upid, idNoFrame);
                renderObjCard.State = ControlRenderObj.idStateNormal;
                AffixCardFaceToCurrentStateRenderCard(renderObjCard, progElement.upid);

                // Create the data thing.
                UiSelector.ItemData itemData = new UiSelector.ItemData(renderObjCard, progElement);

                // Add the programming element as a leaf node to its group node.
                PieNode itemNode = new PieNode(null, itemData);
                itemNode.myIndex = groupNode.buildIndex++;

                if (!groupNode.children.ContainsKey(progElement.upid))
                    groupNode.children.Add(progElement.upid, itemNode);
            }
        }

        /// <summary>
        /// Recursive test to see whether adding an call to inline is valid.
        /// </summary>
        /// <param name="brain">Current brain we're working in.</param>
        /// <param name="cur">The page we want to inline.</param>
        /// <param name="target">The page we're starting on.  If we get back here we've got a loop.</param>
        /// <param name="touched">Array of bools telling us which tasks we've already tested.</param>
        /// <returns></returns>
        public static bool IsValidInline(Brain brain, int cur, int target, bool[] touched)
        {
            if (cur == target)
                return false;

            touched[target] = true;

            // Loop through all reflexes in target page.  Recurse on all untouched inlined pages.
            Task task = (Task)brain.tasks[target];
            for (int i = 0; i < task.reflexes.Count; i++)
            {
                Reflex reflex = task.reflexes[i] as Reflex;
                if (reflex != null 
                    && reflex.actuatorUpid == "actuator.inlinetask" 
                    && reflex.modifierUpids.Length > 0 
                    && reflex.modifierUpids[0].StartsWith("modifier.task"))
                {
                    int newTarget = reflex.modifierUpids[0].Substring("modifier.task".Length)[0] - 'a';
                    if (!touched[newTarget])
                    {
                        if (!IsValidInline(brain, cur, newTarget, touched))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }   // end of IsValidInline

        public void EditCardUsingPieMenu(Object sender, EventArgs args)
        {
            CardSpace.CardType effectiveCardType = cardType;

            if (this.cardType == CardSpace.CardType.Selector &&
                reflex.Selector != null &&
                this.card.upid != reflex.Selector.upid &&
                !reflex.Selector.hiddenDefault)
            {
                effectiveCardType = CardSpace.CardType.Modifier;
            }

            if (this.cardType == CardSpace.CardType.Selector &&
                this.card is NullSelector &&
                this.reflex.Modifiers.Count > 0)
            {
                effectiveCardType = CardSpace.CardType.Modifier;
            }

            // adjust our state
            IControl cardControl = this as IControl;
            cardControl.Hot = false;

            // Create the root node of the tree structure that will contain all compatible tiles in a heirarchy by group.
            CardSpace.Group rootGroupObj = CardSpace.Cards.GetGroup(CardSpace.Group.RootGroup);
            PieNode rootGroupNode = new PieNode(rootGroupObj, null);

            // A dictionary of group nodes so that they can be looked up while building the tree.
            Dictionary<string, PieNode> groupNodeTable = new Dictionary<string, PieNode>();
            groupNodeTable.Add(rootGroupNode.groupObj.upid, rootGroupNode);

            // Build the unaltered heirarchy of tiles, as specified in CardSpace.
            BuildCompatibleNodeTree(effectiveCardType, groupNodeTable);

            if (effectiveCardType == CardSpace.CardType.Selector)
            {
                // For the selector menu, also pull in modifiers.
                BuildCompatibleNodeTree(CardSpace.CardType.Modifier, groupNodeTable);
            }

            // If the only item in the root group is a sub-menu. Make that sub-menu the root. Unless, of course,
            // the single sub-menu is not a pie selector. In that case we want to show the single pie that leads
            // you to the not-pie.
            if (rootGroupNode.children.Count == 1)
            {
                List<PieNode> childList = new List<PieNode>(rootGroupNode.children.Values);
                if (!childList[0].IsLeaf && !childList[0].groupObj.notPieSelector)
                    rootGroupNode = childList[0];
            }

            // Combine and regroup tiles as necessary to achieve optimal pie size at each tree level.
            BalancePieTreeRecurse(rootGroupNode);

            // Create the root UiSelector
            if (rootGroupNode.groupObj.notPieSelector)
            {
                rootGroupNode.groupSelector = new NotPieSelector(this, "EditBrain.Pick" + effectiveCardType);
                (rootGroupNode.groupSelector as NotPieSelector).Name = CardSpace.Group.RootGroup;
            }
            else
            {
                rootGroupNode.groupSelector = new PieSelector(this, "EditBrain.Pick" + effectiveCardType);
                (rootGroupNode.groupSelector as PieSelector).Name = CardSpace.Group.RootGroup;
            }

            // Now that we have the final form of the tree, use it to build the pie selector heirarchy.
            BuildSelectorTreeRecurse(rootGroupNode, 0);

            if (rootGroupNode.children.Count == 0)
            {
                Foley.PlayCut();

                // reset back to hot
                cardControl.Hot = true;
            }
            else
            {
                this.cardSelector = rootGroupNode.groupSelector;
                this.cardSelector.ComposeDefault = ComposeDefault;

                ITransform transformSelector = this.cardSelector as ITransform;
                if (transformSelector != null)
                {
                    transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, (float)rootGroupNode.depth * 0.3f);
                    transformSelector.Compose();
                }

                this.cardSelector.Select += SelectCard;
                this.cardSelector.Cancel += CancelCard;

                Foley.PlayPressA();
                UiCursor.ActiveCursor.Deactivate();
                this.cardSelector.Activate();
            }
        }

        private int ComparePieNodeGroupSizesAsc(PieNode x, PieNode y)
        {
            return x.children.Count.CompareTo(y.children.Count);
        }

        private int ComparePieNodeGroupSizesDesc(PieNode x, PieNode y)
        {
            return y.children.Count.CompareTo(x.children.Count);
        }

        private int ComparePieNodeGroupIndiciesAsc(PieNode x, PieNode y)
        {
            return x.myIndex.CompareTo(y.myIndex);
        }

        private int ComparePieNodeGroupIndiciesDesc(PieNode x, PieNode y)
        {
            return y.myIndex.CompareTo(x.myIndex);
        }

        private void BalancePieTreeRecurse(PieNode groupNode)
        {
            // This is a leaf node.
            if (groupNode.item != null)
                return;

            // Do not traverse not-pie-selector branches.
            if (groupNode.groupObj.notPieSelector)
                return;

            int targetPieMenuSize = GetOptimalPieMenuSize();

            // First pass, absorb sub-menus starting with the largest and working down to smaller ones, as long as we have room.
            int vacantSlots = targetPieMenuSize - (groupNode.children.Values.Count - 1);
            if (vacantSlots > 0)
            {
                List<PieNode> childList = new List<PieNode>(groupNode.children.Values);

                // Sort by group size, high to low.
                childList.Sort(ComparePieNodeGroupSizesDesc);

                for (int i = 0; i < childList.Count; ++i)
                {
                    PieNode node = childList[i];

                    // Would the node let us absorb its children if we wanted to?
                    if (!node.IsBalancable)
                        continue;

                    // Absorb the sub-menu if we have space. Allow a small amount of slop in the vacant slots
                    // to allow some sub-menus to spill in to the 'more' dynamic group (later on in this function).
                    if (node.children.Count <= vacantSlots + 2)
                    {
                        vacantSlots -= node.children.Count;

                        // Remove the child node containing the sub-menu to be absorbed, since we will be adding its children to the current menu.
                        groupNode.children.Remove(node.groupObj.upid);

                        // Reassign the absorbed group's children to the parent, reindexing them so they will draw next to each other at the end of the menu.
                        Dictionary<string, PieNode>.Enumerator nodeIter = node.children.GetEnumerator();
                        while (nodeIter.MoveNext())
                        {
                            nodeIter.Current.Value.myIndex = groupNode.buildIndex++;
                            groupNode.children.Add(nodeIter.Current.Key, nodeIter.Current.Value);
                        }
                    }

                    // pie full?
                    if (vacantSlots <= 0)
                        break;
                }
            }

            // Second pass, absorb all single-item sub-menus because they look funny. If they overflow the optimal size,
            // we'll push them into the dynamic 'more' group at later in this function.
            {
                List<PieNode> childList = new List<PieNode>(groupNode.children.Values);

                // Sort by group size, low to high.
                childList.Sort(ComparePieNodeGroupSizesAsc);

                for (int i = 0; i < childList.Count; ++i)
                {
                    PieNode node = childList[i];

                    // Would the node let us absorb its children if we wanted to?
                    if (!node.IsBalancable)
                        continue;

                    // Look for single-item sub-menus and absorb them.
                    if (node.children.Count == 1)
                    {
                        // Remove the node containing the sub-menu to be absorbed.
                        groupNode.children.Remove(node.groupObj.upid);

                        // Reassign the group's children to the parent, reindexing them so they will draw next to each other at the end of the menu.
                        Dictionary<string, PieNode>.Enumerator nodeIter = node.children.GetEnumerator();
                        while (nodeIter.MoveNext())
                        {
                            nodeIter.Current.Value.myIndex = groupNode.buildIndex++;
                            groupNode.children.Add(nodeIter.Current.Key, nodeIter.Current.Value);
                        }
                    }
                }
            }

            // If necessary, dynamically create a new overflow sub-menu, adding just enough elements to bring us down to the optimal number of items.
            const int kTargetPieSizeSlop = 1;
            if (groupNode.children.Count > targetPieMenuSize + kTargetPieSizeSlop)
            {
                PieNode dynamicGroupNode = new PieNode(CardSpace.Cards.GetGroup("group.more"), null);

                // Add items in reverse index order, skipping sub-menus.
                List<PieNode> childList = new List<PieNode>(groupNode.children.Values);
                childList.Sort(ComparePieNodeGroupIndiciesDesc);

                for (int i = 0; i < childList.Count; ++i)
                {
                    PieNode node = childList[i];

                    if (!node.IsLeaf)
                        continue;

                    node.buildIndex = dynamicGroupNode.buildIndex++;
                    dynamicGroupNode.children.Add(node.item.progElement.upid, node);

                    if (groupNode.children.Count - dynamicGroupNode.children.Count == targetPieMenuSize - 1)
                        break;
                }

                // If we harvested enough overflow items. Make it a sub-menu.
                if (dynamicGroupNode.children.Count > kTargetPieSizeSlop)
                {
                    Dictionary<string, PieNode>.Enumerator nodeIter = dynamicGroupNode.children.GetEnumerator();

                    while (nodeIter.MoveNext())
                    {
                        groupNode.children.Remove(nodeIter.Current.Key);
                    }
                }

                dynamicGroupNode.myIndex = groupNode.buildIndex++;
                groupNode.children.Add(dynamicGroupNode.groupObj.upid, dynamicGroupNode);
            }

            // Balance sub-menus of this node.
            {
                List<PieNode> childList = new List<PieNode>(groupNode.children.Values);
                for (int i = 0; i < childList.Count; ++i)
                {
                    BalancePieTreeRecurse(childList[i]);
                }
            }

            // Reorder items in this menu, pushing the 'more' group to the end and ensuring we don't have a menu group
            // as the first item if possible to avoid it.
            {
                groupNode.buildIndex = 0;

                List<PieNode> childList = new List<PieNode>(groupNode.children.Values);
                childList.Sort(ComparePieNodeGroupIndiciesAsc);
                
                for (int i = 0; i < childList.Count; ++i)
                {
                    PieNode node = childList[i];
                    node.myIndex = groupNode.buildIndex++;

                    // Push 'more' group to the end.
                    if (!node.IsLeaf && node.groupObj.upid == "group.more")
                        node.myIndex += 150;
                }

                childList.Sort(ComparePieNodeGroupIndiciesAsc);

                // Push sub-menus out to the end, but placed before the 'more' group, until we hit a leaf node.
                for (int i = 0; i < childList.Count; ++i)
                {
                    PieNode node = childList[i];

                    if (node.IsLeaf)
                        break;

                    node.myIndex += 100;
                }
            }
        }

        private void BuildSelectorTreeRecurse(PieNode groupNode, int depth)
        {
            // Depth value controls draw ordering of pie selectors.
            groupNode.depth = depth + 1;

            // Get the node's children and sort by index ascending. The sort ensures they appear in the order listed in CardSpace.Xml (caveat: expanded sub-menus will appear at the end).
            List<PieNode> childList = new List<PieNode>(groupNode.children.Values);
            childList.Sort(ComparePieNodeGroupIndiciesAsc);

            for (int i = 0; i < childList.Count; ++i)
            {
                PieNode node = childList[i];

                if (node.item != null)
                {
                    // It's a leaf node, add it as an item to the selector.
                    groupNode.groupSelector.AddItem(node.item.item, node.item.progElement);
                }
                else if (node.children.Count > 0)
                {
                    // It's a sub-menu. Create a new selector for it and recurse down to populate.
                    node.groupSelector = BlankSelector(groupNode.groupSelector, node.depth, node.groupObj.upid);
                    BuildSelectorTreeRecurse(node, groupNode.depth);
                }
            }
        }

        private int GetOptimalPieMenuSize()
        {
            int result = BokuGame.IsWidescreen ? 12 : 10;
            result = Program2.CmdLine.GetInt("PIESIZE", result);
            result = (int)MathHelper.Clamp(result, 2, 100);
            return result;
        }

        private UiSelector BlankSelector(UiSelector parentSelector, float itemDepth, string upidGroup)
        {
            CardSpace.Group group = CardSpace.Cards.GetGroup(upidGroup);
            Billboard renderObjGroup = new Billboard(parentSelector, null, new Vector2(0.8f, 0.8f));
            renderObjGroup.Texture = CardSpace.Cards.CardFaceTexture(group.upid);
            ITransform transformRender = renderObjGroup as ITransform;
            if (transformRender != null)
            {
                transformRender.Local.Translation += new Vector3(0.0f, 0.0f, 0.25f);
                transformRender.Local.Compose();
            }

            UiSelector uiSelector = null;
            int indexItem = -1;
            if (!group.notPieSelector)
            {
                PieSelector pieSelector = new PieSelector(renderObjGroup, "EditBrain.Pick" + cardType + "." + upidGroup);
                pieSelector.Name = upidGroup;

                ITransform transformSelector = pieSelector as ITransform;
                transformSelector.Local.Translation = new Vector3(0.0f, 0.0f, itemDepth * 0.3f);
                transformSelector.Compose();

                pieSelector.ComposeDefault = ComposeDefault;
                pieSelector.Select += SelectCard;
                pieSelector.Cancel += CancelCard;
                indexItem = parentSelector.AddGroup(renderObjGroup, pieSelector);

                uiSelector = pieSelector;
            }
            else
            {
                NotPieSelector notPie = new NotPieSelector(parentSelector, "EditBrain.Pick" + cardType + "." + upidGroup);
                notPie.Name = upidGroup;

                notPie.ComposeDefault = ComposeDefault;
                notPie.Select += SelectCard;
                notPie.Cancel += CancelCard;
                indexItem = parentSelector.AddGroup(renderObjGroup, notPie);

                uiSelector = notPie;
            }

            // add some adornments since groups have no model of a tile
            UiSelector.ItemData itemData = parentSelector[indexItem];
            Billboard adornment = new Billboard(parentSelector, @"Textures\Tiles\empty_tile", new Vector2(1.0f, 1.0f));
            transformRender = adornment as ITransform;
            if (transformRender != null)
            {
                transformRender.Local.Translation += new Vector3(0.0f, 0.0f, 0.24f);
                transformRender.Local.Compose();
            }
            itemData.AddAdornment(adornment);

            return uiSelector;
        }

        public void SelectCard(UiSelector selector)
        {
            UiSelector.GroupData groupData = selector.SelectedItem as UiSelector.GroupData;
            if (groupData != null)
            {
                // sub group was activated
                Foley.PlayPressA();
            }
            else
            {
                // an item was activated
                Foley.PlayPressA();

                this.pendingCard = selector.ParamSelectedItem as ProgrammingElement;
                ReflexCard.LastPickedCard = this.pendingCard;
                if (this.Change != null)
                {
                    Change(this, this.pendingCard);
                }
                EditCards(selector);
            }
        }

        public void CancelCard(UiSelector selector)
        {
            Foley.PlayBack();

           if (selector == this.cardSelector)
            {
                EditCards(selector);
            }
        }

        protected void AffixCardFaceToCurrentState()
        {
            if (renderObj != null && this.card != null && this.card.upid != ProgrammingElement.upidNull)
            {
                Texture2D cardFace = CardSpace.Cards.CardFaceTexture(this.card.upid);
                // instance the list
                renderObj.listActivePartInfos = renderObj.ListActivePartInfos;

                if (renderObj.listActivePartInfos != null)
                {
                    // walk it and update it
                    // the first mesh (one and only)
                    List<PartInfo> meshPartInfos = renderObj.listActivePartInfos[0];
                    // the second part is the face, replace the texture
                    meshPartInfos[1].OverlayTexture = cardFace;
                }
            }
        }

        protected bool SwitchToNormal(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State == States.Inactive)
            {
                updateList.Add(updateObj);
                //updateObj.Activate();
                renderList.Add(renderObj);
                renderObj.Activate();
            }
            else if (State == States.Hot)
            {
                updateObj.Deactivate();
            }
            renderObj.State = ControlRenderObj.idStateNormal;
            AffixCardFaceToCurrentState();
            return result;
        }

        protected bool SwitchToInactive(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State != States.Inactive)
            {
                renderObj.Deactivate();
                renderList.Remove(renderObj);
                if(State == States.Hot)
                {
                    updateObj.Deactivate();
                }
                updateList.Remove(updateObj);
                result = true; // remove us
            }
            return result;
        }

        protected bool SwitchToHot(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (State == States.Inactive)
            {
                updateList.Add(updateObj);
//                updateObj.Activate();
                renderList.Add(renderObj);
                renderObj.Activate();
            }
            updateObj.Activate();

            renderObj.State = ControlRenderObj.idStateHot;
            AffixCardFaceToCurrentState();
            return result;
        }

        protected bool SwitchToDisabled(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            return result;
        }

        protected void ApplyCardChange(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            renderObj.Deactivate();
            renderList.Remove(renderObj);
            string upidPending;
            if (this.pendingCard == null)
            {
                //this.pendingCard = CardSpace.Cards.NullPiece(cardType);
                upidPending = ProgrammingElement.upidNull;
            }
            else
            {
                this.pendingCard = this.pendingCard.Clone();
                upidPending = this.pendingCard.upid;
            }

            // update the render object
            ControlRenderObj renderNewObj = InstanceRenderCard(parent, controls, cardType, upidPending, null);
            ITransform transformRender = renderObj as ITransform;
            ITransform transformNewRender = renderNewObj as ITransform;
            transformNewRender.Local = new Transform( transformRender.Local );
            transformNewRender.Compose();
            renderObj = renderNewObj;

            // update the reflex
            ReflexPanel panel = parent as ReflexPanel;
            reflex.Replace(this.card, this.pendingCard, cardType, panel.ActiveCard);
            this.card = this.pendingCard;

            renderList.Add(renderObj);
            renderObj.Activate();

            // Set our magic refresh card which will let the cursor
            // automagically move to the next spot.  But don't do 
            // this for the say verb or said filter since we want 
            // to leave the cursor there so we can open the text editor.
            if (pendingCard != null)
            {
                prevUpid = pendingCard.upid;
            }
            else
            {
                prevUpid = null;
            }

            //if this flag isn't set, a "NavNext" call will be made, moving to the next tile and preventing the card from remaining active upon
            //return from the screen that pops up (say dialog, world/object settings, etc.).  If you have a tile that brings up a new screen after placement,
            //it will likely need to be added to this list.              
            if (pendingCard != null &&
                !(pendingCard.upid == "actuator.say" ||
                  pendingCard.upid == "actuator.microbit.say" || 
                  pendingCard.upid == "filter.said" || 
                  pendingCard.upid == "actuator.movementspeedmodify" || 
                  pendingCard.upid == "actuator.turningspeedmodify" ||
                  pendingCard.upid == "actuator.rescale" || 
                  pendingCard.upid == "actuator.rescaleinstant" ||
                  pendingCard.upid == "actuator.holddistance" ||
                  pendingCard.upid == "actuator.holddistanceinstant" ||
                  pendingCard.upid == "actuator.worldlightingchange" || 
                  pendingCard.upid == "actuator.worldlightingchangeinstant" ||
                  pendingCard.upid == "actuator.worldskychange" || 
                  pendingCard.upid == "actuator.worldskychangeinstant" || 
                  pendingCard.upid == "actuator.nextlevel"))
            {
                magicFlagOfRefreshAvoiding = true;
            }
        }

        /// <summary>
        /// Magic flag that lets us know if a new tile has been placed so
        /// that we can move the cursor to match.  The reason we need this 
        /// is that because of the delayed refresh architecture it takes
        /// forever before the new tile is valid.  Until it's valid we 
        /// can't navigate to it.
        /// </summary>
        static bool magicFlagOfRefreshAvoiding = false;
        /// <summary>
        /// Upid from the last added tile used to ensure that when we automatically
        /// switch to the next spot and that spot already has the "say" verb tile
        /// that we don't automatically open the text editor.
        /// </summary>
        static string prevUpid = null;  

        protected void ApplyUpdateObjChange(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            if (updateObj != null)
            {
                updateObj.Deactivate();
                updateList.Remove(updateObj);
            }

            updateObj = updateObjPending;

            updateList.Add(updateObj);
            //updateObj.Activate(); this is only set when hot

            if (magicFlagOfRefreshAvoiding)
            {
                magicFlagOfRefreshAvoiding = false;
                InGame.inGame.Editor.NavCardNext(null, null);
            }
        }
        
        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            // fix our update object
            // to the active one
            if (updateObj != updateObjPending)
            {
                ApplyUpdateObjChange(updateList, renderList);
            }
            if (this.pendingCard != this.card)
            {
                ApplyCardChange(updateList, renderList);
            }

            if (State != PendingState)
            {
                if (PendingState == States.Active)
                {
                    SwitchToNormal(updateList, renderList);
                }
                else if (PendingState == States.Inactive)
                {
                    result = SwitchToInactive(updateList, renderList);
                }
                else if (PendingState == States.Hot)
                {
                    SwitchToHot(updateList, renderList);
                }
                else if (PendingState == States.Disabled)
                {
                    SwitchToDisabled(updateList, renderList);
                }

                State = PendingState;
            }
            if (cardSelector != null && cardSelector.Refresh(updateList, renderList))
            {
                cardSelector = null;
            }
            return result;
        }

        override public void Activate()
        {
            if (State != States.Active)
            {
                PendingState = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        override public void Deactivate()
        {
            if (State != States.Inactive)
            {
                PendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }

        public void ClearCard(Object sender, EventArgs args)
        {
            Foley.PlayCut();

            // If the card we're thinking about cutting is the hidden NullSelector
            // then just bail.  Otherwise we end up cutting any real selector.
            // TODO Figure out why we have both a real selector and a hidden
            // null selector active at the same time.  There can be only one!
            if (this.pendingCard is NullSelector)
            {
                return;
            }

            if (this.pendingCard.upid == "actuator.rescale")
            {
                this.reflex.Data.ReScaleEnabled = false;
            }
            else if (this.pendingCard.upid == "actuator.holddistance")
            {
                //this.reflex.Data.HoldDistanceEnabled = false;
            }
            else if (this.pendingCard.upid == "actuator.worldlightingchange" || this.pendingCard.upid == "actuator.worldlightingchangeinstant")
            {
                this.reflex.Data.WorldLightChangeEnabled = false;
            }
            else if (this.pendingCard.upid == "actuator.worldskychange" || this.pendingCard.upid == "actuator.worldskychangeinstant")
            {
                this.reflex.Data.WorldSkyChangeEnabled = false;
            }
            else if (this.pendingCard.upid == "modifier.microbit.pattern")
            {
                // Figure out index of this card.
                int index = -1;
                for (int i=0; i<Reflex.Modifiers.Count; i++)
                {
                    if (Reflex.Modifiers[i].upid == "modifier.microbit.pattern")
                    {
                        ++index;
                    }
                    if (Reflex.Modifiers[i] == this.pendingCard)
                    {
                        break;
                    }
                }

                Debug.Assert(index > -1);
                Debug.Assert(index < Reflex.MicrobitPatterns.Count);

                // Need to remove this tile's pattern from the pattern array.
                if (index != -1)
                {
                    Reflex.MicrobitPatterns.RemoveAt(index);
                }
            }

            // rely on the change event to cause things to be removed 
            this.pendingCard = null;
            if (this.Change != null)
            {
                Change(this, null ); // this.pendingCard);
            }
            BokuGame.objectListDirty = true;
        }

        public void InsertReflex(Object sender, EventArgs args)
        {
            ReflexPanel parentPanel = this.parent as ReflexPanel;
            parentPanel.InsertReflex();
        }

        bool IControl.Hot
        {
            get { return (State == States.Hot); }
            set
            {
                if (value)
                {
                    if (State != States.Hot)
                    {
                        if (PendingState != States.Hot)
                        {
                            PendingState = States.Hot;
                            BokuGame.objectListDirty = true;
                        }
                    }
                    else
                    {
                        // We're already hot, just reinforce this idea with the cursor.
                        UiCursor.ActiveCursor.Parent = this;
                    }
                }
                else
                {
                    if (State != States.Active)
                    {
                        if (PendingState != States.Active)
                        {
                            PendingState = States.Active;
                            BokuGame.objectListDirty = true;
                        }
                    }
                    else if (PendingState != States.Active)
                    {
                        // No state change, just make sure pending state matches current state.  No clue how this would actually get out of sync...
                        PendingState = States.Active;
                    }
                }
            }
        }
        bool IControl.Disabled
        {
            get { return (State == States.Disabled); }
            set
            {
                if (value)
                {
                    if (State != States.Disabled)
                    {
                        PendingState = States.Disabled;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Disabled)
                    {
                        PendingState = States.Disabled;
                    }
                }
                else
                {
                    if (State != States.Active)
                    {
                        PendingState = States.Active;
                        BokuGame.objectListDirty = true;
                    }
                    else if (PendingState != States.Active)
                    {
                        PendingState = States.Active;
                    }
                }
            }
        }
    }
}
