using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Boku.Fx;

namespace Surfacer
{
    public partial class CharacterEdit : Form
    {
        // the file we're reading from and writing to
        private string fileName;
        private SurfaceDict surfDict;
        // the surface sets from this chracter definition
        private List<SurfaceSetRecord> surfaceSets = new List<SurfaceSetRecord>();

        private XmlDocument xmlDoc;

        private bool dirty;
        private bool Dirty
        {
            get
            {
                return dirty;
            }
            set
            {
                dirty = value;
                SaveBtn.Enabled = dirty;
                RevertBtn.Enabled = dirty;
            }
        }

        public CharacterEdit(string filePath, SurfaceDict surfaces)
        {
            InitializeComponent();

            Initialize(filePath, surfaces);
        }

        public void Initialize(SurfaceDict surfaces)
        {
            Initialize(fileName, surfaces);
        }

        public void Initialize(string filePath, SurfaceDict surfaces)
        {
            fileName = filePath;
            surfDict = surfaces;
            this.Text = System.IO.Path.GetFileNameWithoutExtension(fileName);

            xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            // note on persistence: the surface sets are in subnodes of the root xml document
            // each surface set record will hold pointers to these subnodes
            // when we save, we copy date back to the subnodes and then write the whole doc
            // back to the file.
            foreach (XmlNode node in xmlDoc.ChildNodes)
            {
                // look for the actor - should be one per file
                if (node.Name == "XmlGameActor")
                {
                    foreach (XmlNode actorNode in node.ChildNodes)
                    {
                        // now get the surface sets - could be a couple
                        if (actorNode.Name == "SurfaceSets")
                        {
                            foreach (XmlNode surfSetNode in actorNode.ChildNodes)
                            {
                                if (surfSetNode.Name == "SurfaceSet")
                                {
                                    SurfaceSetRecord surfSetRec = new SurfaceSetRecord(surfSetNode);
                                    surfaceSets.Add(surfSetRec);
                                    SurfSetMenu.Items.Add(surfSetRec);
                                }
                            }
                        }
                    }
                }
            }

            Dirty = false;

            BuildSurfacePickers(surfaces);
            SurfSetMenu.SelectedIndex = 0;
        }

        public void BuildSurfacePickers(SurfaceDict surfaces)
        {
            string slot1Val = (string)slot1Pick.SelectedItem;
            string slot2Val = (string)slot2Pick.SelectedItem;
            string slot3Val = (string)slot3Pick.SelectedItem;
            string slot4Val = (string)slot4Pick.SelectedItem;
            string slot5Val = (string)slot5Pick.SelectedItem;
            string slot6Val = (string)slot6Pick.SelectedItem;
            string slot7Val = (string)slot7Pick.SelectedItem;
            string slot8Val = (string)slot8Pick.SelectedItem;

            slot1Pick.Items.Clear();
            slot2Pick.Items.Clear();
            slot3Pick.Items.Clear();
            slot4Pick.Items.Clear();
            slot5Pick.Items.Clear();
            slot6Pick.Items.Clear();
            slot7Pick.Items.Clear();
            slot8Pick.Items.Clear();

            // build all the slot pickers by adding every surface to every picker
            foreach (Surface s in surfaces.Surfaces)
            {
                slot1Pick.Items.Add(s.Name);
                slot2Pick.Items.Add(s.Name);
                slot3Pick.Items.Add(s.Name);
                slot4Pick.Items.Add(s.Name);
                slot5Pick.Items.Add(s.Name);
                slot6Pick.Items.Add(s.Name);
                slot7Pick.Items.Add(s.Name);
                slot8Pick.Items.Add(s.Name);
            }

            // put the saved values back into the pickers. if the value is no longer there, null will be selected
            slot1Pick.SelectedItem = slot1Val;
            slot2Pick.SelectedItem = slot2Val;
            slot3Pick.SelectedItem = slot3Val;
            slot4Pick.SelectedItem = slot4Val;
            slot5Pick.SelectedItem = slot5Val;
            slot6Pick.SelectedItem = slot6Val;
            slot7Pick.SelectedItem = slot7Val;
            slot8Pick.SelectedItem = slot8Val;
        }

        // a clone of the surfaceset object.
        // there is probably a way to deserialize that directly from an xml node, but the 
        // straightest method requires more cross-project integration than I want to tackle right now.
        // if this tool turns out to be really useful, it would be worth revisiting that decision
        class SurfaceSetRecord
        {
            private XmlNode sourceNode;
            private string name;
            private List<string> surfNames = new List<string>();
            public List<string> SurfaceNames
            {
                get { return surfNames; }
            }

