
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

using Boku.Base;
using Boku.Common;
using Boku.UI;
using Boku.Programming;
using Boku.Input;
using Boku.Audio;

namespace Boku.UI
{
    public class ReflexPanel : GameObject, IControl, ITransform
    {
        public const string idRow = "row";
        public const string idLeftClauseAnchor = "left clause anchor";
        public const string idRightClauseAnchor = "right clause anchor";
        public const string idRowHandleAnchor = "handle anchor";

        public const string idLeftAnchor = "left anchor";
        public const string idTileAnchor = "tile anchor";
        public const string idRightAnchor = "right anchor";

        public const string idExpansionAnchor = "expansion anchor";

        public const string idWhenClause = "when clause";
        public const string idDoClause = "do clause";

        public const int indexDefaultCard = -1;

        public Vector3 position = Vector3.Zero;     // Used to try and work around the issues that arise when
                                                    // removing the references to Twitches.  The core of the 
                                                    // problem is that all of the movement for the ReflexHandle
                                                    // has been defined as relative rather than absolute.  This
                                                    // means that any active twitches have to complete before
                                                    // you can do the next movement since you need to know the
                                                    // final position before adding the offset.  So, this variable
                                                    // will contain the position of the ReflexHandle as if the 
                                                    // Twitches didn't exist.  It will also be a container for 
                                                    // much frustration and head shaking.

        private static List<ReflexData> cutPasteBuffer = null;
        /// <summary>
        /// Shared reflex cut/paste buffer.  This allows reflexes to be cut/paste
        /// between different tasks, brains, even games.
        /// </summary>
        public static List<ReflexData> CutPasteBuffer
        {
            get { return ReflexPanel.cutPasteBuffer; }
            set { ReflexPanel.cutPasteBuffer = value; }
        }

        protected class UpdateObjEditCards : UpdateControl
        {
            private ReflexPanel parent = null;
            public List<UpdateObject> updateList = null; // Children's update list.

            private CommandMap commandMap;

            public UpdateObjEditCards(ReflexPanel parent /*, ref Shared shared*/)
            {
                this.parent = parent;
                commandMap = new CommandMap(@"ReflexPanel");

                updateList = new List<UpdateObject>();
            }

            public override void Update()
            {
                // Update the parent's list of objects.
                for (int i = 0; i < updateList.Count; i++)
                {
                    UpdateObject obj = updateList[i] as UpdateObject;
                    Debug.Assert(obj != null);
                    obj.Update();
                }

                // Ensure this reflex is at the correct indent position.
                parent.AnimatePanelIndent(false);
            }

            public override void AddCommands(CommandMap map)
            {
                commandMap.Add(map);
            }
            public override void RemoveCommands(CommandMap map)
            {
                commandMap.Remove(map);
            }
            public override void AddCommandsToControl(IControl control)
            {
                control.AddCommands(this.commandMap);
            }
            public override void RemoveCommandsFromControl(IControl control)
            {
                control.RemoveCommands(this.commandMap);
            }
            public override void Activate()
            {
                CommandStack.Push(commandMap);
            }
            public override void Deactivate()
            {
                CommandStack.Pop(commandMap);
            }
        }
        protected ControlRenderObj renderPanel;
        // this is coded to support multiple update objects
        // even though its not really used.  It is being left in place
        // for future use rather than removing it
        protected UpdateControl updateObj; // active update object
        protected UpdateControl updateObjPending;
        protected UpdateObjEditCards updateObjEditCards;


        public List<IControl> listControls = new List<IControl>();              // Active controls to navigate right and left through.
        protected List<RenderObject> listWhenClause = new List<RenderObject>(); // Render objects for the when clause, excluding ReflexCards.
        protected List<RenderObject> listDoClause = new List<RenderObject>();   // Render objects for the do clause, excluding ReflexCards.

        public enum States
        {
            Inactive,
            Active,
            Hot,
            Disabled,
            Removed,
        }
        private States state = States.Inactive;
        public States pendingState = States.Inactive;

        private Object parent;
        private int indexActiveCard = ReflexPanel.indexDefaultCard;
        private Reflex reflex;

        private bool moving = false;
        protected AudioCue cueClick;
        protected ControlCollection controls;
        public bool uiRebuild = false;

