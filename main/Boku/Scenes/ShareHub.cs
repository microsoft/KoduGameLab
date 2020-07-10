// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Boku.Base;
using Boku.Fx;
using Boku.Common;
using Boku.Common.Xml;
using Boku.SimWorld;
using Boku.UI;
using Boku.UI2D;
using Boku.Input;
using Boku.Common.Sharing;

namespace Boku
{
    /// <summary>
    /// It presents the user with a set of sharing modes for selection.
    /// You see it after selecting "Share" from the main menu.
    /// </summary>
    public class ShareHub : GameObject, INeedsDeviceReset
    {
        public static ShareHub Instance;

        public const float kAsyncOpDelay = 0.01f;    // seconds

        protected class Shared : INeedsDeviceReset
        {
            public Camera camera = new PerspectiveUICamera();

            public UIGrid grid = null;

            public List<UIGridShareHubElement> elements;

            public ShareHub parent;

            public AABB2D openBox = new AABB2D();       // Hit box for "open sharing session" tile at bottom.

            public bool forceSessionRestart = false;    // This is set to true when we are hosting a shring session
                                                        // and the session must be aborted for any reason.  When 
                                                        // this is true we start hosting a new session just as if
                                                        // the user had done so.

            // c'tor
            public Shared(ShareHub parent)
            {
                this.parent = parent;

                // Set up the camera for the right screen size
                // to match the rendertarget we use.
                camera.Resolution = new Point(1280, 720);
            }

            public void LoadContent(bool immediate)
            {
                BokuGame.Load(grid, immediate);
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
            }

            public void UnloadContent()
            {
                BokuGame.Unload(grid);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
                BokuGame.DeviceReset(grid, device);
            }

            internal void Activate()
            {
                if (!LiveManager.AcceptedShareInvitation)
                {
                    LiveManager.FriendUpdatesEnabled = true;
                    LiveManager.JoinableUpdatesEnabled = true;
                }
                else
                {
                    LiveManager.FriendUpdatesEnabled = false;
                    LiveManager.JoinableUpdatesEnabled = false;
                }

                // TODO: remap to a kodu-meaningful string if remote player is playing kodu (but could look odd in the dashboard and other games that show presence).
                InGame.SetPresence(GamerPresenceMode.CornflowerBlue);

                // We rebuild the grid each time the share hub is activated, to reflect the dynamic nature of the friend list.
                grid = new UIGrid(parent.OnSelect, parent.OnCancel, new Point(1, 0), "App.ShareHub.Grid");

                elements = new List<UIGridShareHubElement>();

                // Set grid properties.
                grid.Spacing = new Vector2(0.0f, -0.1f);    // The first number doesn't really matter since we're doing a 1d column.
                grid.Scrolling = true;
                grid.Wrap = false;
                grid.SlopOffset = true;
                grid.UseMouseScrollWheel = true;

                // First element is the "Enter Sharing Room" button.
                //grid.Add(new UIGridStartSharingElement(), 0, 0);

                BokuGame.Load(grid, true);

                grid.Active = true;

                // Queue up session finders for the sharing servers.

                if (!LiveManager.AcceptedShareInvitation)
                {
                    foreach (string gamerTag in SharingServers.Instance.gamerTags)
                    {
                        UIGridSharingServerElement elem = new UIGridSharingServerElement(gamerTag);

                        elem.JoinableChanged += new StatusChangedDelegate(elem_JoinableChanged);

                        elements.Add(elem);

                        elem.StartCheckingPresence();
                    }
                }
            }

            void elem_JoinableChanged(UIGridShareHubElement elem)
            {
                RefreshGrid();
            }


            List<UIGridShareHubElement> refreshGrid_scratch = new List<UIGridShareHubElement>();

