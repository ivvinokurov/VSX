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
using System.IO;
using VSUILib;
using System.Resources;

namespace VXmlExplorer
{
    public partial class VSFrmXML : Form
    {
        private bool EXPANDING = false;

        private bool OPENED = false;

        private string ROOT = "";

        /////// Context menu objects ///////
        private TreeNode CONTEXT_TREE_NODE = null;
        private ListViewItem CONTEXT_LIST_VIEW_ITEM = null;
        VXmlNode CONTEXT_XML_NODE = null;
        VXmlReference CONTEXT_REF_NODE = null;

        string MENU_NODE_SELECTED_TYPE = "";            // node ("") /attr prefix/ comment prefix
        string MENU_NODE_SELECTED_NAME = "";            // node ("") /attr name/ comment name
        ////////////////////////////////////

        //private VStorage.VStorageCore core;
        private VXmlCatalog cont = null;

        public VSFrmXML()
        {
            InitializeComponent();
            lbResult.Parent = pnQuery;
            stQuery.Parent = pnQuery;
            
            spContainer.Dock = DockStyle.Fill;
            lvDetails.Parent = spContainer.Panel2;
            lvDetails.Dock = DockStyle.Fill;

            tvCat.Parent = spContainer.Panel1;
            tvCat.Dock = DockStyle.Fill;

            spContainer.SplitterDistance = spContainer.Width / 2;

            tvCat.ImageList = new ImageList();
            tvCat.ImageList.Images.Add("selected_item", Properties.Resources.selected_item);
            tvCat.ImageList.Images.Add("catalog", Properties.Resources.catalog);
            tvCat.ImageList.Images.Add("attribute", Properties.Resources.attribute);
            tvCat.ImageList.Images.Add("comment", Properties.Resources.comment);
            tvCat.ImageList.Images.Add("content", Properties.Resources.content);
            tvCat.ImageList.Images.Add("document", Properties.Resources.document);
            tvCat.ImageList.Images.Add("element", Properties.Resources.element);
            tvCat.ImageList.Images.Add("text", Properties.Resources.text);
            tvCat.ImageList.Images.Add("tag", Properties.Resources.tag);

            tvCat.ImageList.ImageSize = new Size(28, 28);

            lvDetails.SmallImageList = new ImageList();
            lvDetails.SmallImageList.Images.Add("selected_item", Properties.Resources.selected_item);
            lvDetails.SmallImageList.Images.Add("catalog", Properties.Resources.catalog);
            lvDetails.SmallImageList.Images.Add("attribute", Properties.Resources.attribute);
            lvDetails.SmallImageList.Images.Add("comment", Properties.Resources.comment);
            lvDetails.SmallImageList.Images.Add("content", Properties.Resources.content);
            lvDetails.SmallImageList.Images.Add("document", Properties.Resources.document);
            lvDetails.SmallImageList.Images.Add("element", Properties.Resources.element);
            lvDetails.SmallImageList.Images.Add("text", Properties.Resources.text);
            lvDetails.SmallImageList.Images.Add("tag", Properties.Resources.tag);

            lvDetails.SmallImageList.ImageSize = new Size(20, 20);

            tvCat.SelectedImageKey = "";
            tvCat.SelectedImageIndex = -1;

            tsSearchType.Text = "XQL";
            //tsSearchType.Text = tsSearchType.Items[0].ToString();
        }

        /// <summary>
        /// Open existing storage
        /// </summary>
        private void ACTION_OpenStorage(string path = "")
        {
            ROOT = "";
            if (path == "")
            {
                ROOT = VSUILib.VSUICommonFunctions.SelectPath(DEFS.KEY_STORAGE_ROOT, "Select МXML storage path");
                    if ((ROOT == VSUILib.VSUICommonFunctions.CANCELLED) | (ROOT == ""))
                    {
                        ROOT = "";
                        return;
                    }
            }
            else
                ROOT = path;

            try
            {
                cont = new VXmlCatalog(ROOT);
            }
            catch (VXmlException e)
            {
                MessageBox.Show(e.Message, "VXML Error", MessageBoxButtons.OK);
                cont.Close();
                return;
            }
            cont.Commit();
            SHOW_Childs();
            SELECT_Node();
            SHOW_Buttons();
            OPENED = true;
        }

        /// <summary>
        /// Close XML storage
        /// </summary>
        private void ACTION_CloseStorage()
        {
            if (OPENED)
            {
                pnQuery.Visible = false;
                tvCat.Nodes.Clear();
                lvDetails.Clear();
                cont.Close();
                OPENED = false;
                ROOT = "";
                tsNode.Text = "";
                SHOW_Buttons();
                CONTEXT_TREE_NODE = null;
                CONTEXT_XML_NODE = null;
            }
        }