        public Reflex Reflex
        {
            get
            {
                return reflex;
            }
        }
        protected Matrix AddClauseLeft( List<RenderObject> list, Matrix linked, string idClause, string idState)
        {
            ITransform transformClause;
            ControlRenderObj renderClause;
            Matrix anchor;
            // create the begining of the when clause trays
            //
            renderClause = controls.InstanceControlRenderObj(this, idClause + ControlCollection.H3gLeftPartTag);
            renderClause.State = idState;

            transformClause = renderClause as ITransform;
            // attach at left anchor
            renderClause.GetPositionTransform(idLeftAnchor, out anchor, true); // invert the anchor to apply to its owner
            linked *= anchor;
            transformClause.Local.Matrix = linked;
            transformClause.Compose();
            list.Add(renderClause);
            // adjust to attach next to the right anchor
            renderClause.GetPositionTransform(idRightAnchor, out anchor);
            linked *= anchor;
            return linked;
        }
        protected Matrix AddClauseRepeat(List<RenderObject> list, out Matrix local, Matrix linked, string idClause, string idState)
        {
            ITransform transformClause;
            ControlRenderObj renderClause;
            Matrix anchor;

            renderClause = controls.InstanceControlRenderObj(this, idClause + ControlCollection.H3gRepeatPartTag);
            renderClause.State = idState;

            transformClause = renderClause as ITransform;
            // attach at left anchor
            renderClause.GetPositionTransform(idLeftAnchor, out anchor, true); // invert the anchor to apply to its owner
            linked *= anchor;
            transformClause.Local.Matrix = linked;
            transformClause.Compose();
            list.Add(renderClause);
            renderClause.GetPositionTransform(idTileAnchor, out anchor);
            local = linked * anchor;

            // adjust to attach next to the right anchor
            renderClause.GetPositionTransform(idRightAnchor, out anchor);
            return linked * anchor;
        }
        protected Matrix AddClauseRight(List<RenderObject> list, out Matrix local, Matrix linked, string idClause, string idState)
        {
            ITransform transformClause;
            ControlRenderObj renderClause;
            Matrix anchor;

            // create the end of the when clause
            renderClause = controls.InstanceControlRenderObj(this, idClause + ControlCollection.H3gRightPartTag);
            renderClause.State = idState;

            transformClause = renderClause as ITransform;
            // attach at left anchor
            renderClause.GetPositionTransform(idLeftAnchor, out anchor, true ); // invert the anchor to apply to its owner
            linked *= anchor;
            transformClause.Local.Matrix = linked;
            transformClause.Compose();
            list.Add(renderClause);

            renderClause.GetPositionTransform(idExpansionAnchor, out anchor);
            local = linked * anchor;

            // adjust to attach next to the right anchor
            renderClause.GetPositionTransform(idRightAnchor, out anchor);
            return linked * anchor;
        }
        protected void AddCard(Matrix local, 
                ProgrammingElement element, 
                CardSpace.CardType cardType, 
                ControlRenderObj renderClause)
        {
            ReflexCard card = new ReflexCard(this, reflex, element, cardType, controls);
            card.renderClause = renderClause;
            ITransform transformCard = card as ITransform;
            Matrix anchor = card.Anchor( true ); // invert the anchor to apply to its owner
            transformCard.Local.Matrix = local * anchor;// *shiftOutOfSocket;
            transformCard.Compose();
            listControls.Add(card);
            card.Change += CardChanged;
        }
        
        public ReflexPanel(Object parent, Reflex reflex, ControlCollection controls)
        {
            this.parent = parent;
            this.reflex = reflex;
            this.controls = controls;
            renderPanel = controls.InstanceControlRenderObj(parent, idRow);

            updateObjEditCards = new UpdateObjEditCards(this);

            CreateCards( ControlRenderObj.idStateNormal );

            updateObjPending = updateObjEditCards;
        }

