using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VStorage;
using System.IO;

namespace VStorageExplorer
{
    public partial class VSFrmExplorer : Form
    {
        private const int STATE_UNDEFINED = 0;
        private const int STATE_SELECTED = 1;
        private const int STATE_OPENED = 2;

        private VSEngine mgr;
        private int mgr_state = STATE_UNDEFINED;

        private string ROOT = "";
        private string DUMP = "";
        
        //////////////////////////////////////////////////////////////
        //////////////////// PRIVATE METHODS /////////////////////////
        //////////////////////////////////////////////////////////////
        /// <summary>
        /// Open storage
        /// </summary>
        /// <param name="mode"></param>
        private void OpenRoot(string path = "")
        {
            
            if (mgr_state == STATE_OPENED)
                mgr.Close();
            mgr_state = STATE_UNDEFINED;
            if (path != "")
                ROOT = path;
            else
                ROOT = VSUILib.VSUICommonFunctions.SelectPath(DEFS.KEY_STORAGE_ROOT, "Select the storage root directory");

            if (ROOT != "")
            {
                mgr_state = STATE_SELECTED;
                mgr = new VSEngine(ROOT);
                OpenStorage();
                if (mgr_state == STATE_OPENED)
                {
                    CloseStorage();
                    statusStrip.Items.Clear();
                    statusStrip.Items.Add("Storage root: " + ROOT);
                    DUMP = ROOT;
                    DisplaySpaceList();
                }
            }
            ShowButtons();
        }

        /// <summary>
        /// Display space list
        /// </summary>
        private void DisplaySpaceList()
        {
            txtInfo.Text = "";
            mgr = new VSEngine(ROOT);
            listSpace.Items.Clear();
            listSpace.Items.Add("<All>");

            string[] ls = mgr.GetSpaceNameList();
            for (int i = 0; i < ls.Length; i++)
                listSpace.Items.Add(ls[i]);

            cbShow.Visible = true;
            cbShow.SelectedIndex = 0;
        }


        /// <summary>
        /// Display info on click
        /// </summary>
        private void DisplayInfo()
        {
            txtInfo.Clear();
            if (listSpace.SelectedIndex >= 0)
            {
                string nm = "*";
                if ((string)listSpace.SelectedItem != "<All>")
                    nm = (string)listSpace.SelectedItem;

                string[] info = mgr.List(nm);
                for (int i = 0; i < info.Length; i++)
                    AddInfo(info[i]);
            }
        }


        /// <summary>
        /// Logger
        /// </summary>
        /// <param name="log"></param>
        /// <param name="msg"></param>
        private void AddInfo(string msg)
        {
            txtInfo.AppendText(msg + "\r" + "\n");
        }

        /// <summary>
        /// Show/hide buttons
        /// </summary>
        private void ShowButtons()
        {
            if (mgr_state != STATE_UNDEFINED)
            {
                TSMcreate.Enabled = true;
                TSMDump.Enabled = true;
                TSMRestore.Enabled = true;
                csRestoreSpace.Visible = TSMRestoreSpace.Enabled = csDumpSpace.Visible = TSMDumpSpace.Enabled = cbShow.Enabled = csExtend.Visible = TSMExtendSpace.Enabled = csDelete.Visible = TSMDeleteSpace.Enabled = csCreateSpacePartition.Visible = TSMCreatePartition.Enabled = (listSpace.SelectedIndex > 0);
            }
        }