        /// <summary>
        /// Delete XML storage
        /// </summary>
        private void ACTION_DeleteStorage()
        {
            string rp = ROOT;
            if (MessageBox.Show("Do you wat to delete storage?", "Delete VXML storage", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            bool delete_content = (cont.Storage.GetSpace(DEFX.XML_CONTENT_SPACE_NAME) != null);

            this.ACTION_CloseStorage();
            VSEngine mgr = new VSEngine(rp);
            try
            {
                mgr.Remove(DEFX.XML_SPACE_NAME);
            }
            catch (VSException er)
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show("Error while deleting VXML space '" + DEFX.XML_SPACE_NAME + "': " + er.Message, "Delete space error", MessageBoxButtons.OK);
                return;
            }
            
            if (delete_content)
            {
                try
                {
                    mgr.Remove(DEFX.XML_CONTENT_SPACE_NAME);
                }
                catch (VSException er)
                {
                    System.Media.SystemSounds.Beep.Play();
                    MessageBox.Show("Error while deleting VXML content space '" + DEFX.XML_CONTENT_SPACE_NAME + "': " + er.Message, "Delete space error", MessageBoxButtons.OK);
                    return;
                }
            }

            MessageBox.Show("VXML storage is deleted at '" + rp + "'", "Delete VXML storage", MessageBoxButtons.OK);
        }

        /// <summary>
        /// Create new XML storage
        /// </summary>
        private void ACTION_CreateStorage()
        {
            VSFrmCreateStorage frm = new VSFrmCreateStorage();
            DialogResult rs = frm.ShowDialog();
            if (rs == DialogResult.OK)
                ACTION_OpenStorage(frm.ROOT);
        }

        /// <summary>
        /// Show/hide buttons and menu items
        /// </summary>
        private void SHOW_Buttons()
        {
            mOpenStorage.Enabled = mCreateStorage.Enabled = !OPENED;
            mDeleteStorage.Enabled = mCloseStorage.Enabled = OPENED;

            mnuCopyToClip.Enabled = mnuDownloadContent.Visible = mnuSaveXML.Visible = mnuSaveXMLWithContent.Visible = false;
            mnuViewXML.Visible = mnuLoadXML.Visible = false;

            mnuCreate.Visible = mCreate.Enabled = false;

            mnuDelete.Visible = mDelete.Enabled = false;

            mnuEdit.Visible = mEdit.Enabled = false;

            mnuCheckIn.Visible = mCheckIn.Enabled = false;

            mnuCheckOut.Visible = mCheckOut.Enabled = false;

            mnuSnap.Visible = mSnap.Enabled = false;

            mnuUndoCheckOut.Visible = mUndoCheckOut.Enabled = false;

            mnuRenameNode.Visible = mnuLookUp.Visible = false;

            mnuSearch.Enabled = mnuSearchTextBox.Enabled = false;

            if (OPENED)
            {
                mnuSearch.Enabled = mnuSearchTextBox.Enabled = true;
                if (CONTEXT_LIST_VIEW_ITEM == null)
                {
                    VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_TREE_NODE.Name));

                    mnuCheckIn.Visible = mCheckIn.Enabled = (CONTEXT_LIST_VIEW_ITEM == null) & DEFX.BR_CHARGEIN_IS_VALID_TYPE(n.NodeTypeCode);

                    mnuCheckOut.Visible = mSnap.Enabled = mnuSnap.Visible = mCheckOut.Enabled = (CONTEXT_LIST_VIEW_ITEM == null) & (DEFX.BR_CHARGEOUT_IS_VALID_TYPE(n.NodeTypeCode) & (!n.IsChargedOut));

                    mnuUndoCheckOut.Visible = mUndoCheckOut.Enabled = (CONTEXT_LIST_VIEW_ITEM == null) & ((n.IsChargedOut) & (n.GUID != ""));

                    mnuCreate.Visible = mCreate.Enabled = (CONTEXT_LIST_VIEW_ITEM == null) & (!n.IsChargedOut) & (DEFX.BR_CHILD_VALID_TYPE_CODES(n.NodeTypeCode).Length > 0);

                    mnuEdit.Visible = mEdit.Enabled = !n.IsChargedOut;

                    mnuDelete.Visible = mDelete.Enabled = (!n.IsChargedOut);

                    mnuCopyToClip.Enabled = true;

                    mnuDownloadContent.Visible = (n.NodeTypeCode == DEFX.NODE_TYPE_CONTENT);

                    mnuSaveXML.Visible = mnuLoadXML.Visible = mnuSaveXMLWithContent.Visible = mnuViewXML.Visible = (CONTEXT_LIST_VIEW_ITEM == null) & DEFX.BR_XML_IS_VALID_TYPE(n.NodeTypeCode);

                    mnuRenameNode.Visible = DEFX.BR_NODE_RENAME(n.NodeTypeCode);

                    mnuLookUp.Visible = (n.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE);
                }
                else
                {
                    mnuDelete.Visible = mDelete.Enabled = mnuEdit.Visible = mEdit.Enabled = true;
                    mnuCopyToClip.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Display next level of the child nodes - recursively
        /// </summary>
        /// <param name="parent"></param>
        private void SHOW_Childs()
        {
            VXmlNode node;
            TreeNodeCollection tc;
            Cursor.Current = Cursors.WaitCursor;

            tvCat.BeginUpdate();

            if (CONTEXT_TREE_NODE == null)
            {
                node = cont;
                CONTEXT_XML_NODE = cont;
                CONTEXT_TREE_NODE = tvCat.Nodes.Add(node.ID, UTIL_PrepareName(node));
                CONTEXT_TREE_NODE.ImageKey = node.NodeType;
                ACTION_SetTreeNodeName(CONTEXT_TREE_NODE, node);
                tc = CONTEXT_TREE_NODE.Nodes;
                tvCat.SelectedNode = CONTEXT_TREE_NODE;
            }
            else
            {
                tc = CONTEXT_TREE_NODE.Nodes;
                node = CONTEXT_XML_NODE;
            }
            tc.Clear();

            if ((node.NodeTypeCode != DEFX.NODE_TYPE_ATTRIBUTE) & (node.NodeTypeCode != DEFX.NODE_TYPE_COMMENT) & (node.NodeTypeCode != DEFX.NODE_TYPE_TEXT))
            {
                VXmlNodeCollection cn = node.AllChildNodes;
                for (int i = 0; i < cn.Count; i++)
                {
                    VXmlNode c = cn[i];
                    TreeNode tn = tc.Add(c.ID, UTIL_PrepareName(c));
                    tn.ImageKey = (c.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE) ? ((VXmlReference)c).ReferenceNode.NodeType : c.NodeType;
                    ACTION_SetTreeNodeName(tn, c);
                }
            }
            tvCat.EndUpdate();
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Set Tree node name
        /// </summary>
        /// <param name="tn"></param>
        /// <param name="c"></param>
        private void ACTION_SetTreeNodeName(TreeNode tn, VXmlNode c)
        {
            string s = "";
            string nodeid = c.ID;
            VXmlNode n = null;
            if (c.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)
            {
                n = ((VXmlReference)c).ReferenceNode;
                nodeid += "~" + n.ID;
            }
            else
                n = c;

            tn.Text = UTIL_PrepareName(c);


            s =
                "Id='" + nodeid + "'\r" + "\n" +
                "Type='" + n.NodeType + "'\r" + "\n" +
                "Name='" + n.Name + "'\r" + "\n" +
                "Value='" + n.Value + "'";
            
            if (c.GUID != "")
                s += "\r" + "\n" + "GUID=" + c.GUID;

            if (c.NodeTypeCode == DEFX.NODE_TYPE_CONTENT)
            {
                VXmlContent ct = (VXmlContent)cont.GetNode(c.Id);
                s += "\r" + "\n" +
                "File name='" + ct.filename + "'" + "\r" + "\n" +
                "Size='" + ct.Length.ToString("#,0;(#,0)") + " bytes";
            }

            tn.ToolTipText = s;
        }


        /// <summary>
        /// Set Tree node name
        /// </summary>
        /// <param name="tn"></param>
        /// <param name="c"></param>
        private void ACTION_SetListItemName(ListViewItem ln, VXmlNode c)
        {
            ln.SubItems[2].Text = UTIL_PrepareName(c);
            for (int i = 0; i < CONTEXT_TREE_NODE.Nodes.Count; i++)
            {
                if (CONTEXT_TREE_NODE.Nodes[i].Name == c.ID)
                {
                    ACTION_SetTreeNodeName(CONTEXT_TREE_NODE.Nodes[i], c);
                    break;
                }
            }

        }


        private string UTIL_PrepareName(VXmlNode c)
        {
            if (c.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)
                return (c.IsChargedOut ? ">> " : "") + "~" + ((VXmlReference)c).ReferenceNode.Name;
            else
                return (c.IsChargedOut ? ">> " : "") + c.Name;
        }
        /// <summary>
        /// Create new child node
        /// </summary>
        private void ACTION_CreateNode()
        {
            cont.Begin();
            
            VSFrmCreateNode form = new VSFrmCreateNode();
            form.Init(cont, CONTEXT_TREE_NODE);

            form.ShowDialog(this);
            cont.Commit();

            if (form.RC == 0)
            {
                SELECT_Node();
                SHOW_Childs();
            }
        }

        /// <summary>
        /// Edit node value
        /// </summary>
        private void ACTION_EditNode()
        {
            string v = "";

            if (CONTEXT_LIST_VIEW_ITEM != null)
                v = CONTEXT_LIST_VIEW_ITEM.SubItems[2].Text;
            else
                v = CONTEXT_XML_NODE.Value;

            string res = VSUILib.VSUICommonFunctions.InputBox("Edit node value", "Value", value: v, numeric: false);

            if (res == VSUILib.VSUICommonFunctions.CANCELLED)
                return;

            cont.Begin();

            if (CONTEXT_LIST_VIEW_ITEM == null)
            {
                CONTEXT_XML_NODE.Value = res;
                ACTION_SetTreeNodeName(CONTEXT_TREE_NODE, CONTEXT_XML_NODE);
            }
            else
            {
                if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_ATTRIBUTE))
                    CONTEXT_XML_NODE.SetAttribute(MENU_NODE_SELECTED_NAME, res);
                else if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_COMMENT))
                {
                    VXmlComment cnode = CONTEXT_XML_NODE.GetCommentNode(MENU_NODE_SELECTED_NAME);
                    cnode.Value = res;
                }
                else if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_TEXT))
                {
                    VXmlText t = CONTEXT_XML_NODE.GetTextNode(MENU_NODE_SELECTED_NAME);
                    t.Value = res;
                }
                else if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_TAG))
                {
                    VXmlTag t = CONTEXT_XML_NODE.GetTagNode(MENU_NODE_SELECTED_NAME);
                    string tv = t.Value;
                    if (tv != res)
                    {
                        CONTEXT_XML_NODE.RemoveTag(tv);
                        CONTEXT_XML_NODE.SetTag(res);
                    }
                }
                SELECT_Node(true, CONTEXT_TREE_NODE);
            }
            cont.Commit();
        }

        /// <summary>
        /// Rename node
        /// </summary>
        private void ACTION_RenameNode()
        {
            VXmlNode n = (CONTEXT_LIST_VIEW_ITEM == null) ? CONTEXT_XML_NODE : cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text));

            string res = VSUILib.VSUICommonFunctions.InputBox("Rename node", "New name", value: n.Name, numeric: false);

            if (res == VSUILib.VSUICommonFunctions.CANCELLED)
                return;
            cont.Begin();
            try
            {
                n.Name = res;
            }
            catch (VXmlException e1)
            {
                MessageBox.Show(e1.Message, "Rename node error");
                cont.RollBack();
                return;
            }
            cont.Commit();

            if (CONTEXT_LIST_VIEW_ITEM == null)
                ACTION_SetTreeNodeName(CONTEXT_TREE_NODE, n);
            else
                ACTION_SetListItemName(CONTEXT_LIST_VIEW_ITEM, n);
        }

        /// <summary>
        /// Delete Node and all subtree
        /// </summary>
        private void ACTION_DeleteNode()
        {
            string nm = (CONTEXT_LIST_VIEW_ITEM == null) ? CONTEXT_TREE_NODE.Text : (CONTEXT_LIST_VIEW_ITEM.SubItems[1].Text + " {" + CONTEXT_LIST_VIEW_ITEM.SubItems[2].Text +"}");
            if (MessageBox.Show("Do you want to proceed and delete node '" + nm + "'?", "Delete node", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                if (CONTEXT_LIST_VIEW_ITEM == null)
                {
                    VXmlNode NodeToDelete = cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_TREE_NODE.Name));
                    VXmlNode pnode = NodeToDelete.ParentNode;
                    if (CONTEXT_REF_NODE == null)
                    {
                        if (pnode == null)
                        {
                            if (MessageBox.Show("You are about to delete root node." + "\r\n" +
                            "Storage will be closed in this case after deletion." + "\r\n" +
                            "Do you want to proceed?", "Delete node", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                                return;
                        }
                    }

                    tsNode.Text = "Deleting node ...";
                    Application.DoEvents();
                    Cursor.Current = Cursors.WaitCursor;
                    cont.Begin();
                    try
                    {
                        if (CONTEXT_LIST_VIEW_ITEM != null)
                        {
                            string rid = NodeToDelete.ID;
                            NodeToDelete.Remove();
                            CONTEXT_LIST_VIEW_ITEM.Remove();
                            for (int i = 0; i < CONTEXT_TREE_NODE.Nodes.Count; i++)
                            {
                                if (CONTEXT_TREE_NODE.Nodes[i].Name == rid)
                                {
                                    CONTEXT_TREE_NODE.Nodes[i].Remove();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (CONTEXT_REF_NODE == null)
                            {
                                CONTEXT_XML_NODE.Remove();
                                if (pnode == null)
                                {
                                    Cursor.Current = Cursors.Default;
                                    ACTION_CloseStorage();
                                    return;
                                }
                            }
                            else
                                CONTEXT_REF_NODE.Remove();

                            tvCat.SelectedNode = CONTEXT_TREE_NODE.Parent;
                            CONTEXT_TREE_NODE.Remove();
                        }
                    }
                    catch (VXmlException e1)
                    {
                        MessageBox.Show(e1.Message, "Error", MessageBoxButtons.OK);
                    }
                    cont.Commit();
                    tsNode.Text = "";
                    SELECT_Node();
                    Cursor.Current = Cursors.Default;
                }
                else
                {
                    if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_ATTRIBUTE))
                        CONTEXT_XML_NODE.RemoveAttribute(MENU_NODE_SELECTED_NAME);
                    else if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_COMMENT))
                        CONTEXT_XML_NODE.RemoveComment(MENU_NODE_SELECTED_NAME);
                    else if (MENU_NODE_SELECTED_TYPE == DEFX.GET_NODETYPE(DEFX.NODE_TYPE_TEXT))
                        CONTEXT_XML_NODE.RemoveText(MENU_NODE_SELECTED_NAME);
                    //else // Tag
                    //    CONTEXT_XML_NODE.RemoveTag.RemoveTagByName(MENU_NODE_SELECTED_NAME);

                    SELECT_Node(true, CONTEXT_TREE_NODE);
                }
            }
        }

        /// <summary>
        /// Checkout Node
        /// lck: true for checkout, false for snap
        /// </summary>
        private void ACTION_CheckOutNode(bool lck = true)
        {
            if (CONTEXT_XML_NODE.IsChargedOut)
            {
                MessageBox.Show("Node is already checked out", "Error", MessageBoxButtons.OK);
                return;
            }

            // Prompt directory
            string st = VSUILib.VSUICommonFunctions.SelectPath(DEFX.KEY_SNAP, "Select target directory");
            if (st != "")
            {
                bool state = true;

                if (lck)
                    tsNode.Text = "Charging out " + CONTEXT_XML_NODE.Name + " ...";
                else
                    tsNode.Text = "Exporting " + CONTEXT_XML_NODE.Name + " ...";

                Application.DoEvents();

                cont.Begin();
                try
                {
                    if (lck)
                        CONTEXT_XML_NODE.ChargeOut(st);
                    else
                        CONTEXT_XML_NODE.Export(st);
                }
                catch (VXmlException e1)
                {
                    MessageBox.Show(e1.Message, "Error", MessageBoxButtons.OK);
                    state = false;
                }
                cont.Commit();
                if (state)
                {
                    SELECT_Node();
                    string op = lck ? "Check Out" : "Snap";
                    MessageBox.Show(op + " completed, GUID=" + CONTEXT_XML_NODE.GUID, "Successful", MessageBoxButtons.OK);
                }
            }
        }

        /// <summary>
        /// Check In Node
        /// </summary>
        private void ACTION_CheckInNode()
        {
            VXmlNode new_node = null;
            string fname = VSUICommonFunctions.SelectFile(DEFX.KEY_SNAP, "Select snap file for check-in", "Snap files (*." + DEFX.XML_EXPORT_FILE_TYPE + ")|*." + DEFX.XML_EXPORT_FILE_TYPE);

            if (fname != "")
            {
                VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(tvCat.SelectedNode.Name));
                bool state = true;
                tsNode.Text = "Checking in " + fname + " ...";

                Application.DoEvents();

                cont.Begin();
                try
                {
                    new_node = n.ChargeIn(fname);
                }
                catch (VXmlException e1)
                {
                    state = false;
                    MessageBox.Show(e1.Message, "Error", MessageBoxButtons.OK);
                }
                cont.Commit();
                if (state)
                   SHOW_NodeInTree(new_node);
                }
            }

        /// <summary>
        /// Show VXml node in the TV-tree
        /// </summary>
        /// <param name="n"></param>
        private void SHOW_NodeInTree(VXmlNode n)
        {
            // Build path
            List<long> l = new List<long>();
            l.Add(n.Id);
            VXmlNode node = n.ParentNode;
            while (node != null)
            {
                l.Add(node.Id);
                node = node.ParentNode;
            }
            TreeNode t = tvCat.Nodes[0];          //Root (document container)

            SELECT_Node(show_list: false, t: t);
            //Rebuild tree
            for (int i = (l.Count - 1); i >= 0; i--)
            {
                if (i > 0)
                {
                    t = t.FirstNode;
                    string val = l[i - 1].ToString();
                    while (t.Name != val)
                    {
                        t = t.NextNode;
                    }
                    SELECT_Node(show_list: false, t: t);
                }
            }
            tvCat.Select();
            SELECT_Node(show_list: true, t: t);
            tvCat.SelectedNode = t;
            t.Expand();
        }

        /// <summary>
        /// Undo Checkout Node
        /// </summary>
        private void ACTION_UndoCheckOutNode()
        {
            bool state = true;
            if (MessageBox.Show("Do you want to Undo CheckOut node?", "Undo CheckOut node", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                cont.Begin();
                try
                {
                    CONTEXT_XML_NODE.UndoChargeOut();
                }
                catch (VXmlException e1)
                {
                    state = false;
                    MessageBox.Show(e1.Message, "Error", MessageBoxButtons.OK);
                }
                cont.Commit();

                if (state)
                    SELECT_Node();
            }
        }

        /// <summary>
        /// Search (XQL)
        /// </summary>
        private void ACTION_SearchNode()
        {
            VXmlNode n = null;
            string st = mnuSearchTextBox.Text.Trim();
            if (st.Length > 0)
            {
                if (tsSearchType.Text == "ID")
                {
                    int nv = 0;
                    bool IsNumeric = int.TryParse(st, out nv);
                    if (IsNumeric & (nv > 0))
                    {
                        n = cont.GetNode((long)nv);

                        if (n != null)
                            SHOW_NodeInTree(n);
                        else
                            MessageBox.Show("Node is not found, ID=" + nv.ToString(), "Lookup node", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                    else
                    {
                        MessageBox.Show("Invalid node ID in request", "Lookup node", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    decimal d = 0;

                    if (CONTEXT_LIST_VIEW_ITEM == null)
                        n = CONTEXT_XML_NODE;
                    else
                        n = cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text));

                    if (n.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)
                        n = ((VXmlReference)n).ReferenceNode;

                    //n = CONTEXT_XML_NODE; // cont.GetNode(VSLib.ConvertStringToLong(tvCat.SelectedNode.Name));

                    VXmlNodeCollection l = null;
                    try
                    {
                        lbResult.Items.Clear();
                        pnQuery.Visible = true;
                        tsNodeCount.Text = "Running query...";
                        Application.DoEvents();
                        Cursor.Current = Cursors.WaitCursor;
                        long l_start = DateTime.Now.Ticks;
                        l = n.SelectNodes(st);
                        long l_end = DateTime.Now.Ticks;
                        d = l_end - l_start;
                    }
                    catch (VXmlException e1)
                    {
                        MessageBox.Show(e1.Message, "Error", MessageBoxButtons.OK);
                    }

                    if (l != null)
                    {
                        tsNodeCount.Text = "Selected " + l.Count.ToString() + " nodes in " + (d / 10000000).ToString("F") + " seconds";
                        Application.DoEvents();
                        Cursor.Current = Cursors.WaitCursor;
                        lbResult.BeginUpdate();
                        for (int i = 0; i < l.Count; i++)
                        {
                            lbResult.Items.Add("<Id:" + l[i].Id.ToString("D8") + "><Name:" + l[i].Name + "><Type:" + l[i].NodeType + ">");
                        }
                        lbResult.EndUpdate();
                    }
                    else
                        tsNodeCount.Text = "";
                    Cursor.Current = Cursors.Default;
                    pnQuery.Visible = true;
                    mnuHide.Visible = true;
                    SHOW_Buttons();
                }
            }
        }

        /// <summary>
        /// Copy node name to clipboard
        /// </summary>
        private void ACTION_CopyToClipboard()
        {
            VXmlNode n = null;
            string s = "";
            if (CONTEXT_LIST_VIEW_ITEM != null)
            {
                s = lvDetails.SelectedItems[0].SubItems[0].Text;
                n = cont.GetNode(VSLib.ConvertStringToLong(s));
            }
            else
                n = CONTEXT_XML_NODE;
            Clipboard.SetText(n.Name);
        }

        private void SELECT_Node(bool show_list = true, TreeNode t = null)
        {
            TreeNode tn = (t == null) ? tvCat.SelectedNode : t;
            if (tn != null)
            {
                CONTEXT_TREE_NODE = tn;

                CONTEXT_XML_NODE = cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_TREE_NODE.Name));
                if (CONTEXT_XML_NODE.NodeTypeCode == DEFX.NODE_TYPE_REFERENCE)
                {
                    CONTEXT_REF_NODE = (VXmlReference)CONTEXT_XML_NODE;
                    CONTEXT_XML_NODE = cont.GetNode(CONTEXT_REF_NODE.ReferenceId);
                }
                else
                    CONTEXT_REF_NODE = null;

                if (CONTEXT_TREE_NODE.FirstNode == null)
                    SHOW_Childs();

                if (show_list)
                    SHOW_NodeList();
                MENU_NODE_SELECTED_TYPE = "";
                MENU_NODE_SELECTED_NAME = "";
            }
            else
            {
                tsNode.Text = "";
            }
            build_path();
        }

        /// <summary>
        /// Show child nodes in the list view
        /// </summary>
        private void SHOW_NodeList()
        {
            CONTEXT_LIST_VIEW_ITEM = null;
            tsNode.Text = build_path(); // CONTEXT_XML_NODE.Name;

            lvDetails.Clear();
            lvDetails.Columns.Clear();
            lvDetails.Columns.Add("Type", 100);
            lvDetails.Columns.Add("Name", 200);
            lvDetails.Columns.Add("Value", 400);

            // Attributes
            VXmlAttributeCollection an = CONTEXT_XML_NODE.Attributes;
            for (int i = 0; i < an.Count; i++)
            {
                VXmlAttribute a = an[i];
                ListViewItem l = new ListViewItem(a.NodeType);

                l.ImageKey = a.NodeType;
                l.SubItems.Add(a.Name);
                l.SubItems.Add(a.Value);
                l.Tag = a.Name;

                lvDetails.Items.Add(l);
            }

            // Comments
            VXmlCommentCollection cnodes = CONTEXT_XML_NODE.CommentNodes;
            for (int i = 0; i < cnodes.Count; i++)
            {
                VXmlComment c = cnodes[i];
                ListViewItem l = new ListViewItem(c.NodeType);

                l.ImageKey = c.NodeType;
                l.SubItems.Add(c.NodeType);
                l.SubItems.Add(c.Value);

                l.Tag = c.Name;
                lvDetails.Items.Add(l);
            }

            // Text nodes
            VXmlTextCollection tx = CONTEXT_XML_NODE.TextNodes;
            for (int i = 0; i < tx.Count; i++)
            {
                VXmlText t = tx[i];
                ListViewItem l = new ListViewItem(t.NodeType);

                l.ImageKey = t.NodeType;
                l.SubItems.Add(t.NodeType);
                l.SubItems.Add(t.Value);
                l.Tag = t.Name;
                lvDetails.Items.Add(l);
            }

            // Tag nodes
            VXmlTagCollection tt = CONTEXT_XML_NODE.TagNodes;
            for (int i = 0; i < tt.Count; i++)
            {
                VXmlTag t = tt[i];
                ListViewItem l = new ListViewItem(t.NodeType);

                l.ImageKey = t.NodeType;
                l.SubItems.Add(t.NodeType);
                l.SubItems.Add(t.Value);
                l.Tag = t.Name;
                lvDetails.Items.Add(l);
            }
        }


        /// <summary>
        /// Download content (content node type only)
        /// </summary>
        private void ACTION_DownloadContent()
        {
            SaveFileDialog sd = new SaveFileDialog();

            sd.Filter = "All files (*.*)|*.*";
            sd.FilterIndex = 1;
            sd.RestoreDirectory = true;
            VXmlContent c = (VXmlContent)((CONTEXT_LIST_VIEW_ITEM == null) ? CONTEXT_XML_NODE : cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text)));

            string s = c.filename;
            sd.FileName = (s == "") ? "NewFileName" : s;

            if (sd.ShowDialog() == DialogResult.OK)
                c.Download(sd.FileName);
        }

        /// <summary>
        /// Save XML
        /// </summary>
        private void ACTION_SaveXML(bool content = true)
        {
            SaveFileDialog sd = new SaveFileDialog();

            sd.Filter = "All files (*.*)|*.*|XML files (*.*)|*.xml";
            sd.FilterIndex = 2;
            sd.RestoreDirectory = true;
            string s = CONTEXT_XML_NODE.Name;
            sd.FileName = s + ".xml";
            if (sd.ShowDialog() == DialogResult.OK)
            {
                tsNode.Text = "Saving XML for " + s + " ...";
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;

                CONTEXT_XML_NODE.SaveXml(sd.FileName, content);
                Cursor.Current = Cursors.Default;
                tsNode.Text = "";
            }
            build_path();
        }

        /// <summary>
        /// View XML
        /// </summary>
        private void ACTION_ViewXML()
        {
            VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(tvCat.SelectedNode.Name));
            VSUICommonFunctions.DisplayText(n.Name, n.Xml);
        }

        /// <summary>
        /// Load XML
        /// </summary>
        private void ACTION_LoadXML()
        {
            string fname = VSUICommonFunctions.SelectFile(DEFX.KEY_LOAD_XML, "Select XML file for load", "XML files (*.xml" + ")|*.xml");

            if (fname != "")
            {
                VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(tvCat.SelectedNode.Name));
                tsNode.Text = "Loading " + fname + " ...";
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;
                cont.Begin();
                string rc = "";
                try
                {
                    if (n.NodeTypeCode == DEFX.NODE_TYPE_DOCUMENT)
                    {
                        VXmlDocument d = (VXmlDocument )cont.GetNode(n.Id);
                        rc = d.Load(fname);
                    }
                    else
                    {
                        rc = n.Load(fname);
                    }
                }
                catch (VXmlException e1)
                {
                    rc = e1.Message;
                }

                if (rc == "")
                {
                    cont.Commit();
                    SHOW_NodeInTree(n);
                }
                else
                {
                    cont.RollBack();
                    MessageBox.Show(rc, "Load error", MessageBoxButtons.OK);
                }
            }
            Cursor.Current = Cursors.Default;
            SELECT_Node();
        }

        /// <summary>
        /// LookUp referencing node in the tree
        /// </summary>
        private void ACTION_LookUpNode()
        {
            if (CONTEXT_LIST_VIEW_ITEM == null)
                SHOW_NodeInTree(CONTEXT_XML_NODE);
            else
            {
                VXmlReference r = (VXmlReference)cont.GetNode(VSLib.ConvertStringToLong(CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text));
                SHOW_NodeInTree(r.ReferenceNode);
            }
        }


        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void VSFrmXML_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (cont != null)
                cont.Close();
        }

        private void tvCat_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            /*
             * Set EXPANDING = true to prevent node select in 'MouseUp' event
             * Set EXPANDING = false in MouseUp event (do not select node if it was true
             */
            //if (!EXPANDING)
            //{
            //    tvCat.SelectedNode = e.Node;
            //    TREE_NODE_SELECTED = true;
                //EXPANDING = true;
                //EXPANDING = false;
            //}
            
            EXPANDING = true;
            //Console.WriteLine("BeforeExpand: EXPANDING=" + EXPANDING.ToString() + " node=" + e.Node.Text);
        }



        private void MenuContext_Opening(object sender, CancelEventArgs e)
        {

            if (tvCat.SelectedNode == null)
                e.Cancel = true;
            else
                this.SHOW_Buttons();
        }


        private void tvCat_MouseUp(object sender, MouseEventArgs e)
        {
            //Console.WriteLine("MouseUp: EXPANDING=" + EXPANDING.ToString());

            if (!EXPANDING)
            {

                TreeNode t = null;
                t = tvCat.GetNodeAt(e.X, e.Y);

                if ((tvCat.SelectedNode != null) & (t != null))
                {
                    if (tvCat.SelectedNode.Name != t.Name)
                    {
                        tvCat.SelectedNode = t;
                        SELECT_Node();
                    }
                }
                else
                {
                    tvCat.SelectedNode = t;
                    SELECT_Node();
                }

                if (e.Button == MouseButtons.Right)
                {
                    SHOW_Buttons();
                    MenuContext.Show(tvCat, e.Location);
                }
            }
            EXPANDING = false;

        }

        private void mnuCreate_Click(object sender, EventArgs e)
        {
            ACTION_CreateNode();
        }

        private void mnuEdit_Click(object sender, EventArgs e)
        {
            ACTION_EditNode();
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            ACTION_DeleteNode();
        }

        private void mnuCheckOut_Click(object sender, EventArgs e)
        {
            ACTION_CheckOutNode(true);
        }

        private void mnuUndoCheckOut_Click(object sender, EventArgs e)
        {
            ACTION_UndoCheckOutNode();
        }


        private void mnuCheckIn_Click(object sender, EventArgs e)
        {
            ACTION_CheckInNode();
        }

        private void mnuSearchTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                ACTION_SearchNode();
            }
        }

        private void mnuSearch_Click(object sender, EventArgs e)
        {
            ACTION_SearchNode();
        }

        private void VSFrmXML_Resize(object sender, EventArgs e)
        {
            //pnTreeView.Height =this.Height- pnQuery.Bottom - 10;
        }

        private void mnuHide_Click(object sender, EventArgs e)
        {
            pnQuery.Visible = false;
            mnuHide.Visible = false;
        }
        private void lbResult_DoubleClick(object sender, EventArgs e)
        {
            if (lbResult.SelectedItem != null)
            {
                string s = (string)lbResult.SelectedItem;
                VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(s.Substring(4,8)));
                SHOW_NodeInTree(n);
            }
        }

        private void mCreate_Click(object sender, EventArgs e)
        {
            this.ACTION_CreateNode();
        }

        private void mEdit_Click(object sender, EventArgs e)
        {
            this.ACTION_EditNode();
        }

        private void mDelete_Click(object sender, EventArgs e)
        {
            this.ACTION_DeleteNode();
        }

        private void mCheckIn_Click(object sender, EventArgs e)
        {
            this.ACTION_CheckInNode();
        }

        private void mCheckOut_Click(object sender, EventArgs e)
        {
            this.ACTION_CheckOutNode(true);
        }

        private void mUndoCheckOut_Click(object sender, EventArgs e)
        {
            this.ACTION_UndoCheckOutNode();
        }

        private void VSFrmXML_Load(object sender, EventArgs e)
        {
            string ks = VSLib.VSGetKey(DEFS.KEY_STORAGE_ROOT);

            if (ks != "")
            {
                if (MessageBox.Show(ks, "Do you want to open the last used storage?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    ACTION_OpenStorage(ks);
            }

            this.SHOW_Buttons();
        }

        private void mOpenStorage_Click(object sender, EventArgs e)
        {
            this.ACTION_OpenStorage();
        }

        private void mCloseStorage_Click(object sender, EventArgs e)
        {
            this.ACTION_CloseStorage();
        }

        private void MenuMain_Click(object sender, EventArgs e)
        {
            this.SHOW_Buttons();
        }

        private void mCreateStorage_Click(object sender, EventArgs e)
        {
            ACTION_CreateStorage();
        }

        private void mDeleteStorage_Click(object sender, EventArgs e)
        {
            ACTION_DeleteStorage();
        }

        private void mnuCopyToClip_Click(object sender, EventArgs e)
        {
            ACTION_CopyToClipboard();
        }

        private void tvCat_KeyUp(object sender, KeyEventArgs e)
        {
            SELECT_Node();
        }

        private void lbResult_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                if (lbResult.SelectedItem != null)
                {
                    string s = (string)lbResult.SelectedItem;
                    VXmlNode n = cont.GetNode(VSLib.ConvertStringToLong(s.Substring(4, 8)));
                    SHOW_NodeInTree(n);
                }

            }
        }

        private void VSFrmXML_Activated(object sender, EventArgs e)
        {
        }

        private void mnuSnap_Click(object sender, EventArgs e)
        {
            ACTION_CheckOutNode(false);
        }

        private void mSnap_Click(object sender, EventArgs e)
        {
            ACTION_CheckOutNode(false);
        }

        private void mnuDownloadContent_Click(object sender, EventArgs e)
        {
            ACTION_DownloadContent();
        }

        private void mnuSaveXML_Click(object sender, EventArgs e)
        {
            ACTION_SaveXML(false);
        }

        private void mnuSaveXMLWithContent_Click(object sender, EventArgs e)
        {
            ACTION_SaveXML(true);
        }

        private void mnuViewXM_Click(object sender, EventArgs e)
        {
            ACTION_ViewXML();
        }

        private void mnuLoadXML_Click(object sender, EventArgs e)
        {
            ACTION_LoadXML();
        }

        private void mnuRenameNode_Click(object sender, EventArgs e)
        {
            ACTION_RenameNode();
        }

        private void lvDetails_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (lvDetails.SelectedItems.Count > 0)
                {
                    CONTEXT_LIST_VIEW_ITEM = lvDetails.SelectedItems[0];
                    string s = CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text;
                    MENU_NODE_SELECTED_TYPE = CONTEXT_LIST_VIEW_ITEM.SubItems[0].Text;

                    MENU_NODE_SELECTED_NAME = (MENU_NODE_SELECTED_TYPE == DEFX.PREFIX_ATTRIBUTE) ? CONTEXT_LIST_VIEW_ITEM.SubItems[1].Text : CONTEXT_LIST_VIEW_ITEM.Tag.ToString();

                    this.SHOW_Buttons();
                    MenuContext.Show(tvCat, e.Location);
                }
            }
        }

        private void lvDetails_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
                SELECT_ListItem();
        }

        private void SELECT_ListItem()
        {
            if (lvDetails.SelectedItems.Count > 0)
                CONTEXT_LIST_VIEW_ITEM = lvDetails.SelectedItems[0];
            else
                CONTEXT_LIST_VIEW_ITEM = null;
        }

        // Build full path
        private string build_path()
        {
            if (CONTEXT_XML_NODE == null)
                return "";

            string path = "/" + CONTEXT_XML_NODE.Name;

            VXmlNode n = CONTEXT_XML_NODE.ParentNode;

            while (n != null)
            {
                path = "/" + n.Name + path;
                n = n.ParentNode;
            }
            return path;
        }

        private void lvDetails_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SELECT_ListItem();
        }

        private void tvCat_AfterExpand(object sender, TreeViewEventArgs e)
        {
            //Console.WriteLine("AfterExpand:: EXPANDING=" + EXPANDING.ToString() + " node=" + e.Node.Text);
        }

        private void mnuLookUp_Click(object sender, EventArgs e)
        {
            ACTION_LookUpNode();
        }

        private void MenuMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void stQuery_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void tvCat_Click(object sender, EventArgs e)
        {

        }

        private void mnuSearchTextBox_MouseHover(object sender, EventArgs e)
        {
        }
    }
}