        protected void CreateCards( string idState )
        {
            // create handle
            //
            ReflexHandle reflexHandle = new ReflexHandle(this, reflex, controls);
            ITransform transformHandle = reflexHandle as ITransform;
            Matrix anchor;
            renderPanel.GetPositionTransform(idRowHandleAnchor, out anchor);
            transformHandle.Local.Matrix = anchor;
            transformHandle.Compose();
            listControls.Add(reflexHandle);

            Matrix chain;
            renderPanel.GetPositionTransform(idLeftClauseAnchor, out chain);
            Matrix clauseOffset = Matrix.Identity;
            {
                Matrix leftClause;
                Matrix rightClause;

                renderPanel.GetPositionTransform(idLeftClauseAnchor, out leftClause);
                renderPanel.GetPositionTransform(idRightClauseAnchor, out rightClause);
                clauseOffset.Translation = rightClause.Translation - leftClause.Translation;
            }

            Matrix local;

            // create the begining of the when clause trays
            //
            chain = AddClauseLeft(this.listWhenClause, chain, idWhenClause, idState);

            // create cards for when clause
            //
            //Matrix shiftOutOfSocket = Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.05f));
            CardSpace.CardType plusType = CardSpace.CardType.Sensor;
            ProgrammingElement plusElement = CardSpace.Cards.NullSensor;

            if (reflex.Sensor != null)
            {
                chain = AddClauseRepeat(this.listWhenClause, out local, chain, idWhenClause, idState);
                AddCard(local, 
                        reflex.Sensor, 
                        CardSpace.CardType.Sensor, 
                        this.listWhenClause[this.listWhenClause.Count-1] as ControlRenderObj);
                plusType = CardSpace.CardType.Filter;
                plusElement = CardSpace.Cards.NullFilter;
            }

            if (reflex.RawFilters != null)
            {
                for (int iFilter = 0; iFilter < reflex.RawFilters.Count; iFilter++)
                {
                    Filter filter = reflex.RawFilters[iFilter] as Filter;

                    chain = AddClauseRepeat(this.listWhenClause, out local, chain, idWhenClause, idState);
                    AddCard(local, 
                            filter, 
                            CardSpace.CardType.Filter,
                            this.listWhenClause[this.listWhenClause.Count - 1] as ControlRenderObj);
                }
            }

            // create the end of the when clause
            chain = AddClauseRight(this.listWhenClause, out local, chain, idWhenClause, idState);
            if (ReflexCard.HasSelectableElements(reflex, plusType))
            {
                // add the ever present + with context
                AddCard(local,
                        plusElement,
                        plusType,
                        this.listWhenClause[this.listWhenClause.Count - 1] as ControlRenderObj);
            }

            // adjust for next clause
            chain *= clauseOffset;

            // create the begining of the do clause trays
            //
            chain = AddClauseLeft(this.listDoClause, chain, idDoClause, idState);

            plusType = CardSpace.CardType.Actuator;
            plusElement = CardSpace.Cards.NullActuator;
            if (reflex.Actuator != null)
            {
                chain = AddClauseRepeat(this.listDoClause, out local, chain, idDoClause, idState);
                AddCard(local, 
                        reflex.Actuator, 
                        CardSpace.CardType.Actuator,
                        this.listDoClause[this.listDoClause.Count - 1] as ControlRenderObj);
                // Specify Selector type here, since selector menu will pull in modifiers as well.
                plusType = CardSpace.CardType.Selector;
                plusElement = CardSpace.Cards.NullSelector;
            }

            if (reflex.Selector != null && !reflex.Selector.hiddenDefault)
            {
                chain = AddClauseRepeat(this.listDoClause, out local, chain, idDoClause, idState);
                AddCard(local, 
                        reflex.Selector, 
                        CardSpace.CardType.Selector,
                        this.listDoClause[this.listDoClause.Count - 1] as ControlRenderObj);
                // Specify Selector type here, since selector menu will pull in modifiers as well.
                plusType = CardSpace.CardType.Selector;
                plusElement = CardSpace.Cards.NullSelector;
            }

            if (reflex.Modifiers != null)
            {
                for (int iModifier = 0; iModifier < reflex.Modifiers.Count; iModifier++)
                {
                    Modifier modifier = reflex.Modifiers[iModifier] as Modifier;

                    chain = AddClauseRepeat(this.listDoClause, out local, chain, idDoClause, idState);
                    AddCard(local, 
                            modifier, 
                            CardSpace.CardType.Modifier,
                            this.listDoClause[this.listDoClause.Count - 1] as ControlRenderObj);
                }
            }

            // create the end of the do clause
            chain = AddClauseRight(this.listDoClause, out local, chain, idDoClause, idState);

            CardSpace.CardType cardTypeMask;
            if (reflex.Actuator == null)
                cardTypeMask = CardSpace.CardType.Actuator;
            else if (reflex.Selector == null && reflex.Modifiers.Count == 0)
                cardTypeMask = CardSpace.CardType.Selector | CardSpace.CardType.Modifier;
            else if (reflex.Selector == null && reflex.Modifiers.Count == 1 && this.ActiveCard > 0 && this.ActiveCard < this.listControls.Count && this.listControls[this.ActiveCard] is ReflexCard)
            {
                cardTypeMask = CardSpace.CardType.Modifier;
                if ((this.listControls[this.ActiveCard] as ReflexCard).Card is Modifier)
                    cardTypeMask |= CardSpace.CardType.Selector;
            }
            else
                cardTypeMask = CardSpace.CardType.Modifier;

            if (ReflexCard.HasSelectableElements(reflex, cardTypeMask))
            {
                // HACK HACK If use has DO Move Freeze this will still add a '+' to the
                // end because it thinks that a selector is still a possibility.  The
                // proper solution is to either make Freeze a selector or, better yet,
                // do away with selectors altogether.
                if (reflex.modifierUpids.Length == 0 || reflex.modifierUpids[0] != "modifier.constraint.immobile")
                {
                    // add the + with context
                    AddCard(local,
                            plusElement,
                            plusType,
                            this.listDoClause[this.listDoClause.Count - 1] as ControlRenderObj);
                }
            }

            for (int indexClause = 0; indexClause < this.listWhenClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listWhenClause[indexClause] as ControlRenderObj;
                renderObj.Activate();
                renderPanel.renderList.Add(renderObj);
            }

            for (int indexClause = 0; indexClause < this.listDoClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listDoClause[indexClause] as ControlRenderObj;
                renderObj.Activate();
                renderPanel.renderList.Add(renderObj);
            }

            for (int indexCard = 0; indexCard < this.listControls.Count; indexCard++)
            {
                GameObject gameObjCard = this.listControls[indexCard] as GameObject;
                gameObjCard.Activate();
                gameObjCard.Refresh(updateObjEditCards.updateList, renderPanel.renderList);
            }
        }