            internal void RefreshGrid()
            {
                Point currSelection = grid.SelectionIndex;

                GetSortedGridElements(refreshGrid_scratch);

                for (int i = 0; i < grid.ActualDimensions.Y; ++i)
                {
                    UIGridShareHubElement elem = grid.Get(0, i) as UIGridShareHubElement;

                    elem.Selected = false;

                    if (!refreshGrid_scratch.Contains(elem))
                    {
                        BokuGame.Unload(elem);
                    }
                }

                grid.Clear();

                for (int i = 0; i < refreshGrid_scratch.Count; ++i)
                {
                    grid.Add(refreshGrid_scratch[i], 0, grid.ActualDimensions.Y);
                    BokuGame.Load(refreshGrid_scratch[i]);
                }

                grid.Dirty = true;

                // Try to preserve selection index.
                // We might want to try to relocate the selected element if it moved instead.
                if (grid.ActualDimensions.Y > currSelection.Y)
                    grid.SelectionIndex = new Point(0, currSelection.Y);
                else if (grid.ActualDimensions.Y > 0)
                    grid.SelectionIndex = new Point(0, grid.ActualDimensions.Y - 1);

                Matrix parentMatrix = Matrix.Identity;
                grid.Update(ref parentMatrix);
            }

            private void GetSortedGridElements(List<UIGridShareHubElement> list)
            {
                list.Clear();

                // Friends playing Kodu
                foreach (UIGridShareHubElement elem in elements)
                {
                    UIGridShareFriendElement friend = elem as UIGridShareFriendElement;

                    if (friend == null)
                        continue;

                    if (!friend.Friend.IsOnline)
                        continue;

                    if (!friend.Friend.IsPlayingKodu)
                        continue;

                    friend.Friend.Dirty = true;
                    list.Add(elem);
                }

                // Super-friends
                foreach (UIGridShareHubElement elem in elements)
                {
                    UIGridSharingServerElement server = elem as UIGridSharingServerElement;

                    if (server == null)
                        continue;

                    if (!server.IsOnline)
                        continue;

                    server.Dirty = true;
                    list.Add(elem);
                }

                // Friends online not playing Kodu
                foreach (UIGridShareHubElement elem in elements)
                {
                    UIGridShareFriendElement friend = elem as UIGridShareFriendElement;

                    if (friend == null)
                        continue;

                    if (!friend.Friend.IsOnline)
                        continue;

                    if (friend.Friend.IsPlayingKodu)
                        continue;

                    friend.Friend.Dirty = true;
                    list.Add(elem);
                }

                // Friends offline
                foreach (UIGridShareHubElement elem in elements)
                {
                    UIGridShareFriendElement friend = elem as UIGridShareFriendElement;

                    if (friend == null)
                        continue;

                    if (friend.Friend.IsOnline)
                        continue;

                    friend.Friend.Dirty = true;
                    list.Add(elem);
                }
            }

            internal void Deactivate()
            {
                LiveManager.FriendUpdatesEnabled = false;
                LiveManager.JoinableUpdatesEnabled = false;

                if (LiveManager.IsConnected)
                {
                    LiveManager.Session.GamerJoined -= Session_GamerJoined;
                    LiveManager.Session.GamerLeft -= Session_GamerLeft;
                }

                LiveManager.ClearQueuedOperations(null);

                grid.Active = false;

                // We rebuild the grid each time the share hub is activated, to reflect the dynamic nature of the friend list.
                BokuGame.Unload(grid);
                grid.Clear();

                elements.Clear();
            }

            internal void Session_GamerJoined(object sender, GamerJoinedEventArgs e)
            {
                // If we invited this friend to our session, this is where we detect them arriving while we're at the share hub.
                UIGridShareFriendElement elem = FindFriendElement(e.Gamer.Gamertag);
                if (elem != null)
                {
                    elem.Friend.IsJoining = false;
                    elem.Friend.IsJoined = true;
                }
            }

            internal void Session_GamerLeft(object sender, GamerLeftEventArgs e)
            {
                UIGridShareFriendElement elem = FindFriendElement(e.Gamer.Gamertag);
                if (elem != null)
                {
                    elem.Friend.IsJoined = false;
                }
            }

            internal UIGridShareFriendElement FindFriendElement(string gamertag)
            {
                // Find the menu element for this gamertag.
                for (int i = 0; i < grid.ActualDimensions.Y; ++i)
                {
                    UIGridShareFriendElement elem = grid.Get(0, i) as UIGridShareFriendElement;

                    if (elem == null)
                        continue;

                    if (elem.Friend.GamerTag == gamertag)
                        return elem;
                }

                return null;
            }
        }

