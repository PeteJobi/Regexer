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
            panel2 = new Panel();
            fasterMLCheckBox = new CheckBox();
            nextBut = new Button();
            prevBut = new Button();
            outToInBut = new Button();
            deleteTemplateBut = new Button();
            saveTemplateBut = new Button();
            templatesComboBox = new ComboBox();
            saveFileBut = new Button();
            openFileBut = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            loadingProgressBar = new ProgressBar();
            outputTabs = new TabControl();
            outputTab = new TabPage();
            outputMatchesTab = new TabPage();
            label2 = new Label();
            label4 = new Label();
            inputTabs = new TabControl();
            inputTab = new TabPage();
            inputMatchesTab = new TabPage();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            panel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            outputTabs.SuspendLayout();
            inputTabs.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.Control;
            panel2.Controls.Add(fasterMLCheckBox);
            panel2.Controls.Add(nextBut);
            panel2.Controls.Add(prevBut);
            panel2.Controls.Add(outToInBut);
            panel2.Controls.Add(deleteTemplateBut);
            panel2.Controls.Add(saveTemplateBut);
            panel2.Controls.Add(templatesComboBox);
            panel2.Controls.Add(saveFileBut);
            panel2.Controls.Add(openFileBut);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 638);
            panel2.Name = "panel2";
            panel2.Size = new Size(1264, 43);
            panel2.TabIndex = 1;
            // 
            // fasterMLCheckBox
            // 
            fasterMLCheckBox.AutoSize = true;
            fasterMLCheckBox.Location = new Point(952, 13);
            fasterMLCheckBox.Name = "fasterMLCheckBox";
            fasterMLCheckBox.Size = new Size(77, 19);
            fasterMLCheckBox.TabIndex = 11;
            fasterMLCheckBox.Text = "Faster ML";
            fasterMLCheckBox.UseVisualStyleBackColor = true;
            fasterMLCheckBox.CheckedChanged += fasterMLCheckBox_CheckedChanged;
            // 
            // nextBut
            // 
            nextBut.Location = new Point(854, 10);
            nextBut.Name = "nextBut";
            nextBut.Size = new Size(75, 23);
            nextBut.TabIndex = 10;
            nextBut.Text = "Next";
            nextBut.UseVisualStyleBackColor = true;
            nextBut.Click += nextBut_Click;
            // 
            // prevBut
            // 
            prevBut.Location = new Point(773, 10);
            prevBut.Name = "prevBut";
            prevBut.Size = new Size(75, 23);
            prevBut.TabIndex = 9;
            prevBut.Text = "Previous";
            prevBut.UseVisualStyleBackColor = true;
            prevBut.Click += prevBut_Click;
            // 
            // outToInBut
            // 
            outToInBut.Location = new Point(196, 10);
            outToInBut.Name = "outToInBut";
            outToInBut.Size = new Size(132, 23);
            outToInBut.TabIndex = 8;
            outToInBut.Text = "Copy output to input";
            outToInBut.UseVisualStyleBackColor = true;
            outToInBut.Click += outToInBut_Click;
            // 
            // deleteTemplateBut
            // 
            deleteTemplateBut.Enabled = false;
            deleteTemplateBut.Location = new Point(634, 10);
            deleteTemplateBut.Name = "deleteTemplateBut";
            deleteTemplateBut.Size = new Size(100, 23);
            deleteTemplateBut.TabIndex = 7;
            deleteTemplateBut.Text = "Delete template";
            deleteTemplateBut.UseVisualStyleBackColor = true;
            deleteTemplateBut.Click += deleteTemplateBut_Click;
            // 
            // saveTemplateBut
            // 
            saveTemplateBut.Enabled = false;
            saveTemplateBut.Location = new Point(538, 10);
            saveTemplateBut.Name = "saveTemplateBut";
            saveTemplateBut.Size = new Size(90, 23);
            saveTemplateBut.TabIndex = 6;
            saveTemplateBut.Text = "Save template";
            saveTemplateBut.UseVisualStyleBackColor = true;
            saveTemplateBut.Click += saveTemplateBut_Click;
            // 
            // templatesComboBox
            // 
            templatesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            templatesComboBox.FormattingEnabled = true;
            templatesComboBox.Location = new Point(366, 10);
            templatesComboBox.Name = "templatesComboBox";
            templatesComboBox.Size = new Size(166, 23);
            templatesComboBox.TabIndex = 5;
            templatesComboBox.SelectedIndexChanged += templatesComboBox_SelectedIndexChanged;
            // 
            // saveFileBut
            // 
            saveFileBut.Location = new Point(106, 10);
            saveFileBut.Name = "saveFileBut";
            saveFileBut.Size = new Size(84, 23);
            saveFileBut.TabIndex = 4;
            saveFileBut.Text = "Save output";
            saveFileBut.Click += saveFileBut_Click;
            // 
            // openFileBut
            // 
            openFileBut.Location = new Point(12, 10);
            openFileBut.Name = "openFileBut";
            openFileBut.Size = new Size(88, 23);
            openFileBut.TabIndex = 3;
            openFileBut.Text = "Select input";
            openFileBut.Click += openFileBut_Click;
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
            tableLayoutPanel1.Size = new Size(1264, 614);
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
            panel1.Size = new Size(422, 614);
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
            outputTabs.Location = new Point(0, 0);
            outputTabs.Margin = new Padding(0);
            outputTabs.Name = "outputTabs";
            outputTabs.Padding = new Point(0, 0);
            outputTabs.SelectedIndex = 0;
            outputTabs.Size = new Size(422, 614);
            outputTabs.TabIndex = 10;
            // 
            // outputTab
            // 
            outputTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            outputTab.Location = new Point(4, 24);
            outputTab.Margin = new Padding(0);
            outputTab.Name = "outputTab";
            outputTab.Size = new Size(414, 586);
            outputTab.TabIndex = 0;
            outputTab.Text = "Output";
            outputTab.UseVisualStyleBackColor = true;
            // 
            // outputMatchesTab
            // 
            outputMatchesTab.Location = new Point(4, 24);
            outputMatchesTab.Margin = new Padding(0);
            outputMatchesTab.Name = "outputMatchesTab";
            outputMatchesTab.Size = new Size(414, 586);
            outputMatchesTab.TabIndex = 1;
            outputMatchesTab.Text = "Matches";
            outputMatchesTab.UseVisualStyleBackColor = true;
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
            label4.Location = new Point(424, 307);
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
            inputTabs.Location = new Point(0, 0);
            inputTabs.Margin = new Padding(0);
            inputTabs.Name = "inputTabs";
            inputTabs.Padding = new Point(0, 0);
            tableLayoutPanel1.SetRowSpan(inputTabs, 4);
            inputTabs.SelectedIndex = 0;
            inputTabs.Size = new Size(421, 614);
            inputTabs.TabIndex = 9;
            // 
            // inputTab
            // 
            inputTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            inputTab.Location = new Point(4, 24);
            inputTab.Margin = new Padding(0);
            inputTab.Name = "inputTab";
            inputTab.Size = new Size(413, 586);
            inputTab.TabIndex = 0;
            inputTab.Text = "Input";
            inputTab.UseVisualStyleBackColor = true;
            // 
            // inputMatchesTab
            // 
            inputMatchesTab.Location = new Point(4, 24);
            inputMatchesTab.Margin = new Padding(0);
            inputMatchesTab.Name = "inputMatchesTab";
            inputMatchesTab.Size = new Size(413, 586);
            inputMatchesTab.TabIndex = 1;
            inputMatchesTab.Text = "Matches";
            inputMatchesTab.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1264, 24);
            menuStrip1.TabIndex = 3;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, saveAsToolStripMenuItem });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(37, 20);
            toolStripMenuItem1.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(114, 22);
            openToolStripMenuItem.Text = "Open";
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(114, 22);
            saveAsToolStripMenuItem.Text = "Save As";
            // 
            // RegexerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(panel2);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "RegexerForm";
            Text = "Regexer";
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            outputTabs.ResumeLayout(false);
            inputTabs.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Panel panel2;
        private TableLayoutPanel tableLayoutPanel1;
        private OpenFileDialog openFileDialog;
        private Button openFileBut;
        private Button saveFileBut;
        private SaveFileDialog saveFileDialog;
        private ComboBox templatesComboBox;
        private Button saveTemplateBut;
        private Label label2;
        private Label label4;
        private Button deleteTemplateBut;
        private Button outToInBut;
        private Button nextBut;
        private Button prevBut;
        private CheckBox fasterMLCheckBox;
        private TabControl inputTabs;
        private TabPage inputTab;
        private TabPage inputMatchesTab;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private TabControl outputTabs;
        private TabPage outputTab;
        private TabPage outputMatchesTab;
        private Panel panel1;
        private ProgressBar loadingProgressBar;
    }
}