        protected void RemoveCards()
        {
            for (int indexClause = 0; indexClause < this.listWhenClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listWhenClause[indexClause] as ControlRenderObj;
                renderObj.Deactivate();
                renderPanel.renderList.Remove(renderObj);
            }
            this.listWhenClause.Clear();

            for (int indexClause = 0; indexClause < this.listDoClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listDoClause[indexClause] as ControlRenderObj;
                renderObj.Deactivate();
                renderPanel.renderList.Remove(renderObj);
            }
            this.listDoClause.Clear();

            for (int indexCard = 0; indexCard < this.listControls.Count; indexCard++)
            {
                GameObject gameObjCard = this.listControls[indexCard] as GameObject;
                gameObjCard.Deactivate();
            }
        }

        protected void CardChanged(ReflexCard reflexCard, ProgrammingElement pendingCard)
        {
            this.uiRebuild = true;
            BokuGame.objectListDirty = true;

            if (pendingCard == null)
            {
                // deleted
                DeleteCard(reflexCard);
            }
            else if (reflexCard.Card.upid == ProgrammingElement.upidNull)
            {
                // new addition
                //AppendCard(reflexCard);
            }
            else
            {
                // just a change
                reflex.Data.CompactModifiers();
                ChangeCard(reflexCard, pendingCard);
            }
        }

        protected void DeleteCard(ReflexCard reflexCard)
        {
            CleanseDependantCards(reflexCard, null);
        }