            public string BumpPath {
                get; set;
            }

            public string DirtPath
            {
                get;
                set;
            }

            public override string ToString()
            {
                return name;
            }

            public SurfaceSetRecord(XmlNode surfSetXML)
            {
                sourceNode = surfSetXML;

                // first get the properties of the surface set
                foreach (XmlNode node in sourceNode.ChildNodes)
                {
                    if (node.Name == "Name")
                    {
                        name = node.InnerText;
                    }
                    if (node.Name == "SurfaceNames")
                    {
                        // now we're going to accumulate the names
                        // of all the surfaces applied to the slots in this
                        // model - note that order is paramount - different slots
                        // correspond to different body parts of the bot
                        foreach (XmlNode surfNameNode in node.ChildNodes)
                        {
                            System.Diagnostics.Debug.Assert(surfNameNode.Name == "string",
                                                            "Expected string parsing SurfaceNames in XmlActor");
                            surfNames.Add(surfNameNode.InnerText);
                        }
                    }
                    else if (node.Name == "BumpDetailName")
                    {
                        BumpPath = node.InnerText;
                    }
                    else if (node.Name == "DirtMapName")
                    {
                        DirtPath = node.InnerText;
                    }
                }
            }

            // Push the values back into the source xmlnode, from which they can be written back to the file.
            public XmlNode WriteBack()
            {
                foreach (XmlNode node in sourceNode.ChildNodes)
                {
                    if (node.Name == "SurfaceNames")
                    {
                        System.Diagnostics.Debug.Assert(node.ChildNodes.Count == surfNames.Count, "Surfaces to write is different from surfaces in node.");

                        int i = 0;
                        foreach (XmlNode surfNameNode in node.ChildNodes)
                        {
                            if (i < surfNames.Count)
                                surfNameNode.InnerText = surfNames[i++];
                            else break;
                        }
                    }
                    else if (node.Name == "BumpDetailName")
                    {
                        node.InnerText = BumpPath;
                    }
                    else if (node.Name == "DirtMapName")
                    {
                        node.InnerText = DirtPath;
                    }
                }
                return sourceNode;
            }
        }

        SurfaceSetRecord currSurfSet = null;

        private void SurfSetMenu_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCurSurfSetFromUI();

            currSurfSet = SurfSetMenu.SelectedItem as SurfaceSetRecord;

            ShowSurfaceSet(currSurfSet);
        }

        private void UpdateCurSurfSetFromUI()
        {
            if (currSurfSet != null)
                UpdateSurfaceSetFromUI(currSurfSet);
        }

        // Copy surface set parameters from the UI to the current surface set
        private void UpdateSurfaceSetFromUI(SurfaceSetRecord surfSet)
        {
            // we're not going to do range checking when harvesting edited values
            // because we did it when we were configuring the editor
            int numSlots = surfSet.SurfaceNames.Count;

            if (slot1Pick.Enabled)
                surfSet.SurfaceNames[0] = slot1Pick.SelectedItem as string;

            if (slot2Pick.Enabled)
                surfSet.SurfaceNames[1] = slot2Pick.SelectedItem as string;

            if (slot3Pick.Enabled)
                surfSet.SurfaceNames[2] = slot3Pick.SelectedItem as string;

            if (slot4Pick.Enabled)
                surfSet.SurfaceNames[3] = slot4Pick.SelectedItem as string;

            if (slot5Pick.Enabled)
                surfSet.SurfaceNames[4] = slot5Pick.SelectedItem as string;

            if (slot6Pick.Enabled)
                surfSet.SurfaceNames[5] = slot6Pick.SelectedItem as string;

            if (slot7Pick.Enabled)
                surfSet.SurfaceNames[6] = slot7Pick.SelectedItem as string;

            if (slot8Pick.Enabled)
                surfSet.SurfaceNames[7] = slot8Pick.SelectedItem as string;


            surfSet.BumpPath = BumpFld.Text;
            surfSet.DirtPath = DirtFld.Text;
        }