        protected class UpdateObj : UpdateObject
        {
            private ShareHub parent = null;
            private Shared shared = null;

            public UpdateObj(ShareHub parent, Shared shared)
            {
                this.parent = parent;
                this.shared = shared;
            }

            public override void Update()
            {
                if (UpdateMessages())
                    return;

                // If we signed out of LIVE, back out to the main menu.
                if (!GamerServices.SignedInToLive)
                {
                    parent.ActivateMainMenu();
                    return;
                }

                // If we are joining a share session by accepting an invite from the Dashboard, bail now; we're just waiting for this
                // operation to complete then we'll open the sharing screen.
                if (LiveManager.JoiningShareInvitation)
                {
                    return;
                }

                // If we are accepting an invite via the Guide interface, start joining the session.
                if (LiveManager.AcceptedShareInvitation)
                {
                    parent.openingSharingRoomMessage.Activate();

                    // Change state from "accepting" to "joining" the invited session.
                    LiveManager.AcceptedShareInvitation = false;
                    LiveManager.JoiningShareInvitation = true;

                    // Disable the share hub UI.
                    shared.grid.Active = false;

                    // Cancel pending LIVE queries.
                    LiveManager.ClearQueuedOperations(null);

                    // Start joining the invited session.
                    InvitedSessionJoiner joinerOp = new InvitedSessionJoiner(parent.JoinSessionComplete_ActivateSharingScreen, null, parent);
                    joinerOp.Queue();

                    return;
                }

                // Don't do any input processing if the guide is up.
                if (GamerServices.IsGuideVisible)
                {
                    return;
                }

                // Check for mouse press on bottom tile.
                // hit is in pixels in screen coords (before overscan adjustment)
                Vector2 hit = MouseInput.GetAspectRatioAdjustedPosition(shared.camera, true);

                bool openPressed = false;
                if(shared.openBox.LeftPressed(hit))
                {
                    openPressed = true;
                }

                GamePadInput pad = GamePadInput.GetGamePad0();
                if (Actions.StartSharing.WasPressed || openPressed || shared.forceSessionRestart)
                {
                    Actions.StartSharing.ClearAllWasPressedState();

                    shared.forceSessionRestart = false;

                    // User selected "Enter Sharing Room"
                    if (!LiveManager.IsConnected)
                    {
                        LiveManager.FriendUpdatesEnabled = false;
                        LiveManager.JoinableUpdatesEnabled = false;

                        parent.openingSharingRoomMessage.Activate();

                        LiveManager.ClearQueuedOperations(null);

                        SessionCreator createOp = new SessionCreator(parent.SessionCreatorComplete_ActivateSharingScreen, null, this);
                        createOp.Queue();
                    }
                    else
                    {
                        parent.ActivateSharingScreen();
                    }
                }

                // If we're still active, ensure that the friends list is up to date.
                // We don't want to do this if not active since we'll just end up
                // adding all our friends to the grid which we just cleared...
                if (shared.grid.Active)
                {
                    // We're not accepting or joining an invited session, and we're still logged in to LIVE, so update the friend menu to reflect their current status bits.
                    List<LiveFriend> friends = LiveManager.Friends;

                    bool refreshGrid = false;

                    foreach (LiveFriend friend in friends)
                    {
                        UIGridShareFriendElement elem = shared.FindFriendElement(friend.GamerTag);

                        // Build the menu on the fly.
                        if (elem == null && friend.IsFriend)
                        {
                            elem = new UIGridShareFriendElement(friend);
                            shared.elements.Add(elem);
                            refreshGrid = true;
                        }
                        else if (elem != null && !friend.IsFriend)
                        {
                            shared.elements.Remove(elem);
                            refreshGrid = true;
                        }
                    }

                    // If we've changed the list, call update on the grid again so 
                    // that it's in a good state for rendering.
                    if (refreshGrid)
                    {
                        shared.RefreshGrid();
                    }
                }

                Matrix parentMatrix = Matrix.Identity;
                shared.grid.Update(ref parentMatrix);

                // Only care about mouse input on the grid if the grid is not empty.
                if (shared.grid.ActualDimensions != Point.Zero)
                {
                    // Mouse Input.
                    // Scroll wheel is handled by the grid.
                    // Clicking on Invite square should send invite.
                    // Clicking on user tile should bring that user into focus.

                    // Check if mouse hitting current selection object.
                    UIGridElement e = shared.grid.SelectionElement;
                    Matrix mat = Matrix.Invert(e.WorldMatrix);
                    Vector2 hitUV = MouseInput.GetHitUV(shared.camera, ref mat, e.Size.X, e.Size.Y, true);

                    bool focusElementHit = false;
                    if (hitUV.X >= 0 && hitUV.X < 1.25 && hitUV.Y >= 0 && hitUV.Y < 1)
                    {
                        focusElementHit = true;

                        // See if we hit the "invite/join" tile which is
                        // to the right of the main part of the tile.
                        if (hitUV.X > 1)
                        {
                            if (MouseInput.Left.WasPressed)
                            {
                                MouseInput.ClickedOnObject = this;
                            }
                            if (MouseInput.Left.WasReleased && MouseInput.ClickedOnObject == this)
                            {
                                parent.OnSelect(shared.grid);
                            }
                        }
                    }

                    // If we didn't hit the focus object, see if we hit any of the others.
                    // If so, bring them into focus.
                    if (!focusElementHit && MouseInput.Left.WasPressed)
                    {
                        for (int i = 0; i < shared.grid.ActualDimensions.Y; i++)
                        {
                            if (i == shared.grid.SelectionIndex.Y)
                                continue;

                            e = shared.grid.Get(0, i);
                            mat = Matrix.Invert(e.WorldMatrix);
                            hitUV = MouseInput.GetHitUV(shared.camera, ref mat, e.Size.X, e.Size.Y, true);

                            if (hitUV.X >= 0 && hitUV.X < 1 && hitUV.Y >= 0 && hitUV.Y < 1)
                            {
                                // We hit an element, so bring it into focus.
                                shared.grid.SelectionIndex = new Point(0, i);

                                break;
                            }

                        }
                    }
                }

            }   // end of Update()

