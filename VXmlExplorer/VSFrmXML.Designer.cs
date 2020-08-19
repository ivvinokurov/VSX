namespace VXmlExplorer
{
    partial class VSFrmXML
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tvCat = new System.Windows.Forms.TreeView();
            this.MenuContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuCreate = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuRenameNode = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLookUp = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCheckIn = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCheckOut = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSnap = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuUndoCheckOut = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCopyToClip = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDownloadContent = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuViewXML = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSaveXML = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSaveXMLWithContent = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLoadXML = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mOpenStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.mCloseStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.mCreateStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.mDeleteStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.mCreate = new System.Windows.Forms.ToolStripMenuItem();
            this.mEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.mDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.mCheckIn = new System.Windows.Forms.ToolStripMenuItem();
            this.mSnap = new System.Windows.Forms.ToolStripMenuItem();
            this.mCheckOut = new System.Windows.Forms.ToolStripMenuItem();
            this.mUndoCheckOut = new System.Windows.Forms.ToolStripMenuItem();
            this.tsSearchType = new System.Windows.Forms.ToolStripComboBox();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSearchTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.mnuSearch = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHide = new System.Windows.Forms.ToolStripMenuItem();
            this.lbResult = new System.Windows.Forms.ListBox();
            this.pnQuery = new System.Windows.Forms.Panel();
            this.stQuery = new System.Windows.Forms.StatusStrip();
            this.tsNodeCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.tsNode = new System.Windows.Forms.ToolStripStatusLabel();
            this.lvDetails = new System.Windows.Forms.ListView();
            this.coId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.coType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.coName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.coValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.spContainer = new System.Windows.Forms.SplitContainer();
            this.MenuContext.SuspendLayout();
            this.MenuMain.SuspendLayout();
            this.pnQuery.SuspendLayout();
            this.stQuery.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spContainer)).BeginInit();
            this.spContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvCat
            // 
            this.tvCat.ContextMenuStrip = this.MenuContext;
            this.tvCat.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvCat.FullRowSelect = true;
            this.tvCat.HideSelection = false;
            this.tvCat.Location = new System.Drawing.Point(12, 175);
            this.tvCat.Name = "tvCat";
            this.tvCat.ShowNodeToolTips = true;
            this.tvCat.Size = new System.Drawing.Size(263, 215);
            this.tvCat.TabIndex = 2;
            this.tvCat.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvCat_BeforeExpand);
            this.tvCat.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.tvCat_AfterExpand);
            this.tvCat.Click += new System.EventHandler(this.tvCat_Click);
            this.tvCat.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tvCat_KeyUp);
            this.tvCat.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tvCat_MouseUp);
            // 
            // MenuContext
            // 
            this.MenuContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuCreate,
            this.mnuDelete,
            this.mnuRenameNode,
            this.mnuEdit,
            this.mnuLookUp,
            this.mnuCheckIn,
            this.mnuCheckOut,
            this.mnuSnap,
            this.mnuUndoCheckOut,
            this.mnuCopyToClip,
            this.mnuDownloadContent,
            this.mnuViewXML,
            this.mnuSaveXML,
            this.mnuSaveXMLWithContent,
            this.mnuLoadXML});
            this.MenuContext.Name = "MenuContext";
            this.MenuContext.Size = new System.Drawing.Size(275, 334);
            this.MenuContext.Opening += new System.ComponentModel.CancelEventHandler(this.MenuContext_Opening);
            // 
            // mnuCreate
            // 
            this.mnuCreate.Name = "mnuCreate";
            this.mnuCreate.Size = new System.Drawing.Size(274, 22);
            this.mnuCreate.Text = "Create Node";
            this.mnuCreate.Click += new System.EventHandler(this.mnuCreate_Click);
            // 
            // mnuDelete
            // 
            this.mnuDelete.Name = "mnuDelete";
            this.mnuDelete.Size = new System.Drawing.Size(274, 22);
            this.mnuDelete.Text = "Delete Node";
            this.mnuDelete.Click += new System.EventHandler(this.mnuDelete_Click);
            // 
            // mnuRenameNode
            // 
            this.mnuRenameNode.Name = "mnuRenameNode";
            this.mnuRenameNode.Size = new System.Drawing.Size(274, 22);
            this.mnuRenameNode.Text = "Rename Node";
            this.mnuRenameNode.Click += new System.EventHandler(this.mnuRenameNode_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.Name = "mnuEdit";
            this.mnuEdit.Size = new System.Drawing.Size(274, 22);
            this.mnuEdit.Text = "Edit Value";
            this.mnuEdit.Click += new System.EventHandler(this.mnuEdit_Click);
            // 
            // mnuLookUp
            // 
            this.mnuLookUp.Name = "mnuLookUp";
            this.mnuLookUp.Size = new System.Drawing.Size(274, 22);
            this.mnuLookUp.Text = "Lookup original node";
            this.mnuLookUp.Click += new System.EventHandler(this.mnuLookUp_Click);
            // 
            // mnuCheckIn
            // 
            this.mnuCheckIn.Name = "mnuCheckIn";
            this.mnuCheckIn.Size = new System.Drawing.Size(274, 22);
            this.mnuCheckIn.Text = "Check In";
            this.mnuCheckIn.Click += new System.EventHandler(this.mnuCheckIn_Click);
            // 
            // mnuCheckOut
            // 
            this.mnuCheckOut.Name = "mnuCheckOut";
            this.mnuCheckOut.Size = new System.Drawing.Size(274, 22);
            this.mnuCheckOut.Text = "CheckOut";
            this.mnuCheckOut.Click += new System.EventHandler(this.mnuCheckOut_Click);
            // 
            // mnuSnap
            // 
            this.mnuSnap.Name = "mnuSnap";
            this.mnuSnap.Size = new System.Drawing.Size(274, 22);
            this.mnuSnap.Text = "Snap";
            this.mnuSnap.Click += new System.EventHandler(this.mnuSnap_Click);
            // 
            // mnuUndoCheckOut
            // 
            this.mnuUndoCheckOut.Name = "mnuUndoCheckOut";
            this.mnuUndoCheckOut.Size = new System.Drawing.Size(274, 22);
            this.mnuUndoCheckOut.Text = "Undo CheckOut";
            this.mnuUndoCheckOut.Click += new System.EventHandler(this.mnuUndoCheckOut_Click);
            // 
            // mnuCopyToClip
            // 
            this.mnuCopyToClip.Enabled = false;
            this.mnuCopyToClip.Name = "mnuCopyToClip";
            this.mnuCopyToClip.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.mnuCopyToClip.Size = new System.Drawing.Size(274, 22);
            this.mnuCopyToClip.Text = "Copy node name to clipboard";
            this.mnuCopyToClip.Click += new System.EventHandler(this.mnuCopyToClip_Click);
            // 
            // mnuDownloadContent
            // 
            this.mnuDownloadContent.Name = "mnuDownloadContent";
            this.mnuDownloadContent.Size = new System.Drawing.Size(274, 22);
            this.mnuDownloadContent.Text = "Download Content";
            this.mnuDownloadContent.Click += new System.EventHandler(this.mnuDownloadContent_Click);
            // 
            // mnuViewXML
            // 
            this.mnuViewXML.Name = "mnuViewXML";
            this.mnuViewXML.Size = new System.Drawing.Size(274, 22);
            this.mnuViewXML.Text = "View XML";
            this.mnuViewXML.Click += new System.EventHandler(this.mnuViewXM_Click);
            // 
            // mnuSaveXML
            // 
            this.mnuSaveXML.Name = "mnuSaveXML";
            this.mnuSaveXML.Size = new System.Drawing.Size(274, 22);
            this.mnuSaveXML.Text = "Save XML";
            this.mnuSaveXML.Click += new System.EventHandler(this.mnuSaveXML_Click);
            // 
            // mnuSaveXMLWithContent
            // 
            this.mnuSaveXMLWithContent.Name = "mnuSaveXMLWithContent";
            this.mnuSaveXMLWithContent.Size = new System.Drawing.Size(274, 22);
            this.mnuSaveXMLWithContent.Text = "Save XML with content";
            this.mnuSaveXMLWithContent.Click += new System.EventHandler(this.mnuSaveXMLWithContent_Click);
            // 
            // mnuLoadXML
            // 
            this.mnuLoadXML.Name = "mnuLoadXML";
            this.mnuLoadXML.Size = new System.Drawing.Size(274, 22);
            this.mnuLoadXML.Text = "Load XML";
            this.mnuLoadXML.Click += new System.EventHandler(this.mnuLoadXML_Click);
            // 
            // MenuMain
            // 
            this.MenuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.tsSearchType,
            this.closeToolStripMenuItem,
            this.mnuSearchTextBox,
            this.mnuSearch,
            this.mnuHide});
            this.MenuMain.Location = new System.Drawing.Point(0, 0);
            this.MenuMain.Name = "MenuMain";
            this.MenuMain.Size = new System.Drawing.Size(837, 27);
            this.MenuMain.TabIndex = 0;
            this.MenuMain.Text = "menuStrip1";
            this.MenuMain.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.MenuMain_ItemClicked);
            this.MenuMain.Click += new System.EventHandler(this.MenuMain_Click);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mOpenStorage,
            this.mCloseStorage,
            this.mCreateStorage,
            this.mDeleteStorage,
            this.mCreate,
            this.mEdit,
            this.mDelete,
            this.mCheckIn,
            this.mSnap,
            this.mCheckOut,
            this.mUndoCheckOut});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // mOpenStorage
            // 
            this.mOpenStorage.Name = "mOpenStorage";
            this.mOpenStorage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.mOpenStorage.Size = new System.Drawing.Size(204, 22);
            this.mOpenStorage.Text = "Open";
            this.mOpenStorage.Click += new System.EventHandler(this.mOpenStorage_Click);
            // 
            // mCloseStorage
            // 
            this.mCloseStorage.Name = "mCloseStorage";
            this.mCloseStorage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.mCloseStorage.Size = new System.Drawing.Size(204, 22);
            this.mCloseStorage.Text = "Close";
            this.mCloseStorage.Click += new System.EventHandler(this.mCloseStorage_Click);
            // 
            // mCreateStorage
            // 
            this.mCreateStorage.Name = "mCreateStorage";
            this.mCreateStorage.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.mCreateStorage.Size = new System.Drawing.Size(204, 22);
            this.mCreateStorage.Text = "Create";
            this.mCreateStorage.Click += new System.EventHandler(this.mCreateStorage_Click);
            // 
            // mDeleteStorage
            // 
            this.mDeleteStorage.Name = "mDeleteStorage";
            this.mDeleteStorage.Size = new System.Drawing.Size(204, 22);
            this.mDeleteStorage.Text = "Delete storage";
            this.mDeleteStorage.Click += new System.EventHandler(this.mDeleteStorage_Click);
            // 
            // mCreate
            // 
            this.mCreate.Name = "mCreate";
            this.mCreate.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.mCreate.Size = new System.Drawing.Size(204, 22);
            this.mCreate.Text = "Create node";
            this.mCreate.Click += new System.EventHandler(this.mCreate_Click);
            // 
            // mEdit
            // 
            this.mEdit.Name = "mEdit";
            this.mEdit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.mEdit.Size = new System.Drawing.Size(204, 22);
            this.mEdit.Text = "Edit node value";
            this.mEdit.Click += new System.EventHandler(this.mEdit_Click);
            // 
            // mDelete
            // 
            this.mDelete.Name = "mDelete";
            this.mDelete.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.mDelete.Size = new System.Drawing.Size(204, 22);
            this.mDelete.Text = "Delete node";
            this.mDelete.Click += new System.EventHandler(this.mDelete_Click);
            // 
            // mCheckIn
            // 
            this.mCheckIn.Name = "mCheckIn";
            this.mCheckIn.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.mCheckIn.Size = new System.Drawing.Size(204, 22);
            this.mCheckIn.Text = "Check In";
            this.mCheckIn.Click += new System.EventHandler(this.mCheckIn_Click);
            // 
            // mSnap
            // 
            this.mSnap.Name = "mSnap";
            this.mSnap.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F3)));
            this.mSnap.Size = new System.Drawing.Size(204, 22);
            this.mSnap.Text = "Snap";
            this.mSnap.Click += new System.EventHandler(this.mSnap_Click);
            // 
            // mCheckOut
            // 
            this.mCheckOut.Name = "mCheckOut";
            this.mCheckOut.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.mCheckOut.Size = new System.Drawing.Size(204, 22);
            this.mCheckOut.Text = "Check Out";
            this.mCheckOut.Click += new System.EventHandler(this.mCheckOut_Click);
            // 
            // mUndoCheckOut
            // 
            this.mUndoCheckOut.Name = "mUndoCheckOut";
            this.mUndoCheckOut.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
            this.mUndoCheckOut.Size = new System.Drawing.Size(204, 22);
            this.mUndoCheckOut.Text = "Undo Check Out";
            this.mUndoCheckOut.Click += new System.EventHandler(this.mUndoCheckOut_Click);
            // 
            // tsSearchType
            // 
            this.tsSearchType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tsSearchType.Items.AddRange(new object[] {
            "ID",
            "XQL"});
            this.tsSearchType.Name = "tsSearchType";
            this.tsSearchType.Size = new System.Drawing.Size(75, 23);
            this.tsSearchType.ToolTipText = "Search type";
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(48, 23);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // mnuSearchTextBox
            // 
            this.mnuSearchTextBox.Name = "mnuSearchTextBox";
            this.mnuSearchTextBox.Size = new System.Drawing.Size(400, 23);
            this.mnuSearchTextBox.ToolTipText = "Enter XQL expression or  search by ID (#<id numeric value>)";
            this.mnuSearchTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.mnuSearchTextBox_KeyPress);
            this.mnuSearchTextBox.MouseHover += new System.EventHandler(this.mnuSearchTextBox_MouseHover);
            // 
            // mnuSearch
            // 
            this.mnuSearch.Name = "mnuSearch";
            this.mnuSearch.RightToLeftAutoMirrorImage = true;
            this.mnuSearch.Size = new System.Drawing.Size(54, 23);
            this.mnuSearch.Text = "Search";
            this.mnuSearch.Click += new System.EventHandler(this.mnuSearch_Click);
            // 
            // mnuHide
            // 
            this.mnuHide.Name = "mnuHide";
            this.mnuHide.Size = new System.Drawing.Size(44, 23);
            this.mnuHide.Text = "Hide";
            this.mnuHide.Visible = false;
            this.mnuHide.Click += new System.EventHandler(this.mnuHide_Click);
            // 
            // lbResult
            // 
            this.lbResult.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbResult.FormattingEnabled = true;
            this.lbResult.Location = new System.Drawing.Point(0, 0);
            this.lbResult.Name = "lbResult";
            this.lbResult.Size = new System.Drawing.Size(837, 121);
            this.lbResult.TabIndex = 1;
            this.lbResult.DoubleClick += new System.EventHandler(this.lbResult_DoubleClick);
            this.lbResult.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbResult_KeyUp);
            // 
            // pnQuery
            // 
            this.pnQuery.BackColor = System.Drawing.Color.Ivory;
            this.pnQuery.Controls.Add(this.stQuery);
            this.pnQuery.Controls.Add(this.lbResult);
            this.pnQuery.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnQuery.Location = new System.Drawing.Point(0, 27);
            this.pnQuery.Name = "pnQuery";
            this.pnQuery.Size = new System.Drawing.Size(837, 142);
            this.pnQuery.TabIndex = 4;
            this.pnQuery.Visible = false;
            // 
            // stQuery
            // 
            this.stQuery.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsNodeCount});
            this.stQuery.Location = new System.Drawing.Point(0, 120);
            this.stQuery.Name = "stQuery";
            this.stQuery.Size = new System.Drawing.Size(837, 22);
            this.stQuery.TabIndex = 2;
            this.stQuery.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.stQuery_ItemClicked);
            // 
            // tsNodeCount
            // 
            this.tsNodeCount.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tsNodeCount.Name = "tsNodeCount";
            this.tsNodeCount.Size = new System.Drawing.Size(0, 17);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsNode});
            this.statusStrip.Location = new System.Drawing.Point(0, 573);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(837, 22);
            this.statusStrip.TabIndex = 5;
            this.statusStrip.Text = "statusStrip1";
            // 
            // tsNode
            // 
            this.tsNode.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tsNode.Name = "tsNode";
            this.tsNode.Size = new System.Drawing.Size(0, 17);
            // 
            // lvDetails
            // 
            this.lvDetails.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.coId,
            this.coType,
            this.coName,
            this.coValue});
            this.lvDetails.ContextMenuStrip = this.MenuContext;
            this.lvDetails.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lvDetails.FullRowSelect = true;
            this.lvDetails.GridLines = true;
            this.lvDetails.Location = new System.Drawing.Point(462, 175);
            this.lvDetails.Name = "lvDetails";
            this.lvDetails.Size = new System.Drawing.Size(351, 229);
            this.lvDetails.TabIndex = 7;
            this.lvDetails.UseCompatibleStateImageBehavior = false;
            this.lvDetails.View = System.Windows.Forms.View.Details;
            this.lvDetails.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lvDetails_KeyUp);
            this.lvDetails.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvDetails_MouseDoubleClick);
            this.lvDetails.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvDetails_MouseUp);
            // 
            // coId
            // 
            this.coId.Text = "ID";
            this.coId.Width = 37;
            // 
            // coType
            // 
            this.coType.Text = "Type";
            this.coType.Width = 48;
            // 
            // coName
            // 
            this.coName.Text = "Name";
            this.coName.Width = 76;
            // 
            // coValue
            // 
            this.coValue.Text = "Value";
            this.coValue.Width = 108;
            // 
            // spContainer
            // 
            this.spContainer.Location = new System.Drawing.Point(291, 183);
            this.spContainer.Name = "spContainer";
            this.spContainer.Size = new System.Drawing.Size(150, 100);
            this.spContainer.TabIndex = 8;
            // 
            // VSFrmXML
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(837, 595);
            this.Controls.Add(this.spContainer);
            this.Controls.Add(this.lvDetails);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.pnQuery);
            this.Controls.Add(this.MenuMain);
            this.Controls.Add(this.tvCat);
            this.MainMenuStrip = this.MenuMain;
            this.Name = "VSFrmXML";
            this.Text = "VSXML";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Activated += new System.EventHandler(this.VSFrmXML_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VSFrmXML_FormClosed);
            this.Load += new System.EventHandler(this.VSFrmXML_Load);
            this.Resize += new System.EventHandler(this.VSFrmXML_Resize);
            this.MenuContext.ResumeLayout(false);
            this.MenuMain.ResumeLayout(false);
            this.MenuMain.PerformLayout();
            this.pnQuery.ResumeLayout(false);
            this.pnQuery.PerformLayout();
            this.stQuery.ResumeLayout(false);
            this.stQuery.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spContainer)).EndInit();
            this.spContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView tvCat;
        private System.Windows.Forms.MenuStrip MenuMain;
        private System.Windows.Forms.ToolStripMenuItem mnuSearch;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip MenuContext;
        private System.Windows.Forms.ToolStripMenuItem mnuEdit;
        private System.Windows.Forms.ToolStripMenuItem mnuCreate;
        private System.Windows.Forms.ToolStripMenuItem mnuDelete;
        private System.Windows.Forms.ToolStripMenuItem mnuCheckIn;
        private System.Windows.Forms.ToolStripMenuItem mnuCheckOut;
        private System.Windows.Forms.ToolStripMenuItem mnuUndoCheckOut;
        private System.Windows.Forms.ToolStripTextBox mnuSearchTextBox;
        private System.Windows.Forms.ListBox lbResult;
        private System.Windows.Forms.Panel pnQuery;
        private System.Windows.Forms.ToolStripMenuItem mnuHide;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mOpenStorage;
        private System.Windows.Forms.ToolStripMenuItem mCreateStorage;
        private System.Windows.Forms.ToolStripMenuItem mCloseStorage;
        private System.Windows.Forms.ToolStripMenuItem mCreate;
        private System.Windows.Forms.ToolStripMenuItem mEdit;
        private System.Windows.Forms.ToolStripMenuItem mDelete;
        private System.Windows.Forms.ToolStripMenuItem mCheckIn;
        private System.Windows.Forms.ToolStripMenuItem mCheckOut;
        private System.Windows.Forms.ToolStripMenuItem mUndoCheckOut;
        private System.Windows.Forms.ToolStripStatusLabel tsNode;
        private System.Windows.Forms.ToolStripMenuItem mDeleteStorage;
        private System.Windows.Forms.ToolStripMenuItem mnuCopyToClip;
        private System.Windows.Forms.StatusStrip stQuery;
        private System.Windows.Forms.ToolStripStatusLabel tsNodeCount;
        private System.Windows.Forms.ToolStripMenuItem mnuSnap;
        private System.Windows.Forms.ToolStripMenuItem mSnap;
        private System.Windows.Forms.ToolStripMenuItem mnuDownloadContent;
        private System.Windows.Forms.ToolStripMenuItem mnuSaveXML;
        private System.Windows.Forms.ToolStripMenuItem mnuSaveXMLWithContent;
        private System.Windows.Forms.ToolStripMenuItem mnuViewXML;
        private System.Windows.Forms.ToolStripMenuItem mnuLoadXML;
        private System.Windows.Forms.ToolStripMenuItem mnuRenameNode;
        private System.Windows.Forms.ListView lvDetails;
        private System.Windows.Forms.ColumnHeader coId;
        private System.Windows.Forms.ColumnHeader coType;
        private System.Windows.Forms.ColumnHeader coName;
        private System.Windows.Forms.ColumnHeader coValue;
        private System.Windows.Forms.SplitContainer spContainer;
        private System.Windows.Forms.ToolStripMenuItem mnuLookUp;
        private System.Windows.Forms.ToolStripComboBox tsSearchType;
    }
}