        protected void ChangeCard(ReflexCard reflexCard, ProgrammingElement pendingCard)
        {
            CleanseDependantCards(reflexCard, pendingCard);
            // find all cards that are effected after it
            // remove the cards from listControls
            // remove the clause repeat from the clause list
            // animate the cards collapsing
            // animate the clause repeat collapsing
            // animate the clause right collapsing
        }
        protected void AppendCard(ReflexCard reflexCard)
        {
            CleanseDependantCards(reflexCard, null);
            // append new clause repeat
            // append new plus card
            // animate the card appearing
            // animate the clause repeat appearing
            // animate the clause right expanding
        }
        protected void CleanseDependantCards(ReflexCard reflexCard, ProgrammingElement pendingCard)
        {
            ReflexData clip = reflex.Copy();

            if (reflexCard.cardType == CardSpace.CardType.Sensor)
            {
                if (pendingCard != null)
                {
                    clip.Sensor = pendingCard as Sensor;
                }
                else
                {
                    clip.Sensor = null;
                }
            }
            if (reflexCard.cardType == CardSpace.CardType.Actuator)
            {
                // Kind of a hack here to deal with mixing Switch and Inline.  If either
                // of these actuators are removed and replaced with the other then we need
                // to clear the Modifier list to prevent loops and self-referential inlines.
                if (pendingCard != null && reflexCard != null)
                {
                    if ((pendingCard.upid == "actuator.inlinetask" && reflexCard.Card.upid == "actuator.switchtask")
                        || (pendingCard.upid == "actuator.switchtask" && reflexCard.Card.upid == "actuator.inlinetask"))
                    {
                        clip.Modifiers.Clear();
                    }
                }

                if (pendingCard != null)
                {
                    clip.Actuator = pendingCard as Actuator;
                }
                else
                {
                    clip.Actuator = null;
                }
            }
            if (reflexCard.cardType == CardSpace.CardType.Selector)
            {
                if (pendingCard != null)
                {
                    clip.Selector = pendingCard as Selector;
                }
                else
                {
                    clip.Selector = null;
                }
            }

            reflex.Paste(clip);
        }

        public int LineNumber
        {
            get
            {
                ReflexHandle handle = this.listControls[0] as ReflexHandle;
                return handle.LineNumber;
            }
            set
            {
                ReflexHandle handle = this.listControls[0] as ReflexHandle;
                handle.LineNumber = value;
            }
        }

        public BoundingBox BoundingBox
        {
            get
            {
                IBounding boundingPanel = renderPanel as IBounding;
                return boundingPanel.BoundingBox;
            }
        }
        // reflect ITransform into the renderPanel
        Transform ITransform.Local
        {
            get
            {
                ITransform transform = renderPanel as ITransform;
                return transform.Local;
            }
            set
            {
                ITransform transform = renderPanel as ITransform;
                transform.Local = value;
            }
        }
        Matrix ITransform.World
        {
            get
            {
                ITransform transform = renderPanel as ITransform;
                return transform.World;
            }
        }
        bool ITransform.Compose()
        {
            ITransform transform = renderPanel as ITransform;
            bool changed = transform.Compose();
            if (changed)
            {
                RecalcMatrix();
            }
            return changed;
        }
        void ITransform.Recalc(ref Matrix parentMatrix)
        {
            ITransform transform = renderPanel as ITransform;
            transform.Recalc(ref parentMatrix);
            Matrix world = transform.World;
            for (int indexClause = 0; indexClause < this.listWhenClause.Count; indexClause++)
            {
                ITransform transformObj = listWhenClause[indexClause] as ITransform;
                transformObj.Recalc(ref world);
            }
            for (int indexClause = 0; indexClause < this.listDoClause.Count; indexClause++)
            {
                ITransform transformObj = listDoClause[indexClause] as ITransform;
                transformObj.Recalc(ref world);
            }
            for (int indexCard = 0; indexCard < listControls.Count; indexCard++)
            {
                ITransform transformCard = listControls[indexCard] as ITransform;
                transformCard.Recalc(ref world);
            }
        }
        ITransform ITransform.Parent
        {
            get
            {
                return this.parent as ITransform;
            }
            set
            {
                this.parent = value;
            }
        }
        protected void RecalcMatrix()
        {
            ITransform transformThis = this as ITransform;
            ITransform transformParent = this.parent as ITransform;
            Matrix ParentWorldTransform = transformParent.World;
            transformThis.Recalc(ref ParentWorldTransform);
        }
        protected void NavCard(int indexCard)
        {
            Debug.Assert(indexCard >= 0 && indexCard < listControls.Count);
            if (indexCard != this.indexActiveCard)
            {
                // Update help overlay.
                if (indexCard == 0)
                {
                    // Moving to the handle.
                    HelpOverlay.Pop();
                    // Decide which overlay to push...
                    if (ReflexPanel.CutPasteBuffer == null)
                    {
                        HelpOverlay.Push("RowHandleEmptyPasteBuffer");
                    }
                    else
                    {
                        HelpOverlay.Push("RowHandleFullPasteBuffer");
                    }
                } 
                else if (indexActiveCard == 0 || indexActiveCard == -1)
                {
                    // Moving away from the handle OR moving to a newly created row.
                    HelpOverlay.Pop();
                    HelpOverlay.Push("Tile");
                }

                // update card state
                IControl card;
                if (this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count)
                {
                    card = listControls[this.indexActiveCard] as IControl;
                    card.Hot = false;
                }
                this.indexActiveCard = indexCard;
                card = listControls[this.indexActiveCard] as IControl;
                card.Hot = true;
            }
        }
        protected int FindDefaultCard()
        {
            int indexNewCard = -1;
            // find the actuator and use it as the default
            for (int indexCard = 0; indexCard < listControls.Count; indexCard++)
            {
                ReflexCard card = listControls[indexCard] as ReflexCard;
                if (card != null && card.cardType == CardSpace.CardType.Actuator)
                {
                    indexNewCard = indexCard;
                    break;
                }
            }
            Debug.Assert(indexNewCard != -1);
            return indexNewCard;
        }
        public int ActiveCard
        {
            get
            {
                return this.indexActiveCard;
            }
            set
            {
                int indexNewCard = value;
                if (indexNewCard <= ReflexPanel.indexDefaultCard)
                {
                    indexNewCard = FindDefaultCard();
                }
                // force inside of available cards
                if (indexNewCard >= listControls.Count)
                {
                    indexNewCard = listControls.Count - 1;
                }
                NavCard(indexNewCard);
            }
        }
        public void NavCardPrev()
        {
            if (this.indexActiveCard > 0)
            {
                Foley.PlayClick();

                NavCard(this.indexActiveCard - 1);
            }
        }