            private bool UpdateMessages()
            {
                parent.openingSharingRoomMessage.Update();
                parent.openingInviteGuideMessage.Update();

                return
                    parent.openingSharingRoomMessage.Active ||
                    parent.openingInviteGuideMessage.Active;
            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
                // Leaving the share hub is a good place to clear these bits.
                LiveManager.AcceptedShareInvitation = false;
                LiveManager.JoiningShareInvitation = false;
            }
        }

        protected class RenderObj : RenderObject, INeedsDeviceReset
        {
            private Shared shared;
            private ShareHub parent = null;

            public Texture2D backgroundTexture = null;
            public Texture2D tile1 = null;
            public Texture2D tile2 = null;

            private TextBlob tile1Blob = null;
            private TextBlob tile2Blob = null;

            public RenderObj(ShareHub parent, Shared shared)
            {
                this.shared = shared;
                this.parent = parent;

                tile1Blob = new TextBlob(UI2D.Shared.GetGameFont20, Strings.Localize("shareHub.invite"), 365);
                tile2Blob = new TextBlob(UI2D.Shared.GetGameFont20, Strings.Localize("shareHub.start"), 435);
            }

            public override void Render(Camera camera)
            {
                GraphicsDevice device = BokuGame.bokuGame.GraphicsDevice;

                RenderTarget2D rt = UI2D.Shared.RenderTargetDepthStencil1280_720;

                Vector2 screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);
                Vector2 rtSize = new Vector2(rt.Width, rt.Height);

                // Render the scene to our rendertarget.
                InGame.SetRenderTarget(rt);

                // Set up params for rendering UI with this camera.
                Fx.ShaderGlobals.SetCamera(shared.camera);

                ScreenSpaceQuad quad = ScreenSpaceQuad.GetInstance();

                // Copy the background to the rt.
                quad.Render(backgroundTexture, Vector2.Zero, rtSize, "TexturedNoAlpha");

                // Set up local camera for rendering.
                Fx.ShaderGlobals.SetCamera(shared.camera);

                // Render the grid of friends.
                shared.grid.Render(shared.camera);

                Color darkText = new Color(40, 40, 40);
                Color greenText = new Color(12, 255, 0);