        /// <summary>
        /// Remove space
        /// </summary>
        private void Delete()
        {
            string nm = (string)listSpace.SelectedItem;
            if (MessageBox.Show("Do you want to delete space '" + nm + "'?", "Delete Space", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            try
            {
                mgr.Remove(nm);
                AddInfo("Space has been successfully deleted");
                DisplaySpaceList();
            }
            catch (VSException er)
            {
                AddInfo(er.Message);
            }
            ShowButtons();
        }

        /// <summary>
        /// Extend space
        /// </summary>
        private void Extend()
        {
            string ext = VSUILib.VSUICommonFunctions.InputBox("Extend Space", "Extension size (Mb)", numeric: true);

            if ((ext == "") | (ext == VSUILib.VSUICommonFunctions.CANCELLED))
                return;

            try
            {
                mgr.Extend((string)listSpace.SelectedItem, VSLib.ConvertStringToInt(ext));
                AddInfo("Space has been successfully extended");
                DisplaySpaceList();
            }
            catch (VSException er)
            {
                AddInfo(er.Message);
            }
        }

        /// <summary>
        /// Add partition
        /// </summary>
        private void Add()
        {
            string nm = (string)listSpace.SelectedItem;

            string ext = VSUILib.VSUICommonFunctions.InputBox("Add partition to space '" + nm + "'", "Size (Mb)", numeric: true);

            if ((ext == "") | (ext == VSUILib.VSUICommonFunctions.CANCELLED))
                return;

            try
            {
                mgr.AddPartition((string)listSpace.SelectedItem, VSLib.ConvertStringToInt(ext));
                AddInfo("Partition has been successfully added for space '" + nm + "'");
                DisplaySpaceList();
            }
            catch (VSException er)
            {
                AddInfo(er.Message);
            }
        }

        private void Create()
        {

            VSInputSpace si = new VSInputSpace();
            si.ShowDialog(this);
            if (! si.RESULT)
                return;

            try
            {
                mgr.Create(si.S_NAME, si.S_PAGE_SIZE, si.S_SIZE, si.S_EXTENSION, si.S_DIR);
                AddInfo("Space has been successfully created: '" + si.S_NAME + "'");
                DisplaySpaceList();
            }
            catch (VSException er)
            {
                AddInfo(er.Message);
            }
        }

        private void OpenStorage()
        {
            try
            {
                mgr.Open("");
                mgr_state = STATE_OPENED;
                ShowButtons();
            }
            catch (Exception e)
            {
                AddInfo("Error: " + e.Message);
            }
        }

        private void CloseStorage()
        {
            try
            {
                mgr.Close();
                mgr_state = STATE_SELECTED;
                ShowButtons();
            }
            catch (Exception e)
            {
                AddInfo("Error: " + e.Message);
            }
        }

        private void DisplaySpaceInfo()
        {
            string nm = (string)listSpace.SelectedItem;

            if ((cbShow.SelectedIndex == 0) | (nm == "<All>"))
            { // Display Space Info
                OpenStorage();
                this.DisplayInfo();
                CloseStorage();
            }
            else if (nm != "<All>")
            {
                if (cbShow.SelectedIndex == 1)
                { // Display Free Space Info
                    txtInfo.Text = "";
                    OpenStorage();
                    string[] info = mgr.GetFreeSpaceInfo(nm);
                    CloseStorage();
                    for (int i = 0; i < info.Length; i++)
                        AddInfo(info[i]);
                }
                else if (cbShow.SelectedIndex == 2)
                { // Display Pool Allocation
                    OpenStorage();
                    txtInfo.Text = "";
                    VSpace sp = mgr.GetSpace(nm);
                    short[] pools = sp.GetPools();
                    string spf = "#,#;(#,#)";
                    int padf = 15;

                    AddInfo("Pool#     Allocated size");
                    for (int i = 0; i < pools.Length; i++)
                    {
                        long[] a = sp.GetPoolPointers(pools[i]);
                        long a_use = 0;

                        if (a[0] > 0)
                        {
                            VSObject o = sp.GetRootObject(pools[i]);
                            while (o != null)
                            {
                                a_use+=o.Size;
                                o = o.Next;
                            }


                            if (pools[i] > 0)
                                AddInfo(pools[i].ToString().PadLeft(5) + " " + a_use.ToString(spf).PadLeft(padf));
                            else
                                AddInfo(DEFS.POOL_MNEM(pools[i]).PadLeft(5) + " " + a_use.ToString(spf).PadLeft(padf));
                        }
                    }
                    AddInfo("Done");

                    CloseStorage();
                }
            }
        }

        private void DumpS(string name)
        {
            DUMP = VSUILib.VSUICommonFunctions.SelectPath(DEFS.KEY_DUMP_RESTORE, "Select target directory");

            if (DUMP == "")
                return;

            mgr.Dump(DUMP, name);
            
            AddInfo(" ");

        }

        private void RestoreS(string name)
        {
            DUMP = VSUILib.VSUICommonFunctions.SelectPath(DEFS.KEY_DUMP_RESTORE, "Select source directory");

            if (DUMP == "")
                return;

            mgr.Restore(DUMP, name);

            AddInfo("Restore successful");
        }

        //////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////



        public VSFrmExplorer()
        {
            InitializeComponent();
        }

        private void VSFrmAdm_Load(object sender, EventArgs e)
        {
            mgr = null;
            string ks = VSLib.VSGetKey(DEFS.KEY_STORAGE_ROOT);
            if (ks != "")
            {
                if (MessageBox.Show(ks, "Do you want to open the last used storage?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    OpenRoot(ks);
            }

            ShowButtons();
        }

        private void listSpace_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowButtons();
            DisplaySpaceInfo();
        }

        private void txtStorage_DoubleClick(object sender, EventArgs e)
        {
            OpenRoot();
        }

        private void txtStorage_Click(object sender, EventArgs e)
        {
            if (mgr_state != STATE_UNDEFINED)
            {
                if (MessageBox.Show("Do you want to open another storage?", "Change Storage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }
            OpenRoot();
        }


        private void VSFrmAdm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mgr_state == STATE_OPENED)
                mgr.Close();
        }

        private void VSFrmAdmin_Resize(object sender, EventArgs e)
        {
            listSpace.Width = txtInfo.Width;
        }

        private void mnuOpen_Click(object sender, EventArgs e)
        {
            OpenRoot();
        }

        private void cbShow_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DisplaySpaceInfo();
        }

        private void TSMExtendSpace_Click(object sender, EventArgs e)
        {
            this.Extend();
        }

        private void TSMCreateSpace_Click(object sender, EventArgs e)
        {
            this.Create();
        }

        private void TSMCreatePartition_Click(object sender, EventArgs e)
        {
            this.Add();
        }

        private void TSMDeleteSpace_Click(object sender, EventArgs e)
        {
            this.Delete();
        }

        private void TSMDumpStorage_Click(object sender, EventArgs e)
        {
            this.DumpS("*");
        }

        private void TSMDumpSpace_Click(object sender, EventArgs e)
        {
            this.DumpS((string)listSpace.SelectedItem);
        }

        private void TSMRestoreStorage_Click(object sender, EventArgs e)
        {
            this.RestoreS("*");
        }

        private void TSMRestoreSpace_Click(object sender, EventArgs e)
        {
            this.RestoreS((string)listSpace.SelectedItem);
        }

        private void csCreateSpace_Click(object sender, EventArgs e)
        {
            this.Create();
        }

        private void csCreateSpacePartition_Click(object sender, EventArgs e)
        {
            this.Add();
        }

        private void csDelete_Click(object sender, EventArgs e)
        {
            this.Delete();
        }

        private void csExtend_Click(object sender, EventArgs e)
        {
            this.Extend();
        }

        private void csDumpStorage_Click(object sender, EventArgs e)
        {
            this.DumpS("*");
        }

        private void csDumpSpace_Click(object sender, EventArgs e)
        {
            this.DumpS((string)listSpace.SelectedItem);
        }

        private void csRestoreStorage_Click(object sender, EventArgs e)
        {
            this.RestoreS("*");
        }

        private void csRestoreSpace_Click(object sender, EventArgs e)
        {
            this.RestoreS((string)listSpace.SelectedItem);
        }
    }
}
