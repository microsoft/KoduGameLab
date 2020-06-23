
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BokuShared.Wire;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Boku.Base;
using Boku.Common;
using Boku.Common.Xml;
using Boku.Common.Gesture;
using Boku.UI2D;
using Boku.Fx;
using Boku.Web;
using Point = Microsoft.Xna.Framework.Point;

namespace Boku
{
    /// <summary>
    /// Handles dialogs used for sharing levels.
    /// </summary>
    public class CommunityShareMenu : GameObject, INeedsDeviceReset
    {
        private LevelMetadata CurWorld;

        public CommunityShareMenu()
        {
            // signedInMessage
            {
                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                {
                    // User chose "upload"

                    //find the first link
                    LevelMetadata level = CurWorld;
                    level = level.FindFirstLink();

                    string folderName = Utils.FolderNameFromFlags(level.Genres);
                    string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

                    // Read it back from disk and start uploading it to the community.
                    BokuShared.Wire.WorldPacket packet = XmlDataHelper.ReadWorldPacketFromDisk(fullPath);

                    UploadWorldData(packet, level);

                    // Deactivate dialog.
                    dialog.Deactivate();
                    Deactivate();
                };
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "cancel"
                    // Deactivate dialog.
                    dialog.Deactivate();
                    Deactivate();
                };

                ModularMessageDialog.ButtonHandler handlerY = delegate(ModularMessageDialog dialog)
                {
                    // Deactivate dialog.
                    dialog.Deactivate();
                    Deactivate();
                };
            }

            // signedOutMessage
            {
                ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
                {
                    // Deactivate dialog.
                    dialog.Deactivate();
                };
                ModularMessageDialog.ButtonHandler handlerB = delegate(ModularMessageDialog dialog)
                {
                    // User chose "cancel"
                    // Deactivate dialog.
                    dialog.Deactivate();
                    Deactivate();
                };
                ModularMessageDialog.ButtonHandler handlerY = delegate(ModularMessageDialog dialog)
                {
                    // User chose "upload anonymous"
                    LevelMetadata level = CurWorld;

                    //find the first link
                    level = level.FindFirstLink();

                    string folderName = Utils.FolderNameFromFlags(level.Genres);
                    string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

                    // Share.
                    // Check to see if the community server is reachable before sharing level.
                    if (!Web.Community.Async_Ping(Callback_Ping, fullPath))
                    {
                        ShowNoCommunityDialog();
                    }

                    // Deactivate dialog.
                    dialog.Deactivate();
                    Deactivate();

                };
            }
        }

        private void UploadWorldData(WorldPacket packet, LevelMetadata level)
        {
            if (packet == null)
            {
                ShowShareErrorDialog("Load failed.");
            }
            else if (0 == Web.Community.Async_PutWorldData(packet, Callback_PutWorldData, level))
            {
                ShowShareErrorDialog("Upload failed.");
            }
        }

        /// <summary>
        /// Recreate render targets
        /// </summary>
        /// <param name="device"></param>
        public void DeviceReset(GraphicsDevice device)
        {
        }

        public void Update()
        {
            if (Active)
            {
            }

        }   // end of Update()

        public void DeactivateMenu(ModularMessageDialog dialog)
        {
            //close the dialog
            dialog.Deactivate();
            Deactivate();
        }

        private void ShowWarning(string text)
        {
            //handler for "continue" - user wants to play anyway
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();
            };