                // Tile1
                Vector2 size = new Vector2(tile1.Width, tile1.Height);
                Vector2 position = new Vector2(285, 0);
                quad.Render(tile1, position, size, "TexturedRegularAlpha");

                // TODO Add code to adjust for a change in the number of lines of text to make localization easier.
                position += new Vector2(120, 42);
                tile1Blob.RenderWithButtons(position, darkText);

                int numFriends = CalcNumInvitedFriends();

                SpriteBatch batch = UI2D.Shared.SpriteBatch;
                batch.Begin();

                int w = 70 - (int)UI2D.Shared.GetGameFont30Bold().MeasureString(numFriends.ToString()).X;
                position = new Vector2(790 + w / 2, 52);
                TextHelper.DrawString(UI2D.Shared.GetGameFont30Bold, numFriends.ToString(), position, greenText);

                position = new Vector2(865 - w / 3, 52);
                TextHelper.DrawString(UI2D.Shared.GetGameFont20, numFriends == 1 ? Strings.Localize("shareHub.friend") : Strings.Localize("shareHub.friends"), position, greenText);

                position = new Vector2(865 - w / 3, 77);
                TextHelper.DrawString(UI2D.Shared.GetGameFont20, Strings.Localize("shareHub.invited"), position, greenText);

                batch.End();

                // Tile2
                size = new Vector2(tile2.Width, tile2.Height);
                position = new Vector2(285, 550);
                quad.Render(tile2, position, size, "TexturedRegularAlpha");

                // Set up hit box for bottom tile.
                shared.openBox.Set(position, position + size);

                // TODO Add code to adjust for a change in the number of lines of text to make localization easier.
                position += new Vector2(120, 48);
                tile2Blob.RenderWithButtons(position, darkText);


                InGame.RestoreRenderTarget();

                InGame.Clear(new Color(20, 20, 20));

                // Copy the rendered scene to the rendertarget.
                float rtAspect = rtSize.X / rtSize.Y;
                position = Vector2.Zero;
                Vector2 newSize = screenSize;

                newSize.X = rtAspect * newSize.Y;
                position.X = (screenSize.X - newSize.X) / 2.0f;

                quad.Render(rt, position, newSize, @"TexturedNoAlpha");

                RenderMessages();
            }

            private int CalcNumInvitedFriends()
            {
                List<LiveFriend> friends = LiveManager.Friends;
                PlayerIndex index = GamePadInput.RealToLogical(GamePadInput.LastTouched);
                Gamer gamer = GamePadInput.GetGamer(index);

                int numFriends = 0;
                for (int i = 0; i < friends.Count; i++)
                {
                    if (friends[i].InvitedThem || friends[i].IsJoined || friends[i].IsJoining)
                    {
                        ++numFriends;
                    }
                }

                return numFriends;
            }   // end of CalcNumInvitedFriends()

            private void RenderMessages()
            {
                // Messages will only render if active.
                parent.openingSharingRoomMessage.Render();
                parent.openingInviteGuideMessage.Render();
            }

            public override void Activate()
            {
            }

            public override void Deactivate()
            {
            }

            #region INeedsDeviceReset Members

            public void LoadContent(bool immediate)
            {
            }

            public void InitDeviceResources(GraphicsDevice device)
            {
                if (backgroundTexture == null)
                {
                    backgroundTexture = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\LoadLevel\SharingBackground");
                }
                if (tile1 == null)
                {
                    tile1 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ShareHub\Tile1");
                }
                if (tile2 == null)
                {
                    tile2 = BokuGame.Load<Texture2D>(BokuGame.Settings.MediaPath + @"Textures\ShareHub\Tile2");
                }
            }

            public void UnloadContent()
            {
                BokuGame.Release(ref backgroundTexture);
                BokuGame.Release(ref tile1);
                BokuGame.Release(ref tile2);
            }

            /// <summary>
            /// Recreate render targets
            /// </summary>
            /// <param name="graphics"></param>
            public void DeviceReset(GraphicsDevice device)
            {
            }

            #endregion
        }

        protected Shared shared = null;
        protected RenderObj renderObj = null;
        protected UpdateObj updateObj = null;

