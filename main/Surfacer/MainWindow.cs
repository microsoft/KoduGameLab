// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using Boku.Fx;

using Microsoft.Xna.Framework;

namespace Surfacer
{
    public partial class MainWindow : Form
    {
        // winforms boilerplate
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            LoadDictionary();


            BuildSurfaceList();
            SurfaceList.SelectedIndex = 0;

            ShowStandardTints();

            foreach (CharacterEdit charEd in characterEdits)
            {
                charEd.BuildSurfacePickers(surfaces);
            }
        }

        private void BuildSurfaceList()
        {
            SurfaceList.Items.Clear();
            foreach (Surface surf in surfaces.Surfaces)
            {
                SurfaceList.Items.Add(surf);
            }
        }
        
        // Dictionary of all known character surfaces
        // This does not contain mappings from characters to used surfaces; that's done separately
        private SurfaceDict surfaces;

        // Surface currently selected for editing
        private Surface surfaceBeingEdited;

        // Have we changed the current surface?
        private bool _surfaceIsDirty;
        private bool SurfaceIsDirty
        {
            get
            {
                return _surfaceIsDirty;
            }
            set
            {
                _surfaceIsDirty = value;
//                SaveBtn.Enabled = value;
                RevertBtn.Enabled = value;
            }
        }

        #region managing the dictionary file

        // Standard relative path to the dictionary at runtime
        string DictionaryFileName
        {
            get
            {
                // NOTE that this comes from the content directory of the running app, 
                // not the project directory. Once you have a surface dictionary you like,
                // you have to move it and check it in manually.
                return @"Content\Xml\Actors\SurfaceDict.xml";
            }
        }

        // Load the active dictionary for editing
        private void LoadDictionary()
        {
            LoadDictionary(DictionaryFileName);
        }

        // Load from a particular file
        private void LoadDictionary(string fileName)
        {
             surfaces = SurfaceDict.Load(DictionaryFileName, BokuShared.FileStorageHelper.Instance);
        }

        // Back up the active dictionary file and replace it with our currently edited version.
        private void SaveDictionary()
        {
            SaveDictionary(DictionaryFileName);
        }

        // Save the dictionary to the given filename.
        // If the filename exists, back it up by appending a version number
        private void SaveDictionary(string fileName)
        {
            if(System.IO.File.Exists(fileName))
            {
                string backUpFile = BackupFileName(fileName);
                System.IO.File.Copy(fileName, backUpFile);
                System.IO.File.Delete(fileName);
            }

            // copy the edited surface back to the in-memory version
            // this surface is still in the surface dictionary, so we're now set to persist the whole dictionary
            GatherSurfaceFromUI(surfaceBeingEdited);

            // overwrite the active surface dictionary
            surfaces.Save(fileName, BokuShared.FileStorageHelper.Instance);
            SurfaceIsDirty = false;
        }



        #endregion

