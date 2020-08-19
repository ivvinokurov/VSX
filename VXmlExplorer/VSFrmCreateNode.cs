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
    public partial class VSFrmCreateNode : Form
    {
        public VXmlCatalog CONT;
        public VXmlNode PARENT;

        public TreeNode TN;
        public int RC = 0;      // Retcode
        private short[] n_type_code = null;
        private string[] n_type = null;

        VSUIPanel g = null;

        public VSFrmCreateNode()
        {
            InitializeComponent();
        }

        public void Init(VXmlCatalog dc, TreeNode tn)
        {
            g = new VSUIPanel(pnMain, "1");
            g.Load(Properties.Resources.CreateNode);

            CONT = dc;
            TN = tn;
            this.Text = "Create child node for " + tn.Text;
            cbNodeType.Items.Clear();

            PARENT = CONT.GetNode(VSLib.ConvertStringToLong(tn.Name));
            n_type_code = DEFX.BR_CHILD_VALID_TYPE_CODES(PARENT.NodeTypeCode);
            n_type = DEFX.BR_CREATE_VALID_TYPES(PARENT.NodeTypeCode);


            for (int i = 0; i < n_type.Length; i++)
                if (n_type_code[i] != DEFX.NODE_TYPE_REFERENCE)
                    cbNodeType.Items.Add(n_type[i]);

            g.AddControl("type", cbNodeType);
            g.AddControl("select", btnSelectFile);
            g.AddControl("ok", btnCreate);
            g.AddControl("cancel", btnCancel);
            
            g.Display();
        }

        private void cbNodeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            short type = n_type_code[cbNodeType.SelectedIndex];
            VSUIControl[] controls = new VSUIControl[5];
            controls[0] = new VSUIControl("select");
            controls[0].Visible = (type == DEFX.NODE_TYPE_CONTENT);

            controls[1] = new VSUIControl("file");
            controls[1].Visible = (type == DEFX.NODE_TYPE_CONTENT);

            controls[2] = new VSUIControl("title");
            controls[2].Visible = (type == DEFX.NODE_TYPE_CONTENT);

            controls[3] = new VSUIControl("value");
            controls[3].Caption = (type == DEFX.NODE_TYPE_CATALOG) ? "Title:" : "Value:";

            controls[4] = new VSUIControl("name");

            if ((type == DEFX.NODE_TYPE_CONTENT) | (type == DEFX.NODE_TYPE_COMMENT) | (type == DEFX.NODE_TYPE_TEXT) | (type == DEFX.NODE_TYPE_TAG))
                controls[4].Value = DEFX.GET_NODETYPE(type);
            else
                controls[4].Value = "";

            controls[4].Enabled = (controls[4].Value == "");

            g.Set(controls);

            if (!controls[4].Enabled)
                controls[4].Value = "";
            else
                g.SetFocus("name");
            
            btnCreate.Enabled = (cbNodeType.SelectedIndex >= 0);
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            short node_type = 0;

            string V = "";
            string N = "";

            for (int i=0; i< n_type.Length; i++)
                if (n_type[i] == cbNodeType.Text)
                {
                    node_type = n_type_code[i];
                    break;
                }

            RC = 0;
            g.Read();
            N = g.GetValue("name");
            V = g.GetValue("value");

            if ((N == "") & DEFX.BR_NODE_NEED_NAME(node_type))
                MessageBox.Show("Name field is not defined", "Error");
            else if ((node_type == DEFX.NODE_TYPE_CONTENT) &
                ((g.GetValue("file") == "") | (g.GetValue("title") == "")))
                MessageBox.Show("File name or Title not defined", "Error");
            else
            {
                VXmlNode node = CONT.GetNode(VSLib.ConvertStringToLong(TN.Name));
                try
                {
                    if (node_type == DEFX.NODE_TYPE_CONTENT)
                    {
                        VXmlContent c = node.CreateContent(g.GetValue("file"));
                    }
                    else if (node_type == DEFX.NODE_TYPE_CATALOG)
                    {
                        VXmlCatalog cat = (VXmlCatalog)CONT.GetNode(node.Id);
                        VXmlCatalog newcat = cat.CreateCatalog(g.GetValue("name"));
                        if (V != "")
                            newcat.Value = V;
                    }
                    else if (node_type == DEFX.NODE_TYPE_DOCUMENT)
                    {
                        VXmlCatalog cat = (VXmlCatalog)CONT.GetNode(node.Id);
                        VXmlDocument d = cat.CreateDocument(N);
                        if (V != "")
                            d.Value = V;
                    }
                    else if (node_type == DEFX.NODE_TYPE_ATTRIBUTE)
                    {
                        node.SetAttribute(N, V);
                    }
                    else if (node_type == DEFX.NODE_TYPE_COMMENT)
                    {
                        node.CreateComment(V);
                    }
                    else if (node_type == DEFX.NODE_TYPE_TEXT)
                    {
                        node.CreateTextNode(V);
                    }
                    else if (node_type == DEFX.NODE_TYPE_TAG)
                    {
                        node.SetTag(V.Trim());
                    }
                    else if (node_type == DEFX.NODE_TYPE_ELEMENT)
                    {
                        VXmlElement el = node.CreateElement(N, V);
                    }
                    else
                    {
                        MessageBox.Show("Invalid node type '" + DEFX.GET_NODETYPE(node_type) + "'", "Error");
                        RC = 1;
                    }
                }
                catch (VXmlException ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    RC = 1;
                }
                if (RC == 0)
                    this.Close();
            }

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            RC = 1;
            this.Close();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();

            d.Title = "Select content file";

            DialogResult result = d.ShowDialog(this);

            if ((result != DialogResult.Cancel) & (d.FileName != ""))
                g.SetValue("file", d.FileName);
        }

    }
}