        protected ModularMessageDialog openingSharingRoomMessage;
        protected ModularMessageDialog openingInviteGuideMessage;

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;
        private States pendingState = States.Inactive;

        private CommandMap commandMap = new CommandMap("App.ShareHub");   // Placeholder for stack.

        public bool Active
        {
            get { return (state == States.Active); }
        }

        public bool ForceSessionRestart
        {
            get { return shared.forceSessionRestart; }
            set { shared.forceSessionRestart = value; }
        }

        public ShareHub()
        {
            ShareHub.Instance = this;

            shared = new Shared(this);

            // Create the RenderObject and UpdateObject parts of this mode.
            updateObj = new UpdateObj(this, shared);
            renderObj = new RenderObj(this, shared);

            openingSharingRoomMessage = new ModularMessageDialog(
                Strings.Localize("shareHub.messageOpeningSharingRoom"),
                null, null,
                null, null,
                null, null,
                null, null);

            openingInviteGuideMessage = new ModularMessageDialog(
                Strings.Localize("shareHub.messageOpeningInviteGuide"),
                null, null,
                null, null,
                null, null,
                null, null);

        }   // end of MainMenu c'tor

        private void ActivateMainMenu()
        {
            // Backing out to main menu kills the session we're in.
            // If we're the host, everyone is disconnected.
            LiveManager.CloseSession();

            // Cancel pending profile updates, joinable session finds, etc.
            LiveManager.ClearQueuedOperations(this);

            // Activate main menu.
            Deactivate();
            BokuGame.bokuGame.mainMenu.Activate();
        }

        private void ActivateSharingScreen()
        {
            Deactivate();

            BokuGame.bokuGame.sharingScreen.ReturnToMenu = LoadLevelMenu.ReturnTo.ShareHub;
            BokuGame.bokuGame.sharingScreen.Activate();
        }

        public void OnSelect(UIGrid grid)
        {
            // User selected a friend.

            grid.Active = true;

            int index = shared.grid.SelectionIndex.Y;

            UIGridShareFriendElement friendElement = shared.grid.Get(0, index) as UIGridShareFriendElement;

            UIGridSharingServerElement serverElement = shared.grid.Get(0, index) as UIGridSharingServerElement;

            if (friendElement != null)
            {
                if (friendElement.Friend.IsOnline)
                {
                    if (friendElement.Friend.InvitedUs || friendElement.Friend.IsJoinable)
                    {
                        // Friend has a joinable session, directly join it ("jump in").
                        StartJumpingIn(friendElement.Friend.GamerTag, friendElement);

                        // Don't allow the user to interact with the share hub while the SessionFinder is running.
                        grid.Active = false;
                    }
                    else if (friendElement.Friend.IsJoined && LiveManager.IsConnected)
                    {
                        // Selected friend has joined our session, just go to the sharing room.

                        ActivateSharingScreen();

                        grid.Active = false;
                    }
                    else if (!friendElement.Friend.InvitedThem)
                    {
                        // Start sending an invitation to this friend.

                        friendElement.Friend.InvitingThem = true;

                        if (!LiveManager.IsConnected)
                        {
                            openingInviteGuideMessage.Activate();

                            LiveManager.ClearQueuedOperations(null);

                            // We must have a session open before we can send an invite.
                            // Start creating the network session.
                            SessionCreator createOp = new SessionCreator(SessionCreatorComplete_StartInvitingFriend, friendElement, this);
                            createOp.Queue();
                        }
                        else
                        {
                            // Session is already open, no need to create it before sending the invite.
                            StartInvitingFriend(friendElement);
                        }
                    }
                }
            }
            else if (serverElement != null)
            {
                if (serverElement.IsOnline)
                {
                    StartJumpingIn(serverElement.GamerTag, serverElement);

                    // Don't allow the user to interact with the share hub while the SessionFinder is running.
                    grid.Active = false;
                }
            }
        }

