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
            outToInBut = new Button();
            deleteTemplateBut = new Button();
            saveTemplateBut = new Button();
            templatesComboBox = new ComboBox();
            saveFileBut = new Button();
            openFileBut = new Button();
            loadingProgressBar = new ProgressBar();
            tableLayoutPanel1 = new TableLayoutPanel();
            inputTextbox = new TextBox();
            outputTextbox = new TextBox();
            replaceTextbox = new TextBox();
            patternTextbox = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label4 = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            label3 = new Label();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            panel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.Control;
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
            // loadingProgressBar
            // 
            loadingProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            loadingProgressBar.Location = new Point(53, 5);
            loadingProgressBar.Margin = new Padding(0, 5, 0, 0);
            loadingProgressBar.Name = "loadingProgressBar";
            loadingProgressBar.Size = new Size(363, 9);
            loadingProgressBar.Style = ProgressBarStyle.Marquee;
            loadingProgressBar.TabIndex = 4;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel1.Controls.Add(inputTextbox, 0, 1);
            tableLayoutPanel1.Controls.Add(outputTextbox, 2, 1);
            tableLayoutPanel1.Controls.Add(replaceTextbox, 1, 3);
            tableLayoutPanel1.Controls.Add(patternTextbox, 1, 1);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(label2, 1, 0);
            tableLayoutPanel1.Controls.Add(label4, 1, 2);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(1264, 638);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // inputTextbox
            // 
            inputTextbox.Dock = DockStyle.Fill;
            inputTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            inputTextbox.Location = new Point(3, 23);
            inputTextbox.Multiline = true;
            inputTextbox.Name = "inputTextbox";
            tableLayoutPanel1.SetRowSpan(inputTextbox, 3);
            inputTextbox.ScrollBars = ScrollBars.Both;
            inputTextbox.Size = new Size(415, 612);
            inputTextbox.TabIndex = 0;
            inputTextbox.WordWrap = false;
            inputTextbox.TextChanged += inputTextbox_TextChanged;
            // 
            // outputTextbox
            // 
            outputTextbox.Dock = DockStyle.Fill;
            outputTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            outputTextbox.Location = new Point(845, 23);
            outputTextbox.Multiline = true;
            outputTextbox.Name = "outputTextbox";
            outputTextbox.ReadOnly = true;
            tableLayoutPanel1.SetRowSpan(outputTextbox, 3);
            outputTextbox.ScrollBars = ScrollBars.Both;
            outputTextbox.Size = new Size(416, 612);
            outputTextbox.TabIndex = 2;
            outputTextbox.WordWrap = false;
            // 
            // replaceTextbox
            // 
            replaceTextbox.Dock = DockStyle.Fill;
            replaceTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            replaceTextbox.Location = new Point(424, 342);
            replaceTextbox.Multiline = true;
            replaceTextbox.Name = "replaceTextbox";
            replaceTextbox.ScrollBars = ScrollBars.Both;
            replaceTextbox.Size = new Size(415, 293);
            replaceTextbox.TabIndex = 2;
            replaceTextbox.WordWrap = false;
            replaceTextbox.TextChanged += replaceTextbox_TextChanged;
            // 
            // patternTextbox
            // 
            patternTextbox.Dock = DockStyle.Fill;
            patternTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            patternTextbox.Location = new Point(424, 23);
            patternTextbox.Multiline = true;
            patternTextbox.Name = "patternTextbox";
            patternTextbox.ScrollBars = ScrollBars.Both;
            patternTextbox.Size = new Size(415, 293);
            patternTextbox.TabIndex = 1;
            patternTextbox.WordWrap = false;
            patternTextbox.TextChanged += patternTextbox_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(415, 20);
            label1.TabIndex = 3;
            label1.Text = "Input";
            label1.TextAlign = ContentAlignment.BottomLeft;
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
            label4.Location = new Point(424, 319);
            label4.Name = "label4";
            label4.Size = new Size(415, 20);
            label4.TabIndex = 7;
            label4.Text = "Replace";
            label4.TextAlign = ContentAlignment.BottomLeft;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(loadingProgressBar, 1, 0);
            tableLayoutPanel2.Controls.Add(label3, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(845, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(416, 14);
            tableLayoutPanel2.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Fill;
            label3.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(3, 0);
            label3.Name = "label3";
            label3.Size = new Size(47, 14);
            label3.TabIndex = 6;
            label3.Text = "Output";
            label3.TextAlign = ContentAlignment.BottomLeft;
            // 
            // openFileDialog
            // 
            openFileDialog.FileName = "302.txt";
            // 
            // RegexerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(panel2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "RegexerForm";
            Text = "Regexer";
            panel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Panel panel2;
        private TableLayoutPanel tableLayoutPanel1;
        private TextBox inputTextbox;
        private TextBox replaceTextbox;
        private TextBox outputTextbox;
        private OpenFileDialog openFileDialog;
        private Button openFileBut;
        private Button saveFileBut;
        private SaveFileDialog saveFileDialog;
        private ComboBox templatesComboBox;
        private Button saveTemplateBut;
        private ProgressBar loadingProgressBar;
        private Label label1;
        private TextBox patternTextbox;
        private Label label2;
        private Label label3;
        private Label label4;
        private TableLayoutPanel tableLayoutPanel2;
        private Button deleteTemplateBut;
        private Button outToInBut;
    }
}
