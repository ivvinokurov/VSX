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
using VXML;
using VSUILib;

namespace VXmlExplorer
{
    public partial class VSFrmCreateStorage : Form
    {
        public string ROOT = "";
        public string ERROR = "";

        private int s_size = -1;
        private int s_ext = -1;
        private int s_contsize = -1;
        private int s_context = -1;

        private VSUIPanel g = null;

        public VSFrmCreateStorage()
        {
            InitializeComponent();
            ROOT = "";
            g = new VSUIPanel(pnMain,"1");
            g.Display(Properties.Resources.CreateXMLStorage);
        }

        private void btnLocation_Click(object sender, EventArgs e)
        {
            ROOT = VSUILib.VSUICommonFunctions.SelectPath(DEFS.KEY_STORAGE_ROOT, "Select root directory for XML storage");
            if ((ROOT == VSUILib.VSUICommonFunctions.CANCELLED) | (ROOT == ""))
                ROOT = "";
            else
            {
                btnCreate.Enabled = true;
                g.SetValue("dir", ROOT);
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            g.Read();
            try
            {
                string s = g.GetValue("size").Trim();
                if (s != "")
                    s_size = VSLib.ConvertStringToInt(s);

                s = g.GetValue("ext").Trim();
                if (s != "")
                    s_ext = VSLib.ConvertStringToInt(s);

                s = g.GetValue("contsize").Trim();
                if (s != "")
                    s_contsize = VSLib.ConvertStringToInt(s);

                s = g.GetValue("context").Trim();
                if (s != "")
                    s_contsize = VSLib.ConvertStringToInt(s);
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, "Create VXML space - invalid parameter(s)", MessageBoxButtons.OK);
                return;
            }

            if (s_size <= 0)
            {
                MessageBox.Show("Size is not specified", "Create VXML space", MessageBoxButtons.OK);
                return;
            }

            VXmlCatalog c = new VXmlCatalog();
            c.Set(ROOT, "", s_size, s_ext, s_contsize, s_context);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