        private void StartJumpingIn(string gamerTag, object param)
        {
            // Turn off background updates to friend profiles.
            LiveManager.FriendUpdatesEnabled = false;
            // Turn off background updates to friend joinable states.
            LiveManager.JoinableUpdatesEnabled = false;

            // TODO: Warn of potentially "destructive" action. Player may have invited people to join their session.
            // This action closes the local session, invalidating the invites.
            LiveManager.CloseSession();

            // Cancel pending friend profile and joinable state queries.
            LiveManager.ClearQueuedOperations(null);

            // Show the "initializing sharing room" notification dialog.
            openingSharingRoomMessage.Activate();

            // Start finding our friend's network session.
            SessionFinder finderOp = new SessionFinder(gamerTag, FindSessionComplete_Join, param, this);
            finderOp.Queue();
        }

        private void SessionCreatorComplete_ActivateSharingScreen(AsyncLiveOperation op)
        {
            if (op.Succeeded)
            {
                SessionCreator createOp = op as SessionCreator;

                LiveManager.Session = createOp.Session;
                createOp.Session = null; // avoid session dispose

                ActivateSharingScreen();
            }
            else
            {
                // Failed to create the session for some reason.
                // TODO: Show a message here?


                // Restart the periodic update of profiles and joinable states.
                LiveManager.FriendUpdatesEnabled = true;
                LiveManager.JoinableUpdatesEnabled = true;

                // Re-enable the share hub menu.
                shared.grid.Active = true;
            }
        }

        private void SessionCreatorComplete_StartInvitingFriend(AsyncLiveOperation op)
        {
            openingSharingRoomMessage.Deactivate();
            openingInviteGuideMessage.Deactivate();

            // We needed to create a session before sending out an invitation to our friend.
            // If we're here, then the session create process is complete.
            UIGridShareFriendElement shareHubElement = op.Param as UIGridShareFriendElement;

            if (op.Succeeded)
            {
                // Session was created, start inviting the friend.

                SessionCreator createOp = op as SessionCreator;

                LiveManager.Session = createOp.Session;
                createOp.Session = null; // avoid session dispose

                LiveManager.Session.GamerJoined += shared.Session_GamerJoined;
                LiveManager.Session.GamerLeft += shared.Session_GamerLeft;

                StartInvitingFriend(shareHubElement);
            }
            else
            {
                // TODO: show a message about the failure.
            }
        }

        private void StartInvitingFriend(UIGridShareFriendElement shareHubElement)
        {
            Debug.Assert(LiveManager.IsConnected);

            try
            {
                List<Gamer> list = new List<Gamer>();
                list.Add(shareHubElement.Friend.FriendGamer);
                Guide.ShowGameInvite(GamePadInput.LastTouched, list);
                shareHubElement.Friend.InvitingThem = false;
            }
            catch
            {
                // If the controller is not signed into live, we'll get a GamerPriviledgesException.
            }
        }

        private void FindSessionComplete_Join(AsyncLiveOperation op)
        {
            UIGridShareFriendElement friendElement = op.Param as UIGridShareFriendElement;

            if (op.Succeeded)
            {
                SessionFinder finderOp = op as SessionFinder;

                if (finderOp.AvailableSessions.Count > 0)
                {
                    // We found an available session. Before we can start joining it, we must cancel all pending LIVE
                    // operations. Some of them may be queued attempts to find joinable sessions (incompatible with
                    // joining a session), and the others will be profile updates (no longer relevant as we're leaving
                    // this screen).
                    LiveManager.ClearQueuedOperations(null);

                    openingSharingRoomMessage.Activate();

                    // Start joining the session.
                    AvailableSessionJoiner joinerOp = new AvailableSessionJoiner(finderOp.AvailableSessions[0], JoinSessionComplete_ActivateSharingScreen, op.Param, this);

                    // This operation must start before the network session updates again or
                    // the available sessions collection becomes invalid for some reason.
                    joinerOp.Queue(true);

                    finderOp.AvailableSessions = null;  // prevent dispose, since we passed the available session to the joiner.

                    // TODO: Show joining session message here..
                }
                else
                {
                    // We didn't find the session. Re-enable the share hub's periodic LIVE operations.
                    LiveManager.FriendUpdatesEnabled = true;
                    LiveManager.JoinableUpdatesEnabled = true;

                    // Session is no longer available, so mark friend as not joinable.
                    // This may change if we detect they are joinable again.
                    if (friendElement != null)
                    {
                        friendElement.Friend.IsJoinable = false;
                    }

                    // Not leaving the share hub anymore, since we failed to join the session.
                    shared.grid.Active = true;
                    openingSharingRoomMessage.Deactivate();
                }
            }
            else
            {
                // We didn't find the session. Re-enable the share hub's periodic LIVE operations.
                LiveManager.FriendUpdatesEnabled = true;
                LiveManager.JoinableUpdatesEnabled = true;

                // The join operation failed, so mark friend as not joinable.
                // This may change if we detect they are joinable again.
                if (friendElement != null)
                {
                    friendElement.Friend.IsJoinable = false;
                }

                // Not leaving the share hub anymore, since we failed to join the session.
                shared.grid.Active = true;
                openingSharingRoomMessage.Deactivate();
            }
        }

