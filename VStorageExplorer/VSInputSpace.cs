using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using VSUILib;

namespace VStorageExplorer
{
    public partial class VSInputSpace : Form
    {
        public string S_NAME;
        public int S_SIZE;
        public int S_PAGE_SIZE;
        public int S_EXTENSION;
        public string S_DIR;
        public bool RESULT = false;

        private VSUIPanel pn_control = null;

        public VSInputSpace()
        {
            InitializeComponent();
            pn_control = new VSUIPanel(pnMain, "1");
            pn_control.Display(Properties.Resources.CreateSpace);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            pn_control.Read();
            S_NAME = pn_control.GetValue("name").Trim();
            try
            {
                S_SIZE = Convert.ToInt32(pn_control.GetValue("size").Trim());
                S_PAGE_SIZE = Convert.ToInt32(pn_control.GetValue("page").Trim());
                S_EXTENSION = Convert.ToInt32(pn_control.GetValue("ext").Trim());
                S_DIR = pn_control.GetValue("dir").Trim();
                if (S_DIR == "--default--")
                    S_DIR = "";
            }
            catch (Exception em)
            {
                MessageBox.Show("Error: " + em.Message, "Incorrect value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((S_NAME == "") | (S_PAGE_SIZE < 1) | (S_SIZE < 1) | (S_EXTENSION < 0))
            {
                MessageBox.Show("Error: some value(s) is not specified", "Incorrect value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            RESULT = true;
            btnOK.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnDir_Click(object sender, EventArgs e)
        {
            if (btnDir.Text == "Select")
            {
                FolderBrowserDialog d = new FolderBrowserDialog();
                btnDir.Text = "Default";

                d.Description = "Select space directory";

                DialogResult result = d.ShowDialog(this);
                if ((result != DialogResult.Cancel) & (d.SelectedPath != ""))
                    pn_control.SetValue("dir", d.SelectedPath);
            }
            else
            {
                btnDir.Text = "Select";
                pn_control.SetValue("dir", "--default--");
            }

        }
    }
}