        private void SurfaceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SurfaceList.SelectedItem is Surface)
            {
                EditSurface(SurfaceList.SelectedItem as Surface);
                UpdatePreviewSwatches();
            }
        }

        /* Load the given surface for editing.
         * TODO: - if the currently loaded surface is not saved, warn before discarding and
         *         give an opportunity to cancel.
         *       - do we need to add a bumpscale?
         *       - what do we do with the noise strength?
         */
        private bool EditSurface(Surface surface)
        {
            // if we have changes and we're switching to edit a different texture
            if (surfaceBeingEdited != null && SurfaceIsDirty && surface != surfaceBeingEdited)
            {
                // push the edited surface into the dictionaryS
                GatherSurfaceFromUI(surfaceBeingEdited);
            }

            surfaceBeingEdited = surface;
            SurfaceIsDirty = false;

            AnisotropyXNum.Value = (decimal)surface.Aniso.X;
            AnisotropyYNum.Value = (decimal)surface.Aniso.Y;
            EnvIntensityNum.Value = (decimal)surface.EnvMapIntensity;
            BloomNum.Value = (decimal)surface.Bloom;
            BumpTilingNum.Value = (decimal)surface.BumpScale;
            BumpIntensityNum.Value = (decimal)surface.BumpIntensity;

            DiffuseBtn.BackColor = LegalColor(surface.Diffuse);
            EmissiveBtn.BackColor = LegalColor(surface.Emissive);
            NameFld.Text = surface.Name;

            SpecularBtn.BackColor = LegalColor(surface.SpecularColor);
            SpecPowNum.Value = (decimal)surface.SpecularPower;

            SetTintMul(surface.Tintable);
            WrapNum.Value = (decimal)surface.Wrap;
            return true;
        }

        private void GatherSurfaceFromUI(Surface surface)
        {
            surface.Aniso = new Vector2((float)AnisotropyXNum.Value, (float)AnisotropyYNum.Value);
            surface.EnvMapIntensity = (float)EnvIntensityNum.Value;

            surface.Bloom = (float)BloomNum.Value;
            surface.BumpIntensity = (float)BumpIntensityNum.Value;
            surface.BumpScale = (float)BumpTilingNum.Value;
            surface.Diffuse = MakeV3Color(DiffuseBtn.BackColor);
            surface.Emissive = MakeV3Color(EmissiveBtn.BackColor);
            surface.Name = NameFld.Text;
            surface.SpecularColor = MakeV3Color(SpecularBtn.BackColor);
            surface.SpecularPower = (float)SpecPowNum.Value;
            surface.Tintable = GetTintMul();
            surface.Wrap = (float)WrapNum.Value;
        }

        #region tint multiplier UI
        // The tint multiplier (called "Tintable" in the shader) is allowed to have components outside of 0..1
        // The UI for entering these consists of a swatch (to allow visual picking if components are inside 0..1)
        // as well as three numeric entry fields for entering numeric components directly.
        private void SetTintMul(Vector3 tintMul)
        {
            SetTintMul(tintMul.X, tintMul.Y, tintMul.Z);
        }

        private void SetTintMul(System.Drawing.Color color)
        {
            SetTintMul((float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f);
        }

        private void SetTintMul(float r, float g, float b)
        {
            // fill in the UI for numeric manipulation of the components
            TintMulRNum.Value = (decimal)r;
            TintMulGNum.Value = (decimal)g;
            TintMulBNum.Value = (decimal)b;

            // try to show as a visible color, but may not be accurate if components are outside of 0..1
            TintMulBtn.BackColor = LegalColor(r, g, b);
            UpdatePreviewSwatches();
        }

        private Vector3 GetTintMul()
        {
            Vector3 result;
            result.X = (float)TintMulRNum.Value;
            result.Y = (float)TintMulGNum.Value;
            result.Z = (float)TintMulBNum.Value;
            return result;
        }

        /* Copy tint multiplication colors from individual numeric fields
         * to the color swatch and update the preview swatches.
         */
        private void UpdateTintMulFromNumericComponentUI()
        {
            float r = (float)TintMulRNum.Value;
            float g = (float)TintMulGNum.Value;
            float b = (float)TintMulBNum.Value;

            TintMulBtn.BackColor = LegalColor(r, g, b);

            SurfaceIsDirty = true;
            UpdatePreviewSwatches();
        }
        #endregion tint multiplier UI

        #region standard user colors and preview swatches
        public enum Colors
        {
            Black,
            White,
            Grey,
            Red,
            Green,
            Blue,
            Orange,
            Yellow,
            Purple,
            Pink,
            Brown
        }

        /* Load up the swatches for the immutable user-selectable tints */
        private void ShowStandardTints()
        {
            BlackBef.BackColor = LegalColor(StandardColor(Colors.Black));
            WhiteBef.BackColor = LegalColor(StandardColor(Colors.White));
            GreyBef.BackColor = LegalColor(StandardColor(Colors.Grey));
            RedBef.BackColor = LegalColor(StandardColor(Colors.Red));
            GreenBef.BackColor = LegalColor(StandardColor(Colors.Green));
            BlueBef.BackColor = LegalColor(StandardColor(Colors.Blue));
            OrangeBef.BackColor = LegalColor(StandardColor(Colors.Orange));
            YellowBef.BackColor = LegalColor(StandardColor(Colors.Yellow));
            PurpleBef.BackColor = LegalColor(StandardColor(Colors.Purple));
            PinkBef.BackColor = LegalColor(StandardColor(Colors.Pink));
            BrownBef.BackColor = LegalColor(StandardColor(Colors.Brown));
        }

        /* Show how the standard tints are affected by the current shader */
        private void UpdatePreviewSwatches()
        {
            UpdateSwatch(BlackBef, BlackAft);
            UpdateSwatch(WhiteBef, WhiteAft);
            UpdateSwatch(GreyBef, GreyAft);
            UpdateSwatch(RedBef, RedAft);
            UpdateSwatch(GreenBef, GreenAft);
            UpdateSwatch(BlueBef, BlueAft);
            UpdateSwatch(OrangeBef, OrangeAft);
            UpdateSwatch(YellowBef, YellowAft);
            UpdateSwatch(PurpleBef, PurpleAft);
            UpdateSwatch(PinkBef, PinkAft);
            UpdateSwatch(BrownBef, BrownAft);
        }

        /* Update a single preview swatch */
        private void UpdateSwatch(PictureBox before, PictureBox after)
        {
            Vector3 befV = MakeV3Color(before.BackColor);
//            Vector3 aftV = MakeV3Color(after.BackColor);

            Vector3 colorScale = GetTintMul();
            Vector3 diffuse = MakeV3Color(DiffuseBtn.BackColor);

            Vector3 scaledTint = colorScale * befV;
            Vector3 result = diffuse + scaledTint;

            after.BackColor = LegalColor(result);
        }

        // these are the standard tint colors COPIED from Boku\Boku\Base\Classification.cs
        public static Vector3 StandardColor(Colors color)
        {
            Vector3 result = new Vector3(1.0f);
            switch (color)
            {
                case Colors.Black:
                    result = MakeV3Color(34, 37, 36);
                    break;
                case Colors.White:
                    result = MakeV3Color(250, 250, 233);
                    break;
                case Colors.Grey:
                    result = MakeV3Color(147, 138, 135);
                    break;
                case Colors.Red:
                    result = MakeV3Color(239, 59, 46);
                    break;
                case Colors.Green:
                    result = MakeV3Color(32, 206, 87);
                    break;
                case Colors.Blue:
                    result = MakeV3Color(61, 161, 224);
                    break;
                case Colors.Orange:
                    result = MakeV3Color(255, 160, 31);
                    break;
                case Colors.Yellow:
                    result = MakeV3Color(232, 255, 118);
                    break;
                case Colors.Purple:
                    result = MakeV3Color(109, 70, 123);
                    break;
                case Colors.Pink:
                    result = MakeV3Color(255, 149, 162);
                    break;
                case Colors.Brown:
                    result = MakeV3Color(161, 78, 41);
                    break;
            }
            return result;
        }
        #endregion

        #region color convenience utilitities
        // Given an arbitrary vector3, convert it to a valid System.Color
        System.Drawing.Color LegalColor(Vector3 vector)
        {
            return LegalColor(vector.X, vector.Y, vector.Z);
        }

        // Given rgb values which may have negative or > 1 values, clamp
        // them to the 0...1 range and return a System.Color
        System.Drawing.Color LegalColor(float r, float g, float b)
        {
            int ir = (int)(Math.Min(Math.Abs(r), 1.0f) * 255.0f);
            int ig = (int)(Math.Min(Math.Abs(g), 1.0f) * 255.0f);
            int ib = (int)(Math.Min(Math.Abs(b), 1.0f) * 255.0f);

            return System.Drawing.Color.FromArgb(ir, ig, ib);
        }

        static private Vector3 MakeV3Color(int r, int g, int b)
        {
            float fR = (float)r;
            float fG = (float)g;
            float fB = (float)b;

            return new Vector3(fR / 255.0f, fG / 255.0f, fB / 255.0f);
        }

        static private Vector3 MakeV3Color(System.Drawing.Color color)
        {
            return MakeV3Color(color.R, color.G, color.B);
        }

        #endregion


        private void TintMulRNum_ValueChanged(object sender, EventArgs e)
        {
            UpdateTintMulFromNumericComponentUI();
        }

        private void TintMulGNum_ValueChanged(object sender, EventArgs e)
        {
            UpdateTintMulFromNumericComponentUI();

        }

        private void TintMulBNum_ValueChanged(object sender, EventArgs e)
        {
            UpdateTintMulFromNumericComponentUI();

        }

        private void DiffuseBtn_Click(object sender, EventArgs e)
        {
            SimpleColorPick(DiffuseBtn);
        }


        /* Given a button, open a color picker and allow the user to choose a color.
         * If the color is accepted (i.e. the use does not press cancel,) set
         * the background color of the button and refresh the color preview swatches.
         * Returns true if the user confirmed, false if the user cancelled.
         */
        private bool SimpleColorPick(Button btn)
        {
            bool result = false;
            ColorDialog color = new System.Windows.Forms.ColorDialog();
            color.Color = btn.BackColor;
            color.FullOpen = true;
            DialogResult res = color.ShowDialog();
            if (res == DialogResult.OK)
            {
                btn.BackColor = color.Color;
                UpdatePreviewSwatches();
                SurfaceIsDirty = true;
                result = true;
            }
            return result;
        }

        private void TintMulBtn_Click(object sender, EventArgs e)
        {
            if (SimpleColorPick(TintMulBtn))
            {
                SetTintMul(TintMulBtn.BackColor);
            }
        }

        private void EmissiveBtn_Click(object sender, EventArgs e)
        {
            SimpleColorPick(EmissiveBtn);
        }

        private void SpecularBtn_Click(object sender, EventArgs e)
        {
            SimpleColorPick(SpecularBtn);
        }

        private void NewBtn_Click(object sender, EventArgs e)
        {
            Surface newSurface = new Surface();
            GatherSurfaceFromUI(newSurface);
            string newName = newSurface.Name + " Copy";
            newSurface.Name = newName;
            surfaceBeingEdited = newSurface;
            surfaces.Surfaces.Add(newSurface);
            BuildSurfaceList();

            foreach (Surface surf in SurfaceList.Items)
            {
                if (surf.Name == newName)
                {
                    SurfaceList.SelectedItem = surf;
                    break;
                }
            }

            foreach (CharacterEdit charEd in characterEdits)
            {
                charEd.BuildSurfacePickers(surfaces);
            }

            SurfaceIsDirty = false;
        }

        private void RevertBtn_Click(object sender, EventArgs e)
        {
            EditSurface(surfaceBeingEdited);
        }

        private void SaveDictionaryBtn_Click(object sender, EventArgs e)
        {
            SaveDictionary();
        }

        private void EnvIntensityNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;
        }

        private void AnisotropyXNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;

        }

        private void AnisotropyYNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;

        }

        private void BloomNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;

        }

        private void WrapNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;

        }

        private void BumpTilingNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;

        }

        private void BumpIntensityNum_ValueChanged(object sender, EventArgs e)
        {
            SurfaceIsDirty = true;
        }

        private void RevertDictionaryBtn_Click(object sender, EventArgs e)
        {
            Initialize();
        }

        private List<CharacterEdit> characterEdits = new List<CharacterEdit>();

        private void EditCharacter_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlog = new OpenFileDialog();
            string startDir = System.IO.Directory.GetCurrentDirectory();
            dlog.InitialDirectory = startDir + @"Content\Xml\Actors\";
            dlog.DefaultExt = "xml";
            dlog.CheckFileExists = true;
            dlog.ValidateNames = true;
            dlog.Multiselect = true;

            if (DialogResult.OK == dlog.ShowDialog(this))
            {
                foreach (string fileName in dlog.FileNames)
                {
                    CharacterEdit newEditor = new CharacterEdit(fileName, surfaces);
                    characterEdits.Add(newEditor);
                    newEditor.Show();
                }
            }
        }

        private void NameFld_TextChanged(object sender, EventArgs e)
        {
            if (NameFld.Text != surfaceBeingEdited.Name)
            {
                string newName = NameFld.Text;
                surfaceBeingEdited.Name = newName;
                BuildSurfaceList();

                foreach(Surface surf in SurfaceList.Items)
                {
                    if (surf.Name == newName)
                    {
                        SurfaceList.SelectedItem = surf;
                        break;
                    }
                }

                foreach (CharacterEdit charEd in characterEdits)
                {
                    charEd.BuildSurfacePickers(surfaces);
                }
            }
        }

        public static string BackupFileName(string sourceFileName)
        {
            string backupFile;
            // let's back it up first
            string sourceFileNoExt = System.IO.Path.ChangeExtension(sourceFileName, null);
            int verNum = 1;

            // find a backup file name that isn't in use:
            while (System.IO.File.Exists(backupFile = sourceFileNoExt + "Bak" + verNum + ".xml"))
            {
                verNum++;
            }
            return backupFile;
        }

        private void SaveAsBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlog = new SaveFileDialog();
            string startDir = System.IO.Directory.GetCurrentDirectory();
            dlog.InitialDirectory = startDir + @"Content\Xml\Actors\";
            dlog.DefaultExt = "xml";
            dlog.ValidateNames = true;

            if (DialogResult.OK == dlog.ShowDialog(this))
            {
                string fileName = dlog.FileName;
                bool fileExists = System.IO.File.Exists(fileName);

                if (fileExists)
                {
                    string backUpFile = BackupFileName(fileName);
                    System.IO.File.Copy(fileName, backUpFile);
                    System.IO.File.Delete(fileName);
                }

                surfaces.Save(fileName, BokuShared.FileStorageHelper.Instance);
            }
        }
    }
}