        public void NavCardNext()
        {
            if (this.indexActiveCard < listControls.Count - 1)
            {
                Foley.PlayClick();

                NavCard(this.indexActiveCard + 1);
            }
        }

        public void MoveReflexUp()
        {
            Editor parentEditor = parent as Editor;
            parentEditor.MoveReflexUp(this);
        }

        public void MoveReflexDown()
        {
            Editor parentEditor = parent as Editor;
            parentEditor.MoveReflexDown(this);
        }

        public void RemoveReflex()
        {
            Editor parentEditor = parent as Editor;
            parentEditor.RemoveReflex(this);
        }
        public void InsertReflex()
        {
            Editor parentEditor = parent as Editor;
            parentEditor.InsertReflex(this);
        }
        protected bool SwitchToNormal(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (state == States.Inactive)
            {
                // Don't keep adding the same object to the list!
                if (!updateList.Contains(updateObj))
                {
                    updateList.Add(updateObj);
                }
                //updateObj.Activate();
                renderList.Add(renderPanel);
                SwitchRenderStateTo(ControlRenderObj.idStateNormal);
                
                renderPanel.Activate();
            }
            else if (state == States.Hot)
            {
                updateObj.Deactivate();
                SwitchRenderStateTo(ControlRenderObj.idStateNormal);
            }
            return result;
        }
        protected bool SwitchToRemoved(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (state == States.Hot)
            {
                updateObj.Deactivate();
                
            }
            // also set active card state as we are acting like a thin container
            if (this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count)
            {
                IControl card = listControls[this.indexActiveCard] as IControl;
                card.Hot = false;
            }
            SwitchRenderStateTo(ControlRenderObj.idStateNormal);

            return result;
        }
        protected bool SwitchToInactive(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (state != States.Inactive)
            {
                renderPanel.Deactivate();
                renderList.Remove(renderPanel);
                updateObj.Deactivate();
                updateList.Remove(updateObj);
                result = true; // remove us
            }
            return result;
        }

        protected bool SwitchToHot(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            if (state == States.Inactive)
            {
                // Don't keep adding the same object to the list!
                if (!updateList.Contains(updateObj))
                {
                    updateList.Add(updateObj);
                }
//                updateObj.Activate();
                renderList.Add(renderPanel);
                SwitchRenderStateTo(ControlRenderObj.idStateHot);
                renderPanel.Activate();
            }
            else if (state == States.Active)
            {
                SwitchRenderStateTo(ControlRenderObj.idStateHot);
            }
            updateObj.Activate();
            return result;
        }

