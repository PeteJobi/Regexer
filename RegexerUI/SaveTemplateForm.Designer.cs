using System.ComponentModel;

namespace RegexerUI
{
    partial class SaveTemplateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            label1 = new Label();
            templatesComboBox = new ComboBox();
            saveTemplate = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 35);
            label1.Name = "label1";
            label1.Size = new Size(137, 15);
            label1.TabIndex = 0;
            label1.Text = "Enter the template name";
            // 
            // templatesComboBox
            // 
            templatesComboBox.FormattingEnabled = true;
            templatesComboBox.Location = new Point(12, 62);
            templatesComboBox.Name = "templatesComboBox";
            templatesComboBox.Size = new Size(219, 23);
            templatesComboBox.TabIndex = 1;
            // 
            // saveTemplate
            // 
            saveTemplate.Location = new Point(85, 101);
            saveTemplate.Name = "saveTemplate";
            saveTemplate.Size = new Size(75, 23);
            saveTemplate.TabIndex = 2;
            saveTemplate.Text = "Save";
            saveTemplate.UseVisualStyleBackColor = true;
            saveTemplate.Click += saveTemplate_Click;
            // 
            // SaveTemplateForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(243, 158);
            Controls.Add(saveTemplate);
            Controls.Add(templatesComboBox);
            Controls.Add(label1);
            Name = "SaveTemplateForm";
            Text = "Save template as...";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox templatesComboBox;
        private Button saveTemplate;
    }
}