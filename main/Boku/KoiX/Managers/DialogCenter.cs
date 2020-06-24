
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KoiX;
using KoiX.UI.Dialogs;

namespace KoiX.Managers
{
    using ButtonSet = MessageDialog.ButtonSet;

    /// <summary>
    /// Static class which holds instances of commonly used dialogs.
    /// This provides a common place to both create the dialogs and
    /// to get them for launching.
    /// 
    /// Initial implementation is to just create all the dialogs up
    /// front.  We may want to move to a lazy creation approach or
    /// even create for each use and kill when done.
    /// </summary>
    public static class DialogCenter
    {
        static public MessageDialog NewVersionAvailableDialog;
        static public MessageDialog ImportNeedsNewerVersionDialog;
        static public MessageDialog GamepadDisconnetDialog;

        static public MessageDialog DeleteConfirmDialog;
        static public MessageDialog DeleteConfirmLinkedDialog;
        static public MessageDialog LevelExportedDialog;
        static public MessageDialog LinkedLevelExportedDialog;
        static public MessageDialog ReportAbuseDialog;

        static public MessageDialog LevelNotFirstDialog;        // User is trying to play a "middle" level from a set of linked levels.
        static public MessageDialog BrokenLinkDialog;
        static public MessageDialog BrokenLinkExportDialog;     // User is trying to export a level with broken links.
        static public MessageDialog TargetAlreadyLinkedDialog;

        static public MessageDialog OverwriteWarningDialog;     // Used in SavelLevelDialog.
        static public MessageDialog PreserveLinksDialog;        // Used in SavelLevelDialog.

        static public MessageDialog NoCommunityDialog;          // Community is not responding.
        static public MessageDialog CommunityUploadFailedDialog;
        static public MessageDialog CommunityUploadSuccessDialog;

        static public MessageDialog DownloadLinkedSuccessDialog;

        static public GraphicMessageDialog LoadingLevelWaitDialog;
        static public GraphicMessageDialog TerrainProcessingDialog;

        static public GameOverDialog GameOverDialog;

        static public BrushTypeDialog BrushTypeDialog;
        static public TerrainMaterialDialog TerrainMaterialDialog;
        static public WaterTypeDialog WaterTypeDialog;

        static public TagsDialog TagsDialog;

        static public void Init()
        {
            //NewVersionAvailableDialog = new MessageDialog("");

            ImportNeedsNewerVersionDialog = new MessageDialog(titleId: "textDialog.information", messageId: "newerVersionDialog.text", buttons: ButtonSet.Continue);

            GamepadDisconnetDialog = new MessageDialog(titleId: "warning.warning", messageId: "gamePadInputDialog.unPluggedMoron", buttons: ButtonSet.Continue);
            //GamepadDisconnetDialog = new MessageDialog(titleId: "warning.warning", messageId: "gamePadInputDialog.unPluggedMoron", buttons: ButtonSet.Ok | ButtonSet.Cancel | ButtonSet.Continue | ButtonSet.Delete);

            DeleteConfirmDialog = new MessageDialog(titleId: "textDialog.deleteWorld", messageId: "textDialog.deletePrompt", buttons: ButtonSet.Delete | ButtonSet.Cancel);
            DeleteConfirmLinkedDialog = new MessageDialog(titleId: "textDialog.deleteWorld", messageId: "textDialog.deleteLinkedPrompt", buttons: ButtonSet.Delete | ButtonSet.Cancel);

            LevelExportedDialog = new MessageDialog(titleId: "textDialog.status", messageId: "textDialog.levelExported", buttons: ButtonSet.Continue);
            LinkedLevelExportedDialog = new MessageDialog(titleId: "textDialog.status", messageId: "textDialog.linkedLevelExport", buttons: ButtonSet.Continue);
            ReportAbuseDialog = new MessageDialog(titleId: "textDialog.reportAbuse", messageId: "textDialog.reportAbusePrompt", buttons: ButtonSet.Ok | ButtonSet.Cancel);

            LevelNotFirstDialog = new MessageDialog(titleId: "warning.warning", messageId: "loadLevelMenu.levelNotFirstMessage", buttons: ButtonSet.Yes | ButtonSet.No);
            BrokenLinkDialog = new MessageDialog(titleId: "warning.warning", messageId: "loadLevelMenu.levelLinksBrokenMessage", buttons: ButtonSet.Continue | ButtonSet.Cancel);
            BrokenLinkExportDialog = new MessageDialog(titleId: "warning.warning", messageId: "loadLevelMenu.levelLinksBrokenMessage", buttons: ButtonSet.Continue | ButtonSet.Cancel);
            TargetAlreadyLinkedDialog = new MessageDialog(titleId: "warning.warning", messageId: "loadLevelMenu.targetAlreadyLinkedMessage", buttons: ButtonSet.Continue | ButtonSet.Cancel);

            OverwriteWarningDialog = new MessageDialog(titleId: "warning.warning", messageId: "saveLevelDialog.overwriteWarning", buttons: ButtonSet.Overwrite | ButtonSet.Cancel | ButtonSet.IncrementAndSave);
            PreserveLinksDialog = new MessageDialog(titleId: "warning.warning", messageId: "loadLevelMenu.preserveLinksMessage", buttons: ButtonSet.Yes | ButtonSet.No); 

            NoCommunityDialog = new MessageDialog(titleId: "warning.error", messageId: "miniHub.noCommunityMessage", buttons: ButtonSet.Continue);
            CommunityUploadFailedDialog = new MessageDialog(titleId: "warning.error", messageId: "loadLevelMenu.uploadToCommunityFailed", buttons: ButtonSet.Continue);
            CommunityUploadSuccessDialog = new MessageDialog(titleId: "warning.success", messageId: "minihub.shareSuccessMessage", buttons: ButtonSet.Continue);

            DownloadLinkedSuccessDialog = new MessageDialog(titleId: "warning.success", messageId: "loadLevelMenu.confirmLinkedDownloadMessage", buttons: ButtonSet.Continue);

            LoadingLevelWaitDialog = new GraphicMessageDialog(messageId: "loadLevelMenu.loadingLevelMessage");
            Texture2D waitTexture = KoiLibrary.LoadTexture2D(@"Textures\Terrain\WaitPicture");
            LoadingLevelWaitDialog.AddTexture(waitTexture);

            TerrainProcessingDialog = new GraphicMessageDialog();
            TerrainProcessingDialog.FrameDelay = 0.2f;
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_01"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_02"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_03"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_04"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_05"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_06"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_07"));
            TerrainProcessingDialog.AddTexture(KoiLibrary.LoadTexture2D(@"Textures\Terrain\busyframe_08"));

            GameOverDialog = new GameOverDialog();

            BrushTypeDialog = new BrushTypeDialog(RectangleF.EmptyRect, "tools.brush");
            TerrainMaterialDialog = new TerrainMaterialDialog(RectangleF.EmptyRect, "tools.material");
            WaterTypeDialog = new WaterTypeDialog(RectangleF.EmptyRect, "tools.waterAdd");

            TagsDialog = new TagsDialog();

            LoadContent();

        }   // end of Init()

        static public void LoadContent()
        {
            BrushTypeDialog.LoadContent();
            TerrainMaterialDialog.LoadContent();
            WaterTypeDialog.LoadContent();

        }   // en dof LoadContent()

    }   // end of class DialogCenter

}   // end of namespace KoiX.Managers