        protected void SwitchRenderStateTo( string idState )
        {
            renderPanel.State = idState;
            for (int indexClause = 0; indexClause < this.listWhenClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listWhenClause[indexClause] as ControlRenderObj;
                renderObj.State = idState;
            }
            for (int indexClause = 0; indexClause < this.listDoClause.Count; indexClause++)
            {
                ControlRenderObj renderObj = this.listDoClause[indexClause] as ControlRenderObj;
                renderObj.State = idState;
            }
        }

        protected bool SwitchToDisabled(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;
            return result;
        }
        protected void ApplyUpdateObjChange(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            if (updateObj != null)
            {
                updateObj.Deactivate();
                updateList.Remove(updateObj);
                // have our update object remove its commands from the child controls.
                for (int indexCard = 0; indexCard < listControls.Count; indexCard++)
                {
                    IControl control = listControls[indexCard] as IControl;
                    updateObj.RemoveCommandsFromControl(control);
                }
            }

            updateObj = updateObjPending;

            AttachCommands();

            // Keep updatelist from having multiple copies of the same obj.
            if (!updateList.Contains(updateObj))
            {
                updateList.Add(updateObj);
            }
            // updateObj.Activate(); this is only called when hot
        }

        protected void AttachCommands()
        {
            // have our update object add its commands to the child controls.
            for (int indexCard = 0; indexCard < listControls.Count; indexCard++)
            {
                IControl control = listControls[indexCard] as IControl;
                updateObj.AddCommandsToControl(control);
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
            

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    result = SwitchToNormal(updateList, renderList);
                }
                else if (pendingState == States.Inactive)
                {
                    result = SwitchToInactive(updateList, renderList);
                }
                else if (pendingState == States.Hot)
                {
                    result = SwitchToHot(updateList, renderList);
                }
                else if (pendingState == States.Disabled)
                {
                    result = SwitchToDisabled(updateList, renderList);
                }
                else if (pendingState == States.Removed)
                {
                    result = SwitchToRemoved(updateList, renderList);
                }

                state = pendingState;
            }

            RefreshChildren();

            if (this.uiRebuild)
            {
                int linenumberTemp = this.LineNumber; // store away current line number

                RemoveCards();
                RefreshChildren();
                CreateCards( ControlRenderObj.idStateHot );
                AttachCommands();
                // cause card state reset
                int indexTemp = this.ActiveCard;
                this.indexActiveCard = -1;
                this.ActiveCard = indexTemp;

                this.LineNumber = linenumberTemp; // cause line number texture rebuild
                this.uiRebuild = false;
            }

            return result;
        }

        private void RefreshChildren()
        {
            for (int indexCard = 0; indexCard < listControls.Count; indexCard++)
            {
                GameObject reflexCard = (GameObject)listControls[indexCard];
                if (reflexCard != null && reflexCard.Refresh(updateObjEditCards.updateList, renderPanel.renderList))
                {
                    listControls.RemoveAt(indexCard);
                    indexCard--;
                }
            }
        }

        override public void Activate()
        {
            if (state != States.Active)
            {
                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                // Stash away the initial position.
                ITransform t = this as ITransform;
                position = t.Local.Translation;
            }
        }   

        override public void Deactivate()
        {
            if (state != States.Inactive)
            {
                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;
            }
        }
        public const float zOffsetMovingPanel = 0.4f;

        public bool Removed
        {
            get
            {
                return (this.state == States.Removed || this.pendingState == States.Removed);
            }
            set
            {
                if (this.state != States.Removed)
                {
                    this.pendingState = States.Removed;
                    BokuGame.objectListDirty = true;
                }
            }
        }

        public bool Moving
        {
            get
            {
                return this.moving;
            }
            set
            {
                if (this.moving != value)
                {
                    if (this.moving)
                    {
                        // was moving, no longer moving
                        AnimatePanelShift(-zOffsetMovingPanel, null);
                    }
                    else
                    {
                        // was not moving, now moving
                        AnimatePanelShift(zOffsetMovingPanel, null);
                    }
                    this.moving = value;
                }
            }
        }

        private float twitchTime = 0.2f;

