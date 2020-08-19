namespace VStorageExplorer
{
    partial class VSFrmExplorer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VSFrmExplorer));
            this.listSpace = new System.Windows.Forms.ListBox();
            this.CSMMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.csCreateSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.csCreateSpacePartition = new System.Windows.Forms.ToolStripMenuItem();
            this.csDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.csExtend = new System.Windows.Forms.ToolStripMenuItem();
            this.csDumpSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.csDumpStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.csRestoreSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.csRestoreStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.panel1 = new System.Windows.Forms.Panel();
            this.TSMMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMcreate = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMCreateSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMCreatePartition = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMDeleteSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMExtendSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.TSMDump = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMDumpStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMDumpSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMRestore = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMRestoreStorage = new System.Windows.Forms.ToolStripMenuItem();
            this.TSMRestoreSpace = new System.Windows.Forms.ToolStripMenuItem();
            this.cbShow = new System.Windows.Forms.ToolStripComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.CSMMenu.SuspendLayout();
            this.panel1.SuspendLayout();
            this.TSMMenu.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // listSpace
            // 
            this.listSpace.ContextMenuStrip = this.CSMMenu;
            this.listSpace.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.listSpace.FormattingEnabled = true;
            this.listSpace.ItemHeight = 19;
            this.listSpace.Location = new System.Drawing.Point(0, 34);
            this.listSpace.Name = "listSpace";
            this.listSpace.Size = new System.Drawing.Size(631, 156);
            this.listSpace.TabIndex = 3;
            this.listSpace.SelectedIndexChanged += new System.EventHandler(this.listSpace_SelectedIndexChanged);
            // 
            // CSMMenu
            // 
            this.CSMMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.csCreateSpace,
            this.csCreateSpacePartition,
            this.csDelete,
            this.csExtend,
            this.csDumpSpace,
            this.csDumpStorage,
            this.csRestoreSpace,
            this.csRestoreStorage});
            this.CSMMenu.Name = "CSMMenu";
            this.CSMMenu.Size = new System.Drawing.Size(156, 180);
            // 
            // csCreateSpace
            // 
            this.csCreateSpace.Name = "csCreateSpace";
            this.csCreateSpace.Size = new System.Drawing.Size(155, 22);
            this.csCreateSpace.Text = "Create space";
            this.csCreateSpace.Visible = false;
            this.csCreateSpace.Click += new System.EventHandler(this.csCreateSpace_Click);
            // 
            // csCreateSpacePartition
            // 
            this.csCreateSpacePartition.Name = "csCreateSpacePartition";
            this.csCreateSpacePartition.Size = new System.Drawing.Size(155, 22);
            this.csCreateSpacePartition.Text = "Add partition";
            this.csCreateSpacePartition.Visible = false;
            this.csCreateSpacePartition.Click += new System.EventHandler(this.csCreateSpacePartition_Click);
            // 
            // csDelete
            // 
            this.csDelete.Name = "csDelete";
            this.csDelete.Size = new System.Drawing.Size(155, 22);
            this.csDelete.Text = "Delete space";
            this.csDelete.Visible = false;
            this.csDelete.Click += new System.EventHandler(this.csDelete_Click);
            // 
            // csExtend
            // 
            this.csExtend.Name = "csExtend";
            this.csExtend.Size = new System.Drawing.Size(155, 22);
            this.csExtend.Text = "Extend space";
            this.csExtend.Visible = false;
            this.csExtend.Click += new System.EventHandler(this.csExtend_Click);
            // 
            // csDumpSpace
            // 
            this.csDumpSpace.Name = "csDumpSpace";
            this.csDumpSpace.Size = new System.Drawing.Size(155, 22);
            this.csDumpSpace.Text = "Dump space";
            this.csDumpSpace.Visible = false;
            this.csDumpSpace.Click += new System.EventHandler(this.csDumpSpace_Click);
            // 
            // csDumpStorage
            // 
            this.csDumpStorage.Name = "csDumpStorage";
            this.csDumpStorage.Size = new System.Drawing.Size(155, 22);
            this.csDumpStorage.Text = "Dump storage";
            this.csDumpStorage.Visible = false;
            this.csDumpStorage.Click += new System.EventHandler(this.csDumpStorage_Click);
            // 
            // csRestoreSpace
            // 
            this.csRestoreSpace.Name = "csRestoreSpace";
            this.csRestoreSpace.Size = new System.Drawing.Size(155, 22);
            this.csRestoreSpace.Text = "Restore space";
            this.csRestoreSpace.Visible = false;
            this.csRestoreSpace.Click += new System.EventHandler(this.csRestoreSpace_Click);
            // 
            // csRestoreStorage
            // 
            this.csRestoreStorage.Name = "csRestoreStorage";
            this.csRestoreStorage.Size = new System.Drawing.Size(155, 22);
            this.csRestoreStorage.Text = "Restore storage";
            this.csRestoreStorage.Visible = false;
            this.csRestoreStorage.Click += new System.EventHandler(this.csRestoreStorage_Click);
            // 
            // txtInfo
            // 
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtInfo.Location = new System.Drawing.Point(0, 0);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtInfo.Size = new System.Drawing.Size(663, 333);
            this.txtInfo.TabIndex = 0;
            // 
            // statusStrip
            // 
            this.statusStrip.Location = new System.Drawing.Point(0, 533);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(663, 22);
            this.statusStrip.TabIndex = 8;
            this.statusStrip.Text = "statusStrip1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.listSpace);
            this.panel1.Controls.Add(this.TSMMenu);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(663, 200);
            this.panel1.TabIndex = 9;
            // 
            // TSMMenu
            // 
            this.TSMMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.cbShow});
            this.TSMMenu.Location = new System.Drawing.Point(0, 0);
            this.TSMMenu.Name = "TSMMenu";
            this.TSMMenu.Size = new System.Drawing.Size(663, 27);
            this.TSMMenu.TabIndex = 8;
            this.TSMMenu.Text = "menuStrip2";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuOpen,
            this.TSMcreate,
            this.TSMDeleteSpace,
            this.TSMExtendSpace,
            this.toolStripSeparator1,
            this.TSMDump,
            this.TSMRestore});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // mnuOpen
            // 
            this.mnuOpen.Name = "mnuOpen";
            this.mnuOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.mnuOpen.Size = new System.Drawing.Size(146, 22);
            this.mnuOpen.Text = "Open";
            this.mnuOpen.Click += new System.EventHandler(this.mnuOpen_Click);
            // 
            // TSMcreate
            // 
            this.TSMcreate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TSMCreateSpace,
            this.TSMCreatePartition});
            this.TSMcreate.Enabled = false;
            this.TSMcreate.Name = "TSMcreate";
            this.TSMcreate.Size = new System.Drawing.Size(146, 22);
            this.TSMcreate.Text = "Create";
            // 
            // TSMCreateSpace
            // 
            this.TSMCreateSpace.Name = "TSMCreateSpace";
            this.TSMCreateSpace.Size = new System.Drawing.Size(144, 22);
            this.TSMCreateSpace.Text = "New space";
            this.TSMCreateSpace.Click += new System.EventHandler(this.TSMCreateSpace_Click);
            // 
            // TSMCreatePartition
            // 
            this.TSMCreatePartition.Enabled = false;
            this.TSMCreatePartition.Name = "TSMCreatePartition";
            this.TSMCreatePartition.Size = new System.Drawing.Size(144, 22);
            this.TSMCreatePartition.Text = "Add partition";
            this.TSMCreatePartition.Click += new System.EventHandler(this.TSMCreatePartition_Click);
            // 
            // TSMDeleteSpace
            // 
            this.TSMDeleteSpace.Enabled = false;
            this.TSMDeleteSpace.Name = "TSMDeleteSpace";
            this.TSMDeleteSpace.Size = new System.Drawing.Size(146, 22);
            this.TSMDeleteSpace.Text = "Delete space";
            this.TSMDeleteSpace.Click += new System.EventHandler(this.TSMDeleteSpace_Click);
            // 
            // TSMExtendSpace
            // 
            this.TSMExtendSpace.Enabled = false;
            this.TSMExtendSpace.Name = "TSMExtendSpace";
            this.TSMExtendSpace.Size = new System.Drawing.Size(146, 22);
            this.TSMExtendSpace.Text = "Extend space";
            this.TSMExtendSpace.Click += new System.EventHandler(this.TSMExtendSpace_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(143, 6);
            // 
            // TSMDump
            // 
            this.TSMDump.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TSMDumpStorage,
            this.TSMDumpSpace});
            this.TSMDump.Enabled = false;
            this.TSMDump.Name = "TSMDump";
            this.TSMDump.Size = new System.Drawing.Size(146, 22);
            this.TSMDump.Text = "Dump";
            // 
            // TSMDumpStorage
            // 
            this.TSMDumpStorage.Name = "TSMDumpStorage";
            this.TSMDumpStorage.Size = new System.Drawing.Size(114, 22);
            this.TSMDumpStorage.Text = "Storage";
            this.TSMDumpStorage.Click += new System.EventHandler(this.TSMDumpStorage_Click);
            // 
            // TSMDumpSpace
            // 
            this.TSMDumpSpace.Name = "TSMDumpSpace";
            this.TSMDumpSpace.Size = new System.Drawing.Size(114, 22);
            this.TSMDumpSpace.Text = "Space";
            this.TSMDumpSpace.Click += new System.EventHandler(this.TSMDumpSpace_Click);
            // 
            // TSMRestore
            // 
            this.TSMRestore.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TSMRestoreStorage,
            this.TSMRestoreSpace});
            this.TSMRestore.Enabled = false;
            this.TSMRestore.Name = "TSMRestore";
            this.TSMRestore.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.TSMRestore.Size = new System.Drawing.Size(146, 22);
            this.TSMRestore.Text = "Restore";
            // 
            // TSMRestoreStorage
            // 
            this.TSMRestoreStorage.Name = "TSMRestoreStorage";
            this.TSMRestoreStorage.Size = new System.Drawing.Size(114, 22);
            this.TSMRestoreStorage.Text = "Storage";
            this.TSMRestoreStorage.Click += new System.EventHandler(this.TSMRestoreStorage_Click);
            // 
            // TSMRestoreSpace
            // 
            this.TSMRestoreSpace.Name = "TSMRestoreSpace";
            this.TSMRestoreSpace.Size = new System.Drawing.Size(114, 22);
            this.TSMRestoreSpace.Text = "Space";
            this.TSMRestoreSpace.Click += new System.EventHandler(this.TSMRestoreSpace_Click);
            // 
            // cbShow
            // 
            this.cbShow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbShow.Items.AddRange(new object[] {
            "Display Space Info",
            "Display Free Space",
            "Display Pool Allocation"});
            this.cbShow.Name = "cbShow";
            this.cbShow.Size = new System.Drawing.Size(140, 23);
            this.cbShow.Visible = false;
            this.cbShow.SelectedIndexChanged += new System.EventHandler(this.cbShow_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.txtInfo);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 200);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(663, 333);
            this.panel2.TabIndex = 10;
            // 
            // VSFrmExplorer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(663, 555);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "VSFrmExplorer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Virtual Storage Explorer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VSFrmAdm_FormClosed);
            this.Load += new System.EventHandler(this.VSFrmAdm_Load);
            this.Resize += new System.EventHandler(this.VSFrmAdmin_Resize);
            this.CSMMenu.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.TSMMenu.ResumeLayout(false);
            this.TSMMenu.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listSpace;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.MenuStrip TSMMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuOpen;
        private System.Windows.Forms.ToolStripMenuItem TSMcreate;
        private System.Windows.Forms.ToolStripMenuItem TSMCreateSpace;
        private System.Windows.Forms.ToolStripMenuItem TSMCreatePartition;
        private System.Windows.Forms.ToolStripMenuItem TSMDeleteSpace;
        private System.Windows.Forms.ToolStripMenuItem TSMExtendSpace;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem TSMDump;
        private System.Windows.Forms.ToolStripMenuItem TSMDumpStorage;
        private System.Windows.Forms.ToolStripMenuItem TSMDumpSpace;
        private System.Windows.Forms.ToolStripMenuItem TSMRestore;
        private System.Windows.Forms.ToolStripMenuItem TSMRestoreStorage;
        private System.Windows.Forms.ToolStripMenuItem TSMRestoreSpace;
        private System.Windows.Forms.ToolStripComboBox cbShow;
        private System.Windows.Forms.ContextMenuStrip CSMMenu;
        private System.Windows.Forms.ToolStripMenuItem csCreateSpace;
        private System.Windows.Forms.ToolStripMenuItem csDelete;
        private System.Windows.Forms.ToolStripMenuItem csExtend;
        private System.Windows.Forms.ToolStripMenuItem csDumpSpace;
        private System.Windows.Forms.ToolStripMenuItem csDumpStorage;
        private System.Windows.Forms.ToolStripMenuItem csRestoreSpace;
        private System.Windows.Forms.ToolStripMenuItem csRestoreStorage;
        private System.Windows.Forms.ToolStripMenuItem csCreateSpacePartition;
    }
}