        private void JoinSessionComplete_ActivateSharingScreen(AsyncLiveOperation op)
        {
            openingSharingRoomMessage.Deactivate();
            openingInviteGuideMessage.Deactivate();

            // In case we were joining by accepting an invite, clear that flag now.
            LiveManager.JoiningShareInvitation = false;

            if (op.Succeeded)
            {
                SessionJoiner joinerOp = op as SessionJoiner;

                LiveManager.Session = joinerOp.Session;
                joinerOp.Session = null;    // avoid session dispose, since we joined it.

                ActivateSharingScreen();
            }
            else
            {
                // Failed to join the session for some reason, so re-enable the share hub's periodic LIVE operations.
                LiveManager.FriendUpdatesEnabled = true;
                LiveManager.JoinableUpdatesEnabled = true;

                shared.grid.Active = true;
            }
        }

        public void OnCancel(UIGrid grid)
        {
            // Never mind. Just deactivate the share hub and reactivate main menu.
            ActivateMainMenu();

        }   // end of OnCancel()

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            bool result = false;

            if (state != pendingState)
            {
                if (pendingState == States.Active)
                {
                    updateList.Add(updateObj);
                    updateObj.Activate();
                    renderList.Add(renderObj);
                    renderObj.Activate();

                    shared.Activate();
                }
                else
                {
                    shared.Deactivate();

                    renderObj.Deactivate();
                    renderList.Remove(renderObj);
                    updateObj.Deactivate();
                    updateList.Remove(updateObj);
                }

                state = pendingState;
            }

            return result;
        }

        public override void Activate()
        {
            if (state != States.Active)
            {
                LiveManager.CloseSession();

                // Do stack handling here.  If we do it in the update object we have no
                // clue which order things get pushed and popped and madness ensues.
                CommandStack.Push(commandMap);

                pendingState = States.Active;
                BokuGame.objectListDirty = true;

                DeactivateMessages();
            }
        }

        public override void Deactivate()
        {
            // Do stack handling here.  If we do it in the update object we have no
            // clue which order things get pushed and popped and madness ensues.
            CommandStack.Pop(commandMap);

            if (state != States.Inactive)
            {
                shared.Deactivate();

                pendingState = States.Inactive;
                BokuGame.objectListDirty = true;

                DeactivateMessages();
            }
        }

        private void DeactivateMessages()
        {
            openingSharingRoomMessage.Deactivate();
            openingInviteGuideMessage.Deactivate();
        }

        public void LoadContent(bool immediate)
        {
            BokuGame.Load(shared, immediate);
            BokuGame.Load(renderObj, immediate);
            BokuGame.Load(openingSharingRoomMessage, immediate);
            BokuGame.Load(openingInviteGuideMessage, immediate);
        }

        public void InitDeviceResources(GraphicsDevice device)
        {
            shared.InitDeviceResources(device);
            renderObj.InitDeviceResources(device);
        }

        public void UnloadContent()
        {
            BokuGame.Unload(shared);
            BokuGame.Unload(renderObj);
            BokuGame.Unload(openingSharingRoomMessage);
            BokuGame.Unload(openingInviteGuideMessage);
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="graphics"></param>
        public void DeviceReset(GraphicsDevice device)
        {
            BokuGame.DeviceReset(shared, device);
            BokuGame.DeviceReset(renderObj, device);
            BokuGame.DeviceReset(openingSharingRoomMessage, device);
            BokuGame.DeviceReset(openingInviteGuideMessage, device);
        }

    }
}
