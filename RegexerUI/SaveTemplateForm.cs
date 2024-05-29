using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegexerUI
{
    public partial class SaveTemplateForm : Form
    {
        private readonly string _pattern;
        private readonly string _replace;
        public static string TEMPLATES_FOLDER = "RegexerTemplates";
        public static string TEMPLATE_SEPARATOR = "\n-----------\n";
        public string? SavedTemplate;

        public SaveTemplateForm(ComboBox.ObjectCollection existingTemplates, string selectedTemplate, string pattern, string replace)
        {
            InitializeComponent();
            _pattern = pattern;
            _replace = replace;
            foreach (var existingTemplate in existingTemplates)
            {
                templatesComboBox.Items.Add(existingTemplate);
            }
            templatesComboBox.Text = selectedTemplate;
        }

        private async void saveTemplate_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(TEMPLATES_FOLDER)) Directory.CreateDirectory(TEMPLATES_FOLDER);
            var templatePath = $"{TEMPLATES_FOLDER}/{templatesComboBox.Text}.txt";
            if(File.Exists(templatePath) && MessageBox.Show($"Overwrite {templatesComboBox.Text}?", "Overwrite template?", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            await using var fileStream = File.OpenWrite(templatePath);
            await using var streamWriter = new StreamWriter(fileStream);
            await streamWriter.WriteLineAsync($"{_pattern}{TEMPLATE_SEPARATOR}{_replace}");
            SavedTemplate = templatesComboBox.Text;
            Close();
        }
    }
}
