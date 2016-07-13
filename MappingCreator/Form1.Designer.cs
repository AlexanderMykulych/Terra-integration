namespace MappingCreator
{
	partial class Form1
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
			this.RightPanel = new System.Windows.Forms.Panel();
			this.entityNameGridView = new System.Windows.Forms.DataGridView();
			this.Index = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.BpmEntityNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ServiceEntityName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MainDocPanel = new System.Windows.Forms.Panel();
			this.GridPanel = new System.Windows.Forms.Panel();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.файлToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenConfigFileMenuButton = new System.Windows.Forms.ToolStripMenuItem();
			this.RightPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.entityNameGridView)).BeginInit();
			this.MainDocPanel.SuspendLayout();
			this.GridPanel.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// RightPanel
			// 
			this.RightPanel.Controls.Add(this.entityNameGridView);
			this.RightPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.RightPanel.Location = new System.Drawing.Point(664, 0);
			this.RightPanel.Name = "RightPanel";
			this.RightPanel.Size = new System.Drawing.Size(343, 476);
			this.RightPanel.TabIndex = 0;
			// 
			// entityNameGridView
			// 
			this.entityNameGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.entityNameGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Index,
            this.BpmEntityNameColumn,
            this.ServiceEntityName});
			this.entityNameGridView.Dock = System.Windows.Forms.DockStyle.Top;
			this.entityNameGridView.Location = new System.Drawing.Point(0, 0);
			this.entityNameGridView.MultiSelect = false;
			this.entityNameGridView.Name = "entityNameGridView";
			this.entityNameGridView.ReadOnly = true;
			this.entityNameGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.entityNameGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.entityNameGridView.Size = new System.Drawing.Size(343, 227);
			this.entityNameGridView.TabIndex = 0;
			this.entityNameGridView.RowHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.entityNameGridView_RowHeaderMouseClick);
			// 
			// Index
			// 
			this.Index.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.Index.Frozen = true;
			this.Index.HeaderText = "Index";
			this.Index.Name = "Index";
			this.Index.ReadOnly = true;
			this.Index.Width = 58;
			// 
			// BpmEntityNameColumn
			// 
			this.BpmEntityNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.BpmEntityNameColumn.Frozen = true;
			this.BpmEntityNameColumn.HeaderText = "Bpm name";
			this.BpmEntityNameColumn.Name = "BpmEntityNameColumn";
			this.BpmEntityNameColumn.ReadOnly = true;
			this.BpmEntityNameColumn.Width = 82;
			// 
			// ServiceEntityName
			// 
			this.ServiceEntityName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ServiceEntityName.Frozen = true;
			this.ServiceEntityName.HeaderText = "Service name";
			this.ServiceEntityName.Name = "ServiceEntityName";
			this.ServiceEntityName.ReadOnly = true;
			this.ServiceEntityName.Width = 97;
			// 
			// MainDocPanel
			// 
			this.MainDocPanel.Controls.Add(this.GridPanel);
			this.MainDocPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainDocPanel.Location = new System.Drawing.Point(0, 0);
			this.MainDocPanel.Name = "MainDocPanel";
			this.MainDocPanel.Size = new System.Drawing.Size(664, 476);
			this.MainDocPanel.TabIndex = 1;
			// 
			// GridPanel
			// 
			this.GridPanel.Controls.Add(this.menuStrip1);
			this.GridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GridPanel.Location = new System.Drawing.Point(0, 0);
			this.GridPanel.Name = "GridPanel";
			this.GridPanel.Size = new System.Drawing.Size(664, 476);
			this.GridPanel.TabIndex = 0;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.файлToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(664, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(12, 20);
			// 
			// файлToolStripMenuItem
			// 
			this.файлToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenConfigFileMenuButton});
			this.файлToolStripMenuItem.Name = "файлToolStripMenuItem";
			this.файлToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this.файлToolStripMenuItem.Text = "Файл";
			// 
			// OpenConfigFileMenuButton
			// 
			this.OpenConfigFileMenuButton.Name = "OpenConfigFileMenuButton";
			this.OpenConfigFileMenuButton.Size = new System.Drawing.Size(231, 22);
			this.OpenConfigFileMenuButton.Text = "Отрыть файл конфигурации";
			this.OpenConfigFileMenuButton.Click += new System.EventHandler(this.OpenConfigFileMenuButton_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1007, 476);
			this.Controls.Add(this.MainDocPanel);
			this.Controls.Add(this.RightPanel);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.Text = "Form1";
			this.RightPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.entityNameGridView)).EndInit();
			this.MainDocPanel.ResumeLayout(false);
			this.GridPanel.ResumeLayout(false);
			this.GridPanel.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel RightPanel;
		private System.Windows.Forms.Panel MainDocPanel;
		private System.Windows.Forms.Panel GridPanel;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem файлToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenConfigFileMenuButton;
		private System.Windows.Forms.DataGridView entityNameGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn Index;
		private System.Windows.Forms.DataGridViewTextBoxColumn BpmEntityNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ServiceEntityName;

	}
}

