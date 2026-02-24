namespace RegexerUI
{
    partial class RegexerForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegexerForm));
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            loadingProgressBar = new ProgressBar();
            outputTabs = new TabControl();
            outputTab = new TabPage();
            outputMatchesTab = new TabPage();
            outputSubMatchesTab = new TabPage();
            outputDataGridView = new DataGridView();
            label2 = new Label();
            label4 = new Label();
            inputTabs = new TabControl();
            inputTab = new TabPage();
            inputMatchesTab = new TabPage();
            inputSubMatchesTab = new TabPage();
            inputDataGridView = new DataGridView();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            toolStripMenuItem1 = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            copyOutputToInputToolStripMenuItem = new ToolStripMenuItem();
            clearPatternsToolStripMenuItem = new ToolStripMenuItem();
            clearAllToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1 = new MenuStrip();
            toolStripMenuItem2 = new ToolStripMenuItem();
            syncTabsToolStripCheckBox = new ToolStripMenuItem();
            syncScrollToolStripCheckBox = new ToolStripMenuItem();
            wordWrapToolStripCheckBox = new ToolStripMenuItem();
            lineNumbersToolStripCheckbox = new ToolStripMenuItem();
            fasterMLToolStripCheckbox = new ToolStripMenuItem();
            toolStrip1 = new ToolStrip();
            templatesComboBox = new ToolStripComboBox();
            saveTemplateBut = new ToolStripButton();
            deleteTemplateBut = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            prevBut = new ToolStripButton();
            nextBut = new ToolStripButton();
            flowLayoutPanel1 = new FlowLayoutPanel();
            matchNavLabel = new Label();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            outputTabs.SuspendLayout();
            outputSubMatchesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)outputDataGridView).BeginInit();
            inputTabs.SuspendLayout();
            inputSubMatchesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)inputDataGridView).BeginInit();
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel1.Controls.Add(panel1, 2, 0);
            tableLayoutPanel1.Controls.Add(label2, 1, 0);
            tableLayoutPanel1.Controls.Add(label4, 1, 2);
            tableLayoutPanel1.Controls.Add(inputTabs, 0, 0);
            tableLayoutPanel1.Location = new Point(0, 24);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(1264, 657);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // panel1
            // 
            panel1.Controls.Add(loadingProgressBar);
            panel1.Controls.Add(outputTabs);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(842, 0);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            tableLayoutPanel1.SetRowSpan(panel1, 4);
            panel1.Size = new Size(422, 657);
            panel1.TabIndex = 0;
            // 
            // loadingProgressBar
            // 
            loadingProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            loadingProgressBar.Location = new Point(194, 7);
            loadingProgressBar.Name = "loadingProgressBar";
            loadingProgressBar.Size = new Size(228, 10);
            loadingProgressBar.TabIndex = 10;
            // 
            // outputTabs
            // 
            outputTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            outputTabs.Controls.Add(outputTab);
            outputTabs.Controls.Add(outputMatchesTab);
            outputTabs.Controls.Add(outputSubMatchesTab);
            outputTabs.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            outputTabs.Location = new Point(0, 0);
            outputTabs.Margin = new Padding(0);
            outputTabs.Name = "outputTabs";
            outputTabs.Padding = new Point(0, 0);
            outputTabs.SelectedIndex = 0;
            outputTabs.Size = new Size(422, 657);
            outputTabs.TabIndex = 10;
            outputTabs.Selected += outputTabs_Selected;
            // 
            // outputTab
            // 
            outputTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            outputTab.Location = new Point(4, 24);
            outputTab.Margin = new Padding(0);
            outputTab.Name = "outputTab";
            outputTab.Size = new Size(414, 629);
            outputTab.TabIndex = 0;
            outputTab.Text = "Output";
            outputTab.UseVisualStyleBackColor = true;
            // 
            // outputMatchesTab
            // 
            outputMatchesTab.Location = new Point(4, 24);
            outputMatchesTab.Margin = new Padding(0);
            outputMatchesTab.Name = "outputMatchesTab";
            outputMatchesTab.Size = new Size(414, 629);
            outputMatchesTab.TabIndex = 1;
            outputMatchesTab.Text = "Matches";
            outputMatchesTab.UseVisualStyleBackColor = true;
            // 
            // outputSubMatchesTab
            // 
            outputSubMatchesTab.Controls.Add(outputDataGridView);
            outputSubMatchesTab.Location = new Point(4, 24);
            outputSubMatchesTab.Margin = new Padding(0);
            outputSubMatchesTab.Name = "outputSubMatchesTab";
            outputSubMatchesTab.Size = new Size(414, 629);
            outputSubMatchesTab.TabIndex = 2;
            outputSubMatchesTab.Text = "Sub matches";
            outputSubMatchesTab.UseVisualStyleBackColor = true;
            // 
            // outputDataGridView
            // 
            outputDataGridView.AllowUserToAddRows = false;
            outputDataGridView.AllowUserToDeleteRows = false;
            outputDataGridView.BackgroundColor = SystemColors.Control;
            outputDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            outputDataGridView.Dock = DockStyle.Fill;
            outputDataGridView.EnableHeadersVisualStyles = false;
            outputDataGridView.Location = new Point(0, 0);
            outputDataGridView.Name = "outputDataGridView";
            outputDataGridView.ReadOnly = true;
            outputDataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            outputDataGridView.RowTemplate.Height = 25;
            outputDataGridView.Size = new Size(414, 629);
            outputDataGridView.TabIndex = 0;
            outputDataGridView.CellPainting += IndieMatchesDataGridView_CellPainting;
            outputDataGridView.ColumnAdded += outputDataGridView_ColumnWidthChanged;
            outputDataGridView.ColumnWidthChanged += outputDataGridView_ColumnWidthChanged;
            outputDataGridView.Scroll += outputDataGridView_Scroll;
            outputDataGridView.SelectionChanged += IndieMatchesDataGridView_SelectionChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(424, 0);
            label2.Name = "label2";
            label2.Size = new Size(415, 20);
            label2.TabIndex = 5;
            label2.Text = "Pattern";
            label2.TextAlign = ContentAlignment.BottomLeft;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Dock = DockStyle.Fill;
            label4.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(424, 328);
            label4.Name = "label4";
            label4.Size = new Size(415, 20);
            label4.TabIndex = 7;
            label4.Text = "Replace";
            label4.TextAlign = ContentAlignment.BottomLeft;
            // 
            // inputTabs
            // 
            inputTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            inputTabs.Controls.Add(inputTab);
            inputTabs.Controls.Add(inputMatchesTab);
            inputTabs.Controls.Add(inputSubMatchesTab);
            inputTabs.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            inputTabs.Location = new Point(0, 0);
            inputTabs.Margin = new Padding(0);
            inputTabs.Name = "inputTabs";
            inputTabs.Padding = new Point(0, 0);
            tableLayoutPanel1.SetRowSpan(inputTabs, 4);
            inputTabs.SelectedIndex = 0;
            inputTabs.Size = new Size(421, 657);
            inputTabs.TabIndex = 9;
            inputTabs.Selected += inputTabs_Selected;
            // 
            // inputTab
            // 
            inputTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            inputTab.Location = new Point(4, 24);
            inputTab.Margin = new Padding(0);
            inputTab.Name = "inputTab";
            inputTab.Size = new Size(413, 629);
            inputTab.TabIndex = 0;
            inputTab.Text = "Input";
            inputTab.UseVisualStyleBackColor = true;
            // 
            // inputMatchesTab
            // 
            inputMatchesTab.Location = new Point(4, 24);
            inputMatchesTab.Margin = new Padding(0);
            inputMatchesTab.Name = "inputMatchesTab";
            inputMatchesTab.Size = new Size(413, 629);
            inputMatchesTab.TabIndex = 1;
            inputMatchesTab.Text = "Matches";
            inputMatchesTab.UseVisualStyleBackColor = true;
            // 
            // inputSubMatchesTab
            // 
            inputSubMatchesTab.Controls.Add(inputDataGridView);
            inputSubMatchesTab.Location = new Point(4, 24);
            inputSubMatchesTab.Margin = new Padding(0);
            inputSubMatchesTab.Name = "inputSubMatchesTab";
            inputSubMatchesTab.Size = new Size(413, 629);
            inputSubMatchesTab.TabIndex = 2;
            inputSubMatchesTab.Text = "Sub matches";
            inputSubMatchesTab.UseVisualStyleBackColor = true;
            // 
            // inputDataGridView
            // 
            inputDataGridView.AllowUserToAddRows = false;
            inputDataGridView.AllowUserToDeleteRows = false;
            inputDataGridView.BackgroundColor = SystemColors.Control;
            inputDataGridView.Dock = DockStyle.Fill;
            inputDataGridView.EnableHeadersVisualStyles = false;
            inputDataGridView.Location = new Point(0, 0);
            inputDataGridView.Name = "inputDataGridView";
            inputDataGridView.ReadOnly = true;
            inputDataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            inputDataGridView.RowTemplate.Height = 25;
            inputDataGridView.Size = new Size(413, 629);
            inputDataGridView.TabIndex = 0;
            inputDataGridView.CellPainting += IndieMatchesDataGridView_CellPainting;
            inputDataGridView.ColumnAdded += inputDataGridView_ColumnWidthChanged;
            inputDataGridView.ColumnWidthChanged += inputDataGridView_ColumnWidthChanged;
            inputDataGridView.Scroll += inputDataGridView_Scroll;
            inputDataGridView.SelectionChanged += IndieMatchesDataGridView_SelectionChanged;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, saveAsToolStripMenuItem, copyOutputToInputToolStripMenuItem, clearPatternsToolStripMenuItem, clearAllToolStripMenuItem });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(37, 20);
            toolStripMenuItem1.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(186, 22);
            openToolStripMenuItem.Text = "Open input";
            openToolStripMenuItem.Click += openFileBut_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(186, 22);
            saveAsToolStripMenuItem.Text = "Save output";
            saveAsToolStripMenuItem.Click += saveFileBut_Click;
            // 
            // copyOutputToInputToolStripMenuItem
            // 
            copyOutputToInputToolStripMenuItem.Name = "copyOutputToInputToolStripMenuItem";
            copyOutputToInputToolStripMenuItem.Size = new Size(186, 22);
            copyOutputToInputToolStripMenuItem.Text = "Copy output to input";
            copyOutputToInputToolStripMenuItem.Click += outToInBut_Click;
            // 
            // clearPatternsToolStripMenuItem
            // 
            clearPatternsToolStripMenuItem.Name = "clearPatternsToolStripMenuItem";
            clearPatternsToolStripMenuItem.Size = new Size(186, 22);
            clearPatternsToolStripMenuItem.Text = "Clear patterns";
            clearPatternsToolStripMenuItem.Click += clearPatternsToolStripMenuItem_Click;
            // 
            // clearAllToolStripMenuItem
            // 
            clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
            clearAllToolStripMenuItem.Size = new Size(186, 22);
            clearAllToolStripMenuItem.Text = "Clear all";
            clearAllToolStripMenuItem.Click += clearAllToolStripMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Dock = DockStyle.None;
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem2 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(245, 24);
            menuStrip1.TabIndex = 3;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.DropDownItems.AddRange(new ToolStripItem[] { syncTabsToolStripCheckBox, syncScrollToolStripCheckBox, wordWrapToolStripCheckBox, lineNumbersToolStripCheckbox, fasterMLToolStripCheckbox });
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(80, 20);
            toolStripMenuItem2.Text = "Preferences";
            // 
            // syncTabsToolStripCheckBox
            // 
            syncTabsToolStripCheckBox.Checked = true;
            syncTabsToolStripCheckBox.CheckState = CheckState.Checked;
            syncTabsToolStripCheckBox.Name = "syncTabsToolStripCheckBox";
            syncTabsToolStripCheckBox.Size = new Size(180, 22);
            syncTabsToolStripCheckBox.Text = "Sync tabs";
            // 
            // syncScrollToolStripCheckBox
            // 
            syncScrollToolStripCheckBox.Checked = true;
            syncScrollToolStripCheckBox.CheckOnClick = true;
            syncScrollToolStripCheckBox.CheckState = CheckState.Checked;
            syncScrollToolStripCheckBox.Name = "syncScrollToolStripCheckBox";
            syncScrollToolStripCheckBox.Size = new Size(180, 22);
            syncScrollToolStripCheckBox.Text = "Sync scroll";
            // 
            // wordWrapToolStripCheckBox
            // 
            wordWrapToolStripCheckBox.CheckOnClick = true;
            wordWrapToolStripCheckBox.Name = "wordWrapToolStripCheckBox";
            wordWrapToolStripCheckBox.Size = new Size(180, 22);
            wordWrapToolStripCheckBox.Text = "Word wrap";
            wordWrapToolStripCheckBox.Click += wordWrapToolStripCheckBox_Click;
            // 
            // lineNumbersToolStripCheckbox
            // 
            lineNumbersToolStripCheckbox.CheckOnClick = true;
            lineNumbersToolStripCheckbox.Name = "lineNumbersToolStripCheckbox";
            lineNumbersToolStripCheckbox.Size = new Size(180, 22);
            lineNumbersToolStripCheckbox.Text = "Line numbers";
            lineNumbersToolStripCheckbox.Click += lineNumbersToolStripCheckbox_Click;
            // 
            // fasterMLToolStripCheckbox
            // 
            fasterMLToolStripCheckbox.CheckOnClick = true;
            fasterMLToolStripCheckbox.Name = "fasterMLToolStripCheckbox";
            fasterMLToolStripCheckbox.Size = new Size(180, 22);
            fasterMLToolStripCheckbox.Text = "Faster ML";
            fasterMLToolStripCheckbox.CheckedChanged += fasterMLCheckBox_CheckedChanged;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.None;
            toolStrip1.Items.AddRange(new ToolStripItem[] { templatesComboBox, saveTemplateBut, deleteTemplateBut, toolStripSeparator1, prevBut, nextBut });
            toolStrip1.Location = new Point(245, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(415, 24);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // templatesComboBox
            // 
            templatesComboBox.Name = "templatesComboBox";
            templatesComboBox.Size = new Size(170, 24);
            templatesComboBox.ToolTipText = "Select template";
            templatesComboBox.SelectedIndexChanged += templatesComboBox_SelectedIndexChanged;
            // 
            // saveTemplateBut
            // 
            saveTemplateBut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            saveTemplateBut.Image = (Image)resources.GetObject("saveTemplateBut.Image");
            saveTemplateBut.ImageTransparentColor = Color.Magenta;
            saveTemplateBut.Name = "saveTemplateBut";
            saveTemplateBut.Size = new Size(85, 21);
            saveTemplateBut.Text = "Save template";
            saveTemplateBut.Click += saveTemplateBut_Click;
            // 
            // deleteTemplateBut
            // 
            deleteTemplateBut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            deleteTemplateBut.Image = (Image)resources.GetObject("deleteTemplateBut.Image");
            deleteTemplateBut.ImageTransparentColor = Color.Magenta;
            deleteTemplateBut.Name = "deleteTemplateBut";
            deleteTemplateBut.Size = new Size(94, 21);
            deleteTemplateBut.Text = "Delete template";
            deleteTemplateBut.Click += deleteTemplateBut_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 24);
            // 
            // prevBut
            // 
            prevBut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            prevBut.Image = (Image)resources.GetObject("prevBut.Image");
            prevBut.ImageTransparentColor = Color.Magenta;
            prevBut.Name = "prevBut";
            prevBut.Size = new Size(23, 21);
            prevBut.Text = "Previous match";
            prevBut.Click += prevBut_Click;
            // 
            // nextBut
            // 
            nextBut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            nextBut.Image = (Image)resources.GetObject("nextBut.Image");
            nextBut.ImageTransparentColor = Color.Magenta;
            nextBut.Name = "nextBut";
            nextBut.Size = new Size(23, 21);
            nextBut.Text = "Next match";
            nextBut.Click += nextBut_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(menuStrip1);
            flowLayoutPanel1.Controls.Add(toolStrip1);
            flowLayoutPanel1.Controls.Add(matchNavLabel);
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1264, 24);
            flowLayoutPanel1.TabIndex = 3;
            // 
            // matchNavLabel
            // 
            matchNavLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            matchNavLabel.AutoSize = true;
            matchNavLabel.Location = new Point(663, 0);
            matchNavLabel.Name = "matchNavLabel";
            matchNavLabel.Size = new Size(38, 24);
            matchNavLabel.TabIndex = 5;
            matchNavLabel.Text = "label1";
            matchNavLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // RegexerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(flowLayoutPanel1);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "RegexerForm";
            Text = "Regexer";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            outputTabs.ResumeLayout(false);
            outputSubMatchesTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)outputDataGridView).EndInit();
            inputTabs.ResumeLayout(false);
            inputSubMatchesTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)inputDataGridView).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private Label label2;
        private Label label4;
        private TabControl inputTabs;
        private TabPage inputTab;
        private TabPage inputMatchesTab;
        private TabControl outputTabs;
        private TabPage outputTab;
        private TabPage outputMatchesTab;
        private Panel panel1;
        private ProgressBar loadingProgressBar;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStrip toolStrip1;
        private ToolStripButton saveTemplateBut;
        private ToolStripMenuItem syncTabsToolStripCheckBox;
        private ToolStripMenuItem syncScrollToolStripCheckBox;
        private ToolStripMenuItem fasterMLToolStripCheckbox;
        private ToolStripButton deleteTemplateBut;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton prevBut;
        private ToolStripButton nextBut;
        private FlowLayoutPanel flowLayoutPanel1;
        private ToolStripMenuItem copyOutputToInputToolStripMenuItem;
        private ToolStripComboBox templatesComboBox;
        private TabPage inputSubMatchesTab;
        private DataGridView inputDataGridView;
        private TabPage outputSubMatchesTab;
        private DataGridView outputDataGridView;
        private ToolStripMenuItem clearPatternsToolStripMenuItem;
        private ToolStripMenuItem clearAllToolStripMenuItem;
        private Label matchNavLabel;
        private ToolStripMenuItem wordWrapToolStripCheckBox;
        private ToolStripMenuItem lineNumbersToolStripCheckbox;
    }
}