            if (null == text)
            {
                text = "";
            }
            string labelA = Strings.Localize("textDialog.ok");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA);
        }

        //helper functions to display dialogs
        public void ShowNoCommunityDialog()
        {
            string text = Strings.Localize("miniHub.noCommunityMessage");
            string labelA = Strings.Localize("textDialog.back");
            ModularMessageDialogManager.Instance.AddDialog(text, DeactivateMenu, labelA);
        }
        public void ShowShareErrorDialog(string error)
        {
            string text =String.Format("{0} {1}",Strings.Localize("miniHub.noSharingMessage"),error);
            string labelA = Strings.Localize("textDialog.back");
            ModularMessageDialogManager.Instance.AddDialog(text, DeactivateMenu, labelA);
        }
        public void ShowBrokenLevelShareWarning()
        {
            ShowWarning(Strings.Localize("loadLevelMenu.brokenLevelShareMessage"));
        }
        public void ShowConfirmLinkedShareDialog()
        {
            //handler for if user agrees to share all levels
            ModularMessageDialog.ButtonHandler handlerA = delegate(ModularMessageDialog dialog)
            {
                //close the dialog
                dialog.Deactivate();

                ContinueCommunityShare();
            };

            string text = Strings.Localize("loadLevelMenu.confirmLinkedShareMessage");
            string labelA = Strings.Localize("textDialog.yes");
            string labelB = Strings.Localize("textDialog.no");
            ModularMessageDialogManager.Instance.AddDialog(text, handlerA, labelA, DeactivateMenu, labelB);
        }
        public void ShowShareSuccessDialog()
        {
            string text = Strings.Localize("miniHub.shareSuccessMessage");
            string labelB = Strings.Localize("textDialog.back");
            ModularMessageDialogManager.Instance.AddDialog(text, null, null, DeactivateMenu, labelB);
        }


        public void PopupOnCommunityShare()
        {
            var level = CurWorld;

                //Check if level has links.
            if (level.LinkedToLevel != null || level.LinkedFromLevel != null)
            {
                //check if the chosen level has any broken links - if so, warn the player
                LevelMetadata brokenLevel = null;
                bool forwardsLinkBroken = false;
                if (level.FindBrokenLink(ref brokenLevel, ref forwardsLinkBroken))
                {
                    ShowBrokenLevelShareWarning();
                }
                else
                {
                    //prompt to confirm linked share
                    ShowConfirmLinkedShareDialog();
                }
            }
            else
            {
                //not a linked level, share as per normal
                ContinueCommunityShare();
            }
        }
        internal void ContinueCommunityShare()
        {
            LevelMetadata level = CurWorld;

            //TODO: check for broken links?
            //always start the share on the first level in the set
            level = level.FindFirstLink();

            string folderName = Utils.FolderNameFromFlags(level.Genres);
            string fullPath = BokuGame.Settings.MediaPath + folderName + level.WorldId.ToString() + @".Xml";

            // Share.
            // Check to see if the community server is reachable before sharing level.
            if (!Web.Community.Async_Ping(Callback_Ping, fullPath))
            {
                ShowNoCommunityDialog();
            }
        }   // end of PopupOnCommunityShare()

        public void CheckCommunityCallback(AsyncResult resultObj)
        {
            Web.AsyncResult_UserLogin result = (Web.AsyncResult_UserLogin)resultObj;

            if (result.Success)
            {
                // Yes, the community site is alive.
            }
            else
            {
                // Put up dialog with error for no community site.
                ShowNoCommunityDialog();
            }
        }

        /// <summary>
        /// Callback that results from testing whether or not the community server is active.
        /// </summary>
        /// <param name="resultObj"></param>
        public void Callback_Ping(AsyncResult resultObj)
        {
            AsyncResult result = (AsyncResult)resultObj;

            if (result.Success)
            {
                // Read it back from disk and start uploading it to the community.
                BokuShared.Wire.WorldPacket packet = XmlDataHelper.ReadWorldPacketFromDisk(result.Param as string);

                LevelMetadata level = XmlDataHelper.LoadMetadataByGenre(packet.Info.WorldId, (BokuShared.Genres)packet.Info.Genres);

                UploadWorldData(packet, level);
            }
            else
            {
                ShowShareErrorDialog("Login failed.");
            }
        }   // end of Callback_Ping()

        public void Callback_PutWorldData(AsyncResult result)
        {
            LevelMetadata uploadedLevel = result.Param as LevelMetadata;

            if (result.Success && uploadedLevel != null && uploadedLevel.LinkedToLevel != null)
            {
                LevelMetadata nextLevel = uploadedLevel.NextLink();

                if (nextLevel != null)
                {
                    string folderName = Utils.FolderNameFromFlags(nextLevel.Genres);
                    string fullPath = BokuGame.Settings.MediaPath + folderName + nextLevel.WorldId.ToString() + @".Xml";

                    // Read it back from disk and start uploading it to the community.
                    BokuShared.Wire.WorldPacket packet = XmlDataHelper.ReadWorldPacketFromDisk(fullPath);

                    UploadWorldData(packet, nextLevel);

                    return;
                }
            }

            if (result.Success)
            {
                ShowShareSuccessDialog();
            }
            else
            {
                ShowShareErrorDialog("Share failed.");
            }
        }   // end of Callback_PutWorldData()

        public void LoadContent(bool immediate)
        {
        }   // end of LoadContent()

        public void InitDeviceResources(GraphicsDevice device)
        {
        }

        public void UnloadContent()
        {
        }   // end of UnloadContent()

        public void Render()
        {
            if (!Active)
            {
                return;
            }

            InGame.RenderMessages();//Needed??
        }

        private enum States
        {
            Inactive,
            Active,
        }
        private States state = States.Inactive;

        public bool Active
        {
            get { return (state == States.Active); }
        }

        public override bool Refresh(List<UpdateObject> updateList, List<RenderObject> renderList)
        {
            Debug.Assert(false, "This object is not designed to be put into any lists.");
            return true;
        }   // end of Refresh()

        override public void Activate()
        {
            if (state != States.Active)
            {
                state = States.Active;
                BokuGame.objectListDirty = true;
            }
        }

        public void Activate(LevelMetadata level)
        {
            CurWorld = level;
            PopupOnCommunityShare();
            Activate();
        }
        override public void Deactivate()
        {
            state = States.Inactive;
        }   // End of Deactivate()

    }   // end of class 

}   // end of namespace Boku