        public void AnimatePanelMove(float changeY)
        {
            position.Y += changeY;
            ITransform transform = this as ITransform;
            if(transform != null)
            {
                TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 trans = transform.Local.Translation; trans.Y = value; transform.Local.Translation = trans; transform.Compose(); };
                TwitchManager.CreateTwitch<float>(transform.Local.Translation.Y, position.Y, set, twitchTime, TwitchCurve.Shape.EaseOut);
            }
        }

        public void AnimatePanelShift(float changeZ, TwitchCompleteEvent callback)
        {
            position.Z += changeZ;
            ITransform transform = this as ITransform;
            if (transform != null)
            {
                TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 trans = transform.Local.Translation; trans.Z = value; transform.Local.Translation = trans; transform.Compose(); };
                TwitchManager.CreateTwitch<float>(transform.Local.Translation.Z, position.Z, set, twitchTime, TwitchCurve.Shape.EaseOut, this, callback);
            }
        }

        /// <summary>
        /// Moves the curent panel to the correct indent position to match it's current indent level.
        /// If the panel is already in the correct position, nothing happens.  This is designed to 
        /// work fine if called every frame for each panel.
        /// </summary>
        public void AnimatePanelIndent(bool force)
        {
            float kIndentSpacing = 0.6f;

            float x = reflex.Indentation * kIndentSpacing;
            if (position.X != x || force)
            {
                position.X = x;
                ITransform transform = this as ITransform;
                if (transform != null)
                {
                    TwitchManager.Set<float> set = delegate(float value, Object param) { Vector3 trans = transform.Local.Translation; trans.X = value; transform.Local.Translation = trans;  transform.Compose(); };
                    TwitchManager.CreateTwitch<float>(transform.Local.Translation.X, position.X, set, 0.1f, TwitchCurve.Shape.EaseOut, this, null);
                }
            }
        }

        // IControl
        void IControl.AddCommands(CommandMap map)
        {
            // only add them to the normal state object
            updateObjEditCards.AddCommands(map);
        }
        void IControl.RemoveCommands(CommandMap map)
        {
            // only add them to the normal state object
            updateObjEditCards.RemoveCommands(map);
        }

        bool IControl.Hot
        {
            get
            {
                return (state == States.Hot);
            }
            set
            {
                if (!Removed)
                {
                    if (value)
                    {

                        // also set active card state as we are acting like a thin container
                        if (this.indexActiveCard <= ReflexPanel.indexDefaultCard)
                        {
                            this.indexActiveCard = FindDefaultCard();
                        }
                        Debug.Assert(this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count);
                        IControl card = listControls[this.indexActiveCard] as IControl;
                        card.Hot = true;

                        if (state != States.Hot)
                        {
                            pendingState = States.Hot;
                            BokuGame.objectListDirty = true;
                        }
                        else if (pendingState != States.Hot)
                        {
                            pendingState = States.Hot;
                        }
                    }
                    else
                    {

                        // also set active card state as we are acting like a thin container
                        if (this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count)
                        {
                            IControl card = listControls[this.indexActiveCard] as IControl;
                            card.Hot = false;
                        }
                        if (state != States.Active)
                        {

                            pendingState = States.Active;
                            BokuGame.objectListDirty = true;
                        }
                        else if (pendingState != States.Active)
                        {
                            pendingState = States.Active;
                        }
                    }
                }
            }
        }
        bool IControl.Disabled
        {
            get
            {
                return (state == States.Disabled);
            }
            set
            {
                if (!Removed)
                {
                    if (value)
                    {
                        // also set active card state as we are acting like a thin container
                        if (this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count)
                        {
                            IControl card = listControls[this.indexActiveCard] as IControl;
                            card.Disabled = true;
                        }
                        if (state != States.Disabled)
                        {
                            pendingState = States.Disabled;
                            BokuGame.objectListDirty = true;
                        }
                        else if (pendingState != States.Disabled)
                        {
                            pendingState = States.Disabled;
                        }
                    }
                    else
                    {
                        // also set active card state as we are acting like a thin container
                        if (this.indexActiveCard >= 0 && this.indexActiveCard < listControls.Count)
                        {
                            IControl card = listControls[this.indexActiveCard] as IControl;
                            card.Disabled = false;
                        }
                        if (state != States.Active)
                        {
                            pendingState = States.Active;
                            BokuGame.objectListDirty = true;
                        }
                        else if (pendingState != States.Active)
                        {
                            pendingState = States.Active;
                        }
                    }
                }
            }
        }
        
        
    }
}
