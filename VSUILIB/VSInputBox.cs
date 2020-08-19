using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSUILib
{
    public partial class VSInputBox : Form
    {
        public string VALUE_STRING = "";
        public int VALUE_INT = 0;
        private string type = "";
        private VSUIPanel pn_control = null;

        public VSInputBox()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="caption"></param>
        /// <param name="type">string/int</param>
        public VSInputBox(string title, string caption, string default_value = "", bool numeric = false)
        {
            InitializeComponent();
            this.type = numeric? "int" : "string";

            this.Text = title;

            pn_control = new VSUIPanel(pnMain, "main");
            pn_control.Load(VSUILIB.Properties.Resources.VSInputBox);
            VSUIControl c = new VSUIControl("inpt");
            c.Value = default_value;
            c.Caption = caption;
            pn_control.Set(c);
            pn_control.Display();
        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            pn_control.Read();
            VALUE_STRING = pn_control.GetValue("inpt");
            if (type == "int")
            {
                try
                {
                    VALUE_INT = Convert.ToInt32(VALUE_STRING);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            this.Close();
        }

        private void VSInputBox_Load(object sender, EventArgs e)
        {
            pn_control.SetFocus("inpt");
        }

        private void VSInputBox_Activated(object sender, EventArgs e)
        {
            pn_control.SetFocus("inpt");
        }
    }
}