        // When we load up a surface set, we want to put the right values
        // into the slot pickers.
        // Any slot pickers above the used number will be disabled.
        private void ShowSurfaceSet(SurfaceSetRecord surfSet)
        {
            int numSlots = surfSet.SurfaceNames.Count;

            if (slot1Pick.Enabled = (numSlots > 0))
                slot1Pick.SelectedItem = surfSet.SurfaceNames[0];
            else
                slot1Pick.SelectedItem = null;

            if (slot2Pick.Enabled = (numSlots > 1))
                slot2Pick.SelectedItem = surfSet.SurfaceNames[1];
            else
                slot2Pick.SelectedItem = null;

            if (slot3Pick.Enabled = (numSlots > 2))
                slot3Pick.SelectedItem = surfSet.SurfaceNames[2];
            else
                slot3Pick.SelectedItem = null;

            if (slot4Pick.Enabled = (numSlots > 3))
                slot4Pick.SelectedItem = surfSet.SurfaceNames[3];
            else
                slot4Pick.SelectedItem = null;

            if (slot5Pick.Enabled = (numSlots > 4))
                slot5Pick.SelectedItem = surfSet.SurfaceNames[4];
            else
                slot5Pick.SelectedItem = null;

            if (slot6Pick.Enabled = (numSlots > 5))
                slot6Pick.SelectedItem = surfSet.SurfaceNames[5];
            else
                slot6Pick.SelectedItem = null;

            if (slot7Pick.Enabled = (numSlots > 6))
                slot7Pick.SelectedItem = surfSet.SurfaceNames[6];
            else
                slot7Pick.SelectedItem = null;

            if (slot8Pick.Enabled = (numSlots > 7))
                slot8Pick.SelectedItem = surfSet.SurfaceNames[7];
            else
                slot8Pick.SelectedItem = null;

            BumpFld.Text = surfSet.BumpPath;
            DirtFld.Text = surfSet.DirtPath;
        }

        private void BumpPickBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlog = new OpenFileDialog();
            string startDir = System.IO.Directory.GetCurrentDirectory();
            dlog.InitialDirectory = startDir + @"\Content\Textures\";
            dlog.DefaultExt = "xml";
            dlog.CheckFileExists = true;
            dlog.ValidateNames = true;

            if (DialogResult.OK == dlog.ShowDialog(this))
            {
                string path = dlog.FileName;
                path = System.IO.Path.ChangeExtension(path, null);

                int textFolderStart = path.IndexOf(@"Textures");
                if (textFolderStart > -1)
                {
                    BumpFld.Text = path.Substring(textFolderStart);
                }
                else
                {
                    MessageBox.Show("You must pick a texture in or below the Textures directory.");
                }
            }
        }

        private void DirtPickBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlog = new OpenFileDialog();
            string startDir = System.IO.Directory.GetCurrentDirectory();
            dlog.InitialDirectory = startDir + @"\Content\Textures\";

            dlog.DefaultExt = "xml";
            dlog.CheckFileExists = true;
            dlog.ValidateNames = true;

            if (DialogResult.OK == dlog.ShowDialog(this))
            {
                string path = dlog.FileName;
                path = System.IO.Path.ChangeExtension(path, null);

                int textFolderStart = path.IndexOf(@"Textures");
                if (textFolderStart > -1)
                {
                    BumpFld.Text = path.Substring(textFolderStart);
                }
                else
                {
                    MessageBox.Show("You must pick a texture in or below the Textures directory.");
                }
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            SaveTo(fileName);
        }

        private void SaveTo(string destFile)
        {
            UpdateCurSurfSetFromUI();
            foreach (SurfaceSetRecord surfRec in surfaceSets)
            {
                surfRec.WriteBack();        // write back to the xml node
            }

            if (System.IO.File.Exists(destFile))
            {
                string backupFile = MainWindow.BackupFileName(destFile);

                System.IO.File.Copy(destFile, backupFile);
                System.IO.File.Delete(destFile);
            }

            xmlDoc.Save(destFile);
            Dirty = false;
        }

        private void slot1Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot2Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot3Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot4Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot5Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot6Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot7Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void slot8Pick_SelectedIndexChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void BumpFld_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void DirtFld_TextChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private void RevertBtn_Click(object sender, EventArgs e)
        {
            Initialize(fileName, surfDict);
        }

        private void SaveAsBtn_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlog = new SaveFileDialog();
            string startDir = System.IO.Directory.GetCurrentDirectory();
            dlog.InitialDirectory = startDir + @"\Content\Xml\Actors\";
            dlog.DefaultExt = "xml";
            dlog.ValidateNames = true;

            if (DialogResult.OK == dlog.ShowDialog(this))
            {
                string fileName = dlog.FileName;
                SaveTo(fileName);
            }
        }
    }
}
