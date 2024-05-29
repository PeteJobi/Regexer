using System.Text.RegularExpressions;

namespace RegexerUI
{
    public partial class RegexerForm : Form
    {
        private Regexer.Regexer regexer;
        private string currentFileName;
        private CancellationTokenSource tokenSource;
        private CancellationTokenSource? delayTokenSource;

        public RegexerForm()
        {
            InitializeComponent();
            GetTemplates();
            regexer = new Regexer.Regexer(TimeSpan.FromMinutes(1));
            tokenSource = new CancellationTokenSource();
            loadingProgressBar.Visible = false;
        }

        public async Task FindAndReplace()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
            tokenSource = new CancellationTokenSource();
            if (inputTextbox.Text == string.Empty || patternTextbox.Text == string.Empty)
            {
                outputTextbox.Text = inputTextbox.Text;
                return;
            }

            if (delayTokenSource == null)
            {
                delayTokenSource = new CancellationTokenSource();
                _ = ShowDelay(delayTokenSource.Token);
            }

            try
            {
                var result = await regexer.AutoRegex(inputTextbox.Text, patternTextbox.Text, replaceTextbox.Text, tokenSource.Token);
                if (result == "Cancelled") return;
                outputTextbox.Text = result; 
                delayTokenSource?.Cancel();
            }
            catch (RegexMatchTimeoutException ex)
            {
                outputTextbox.Text = ex.ToString();
                delayTokenSource?.Cancel();
            }
            catch (RegexParseException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
        }

        async Task ShowDelay(CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
                loadingProgressBar.Visible = true;
                await Task.Delay(-1, token);
            }
            catch (TaskCanceledException)
            {
                loadingProgressBar.Visible = false;
                delayTokenSource.Dispose();
                delayTokenSource = null;
            }
        }

        public async Task ProcessInputFile(string fileName)
        {
            using var streamReader = File.OpenText(fileName);
            inputTextbox.Text = await streamReader.ReadToEndAsync();
        }

        public async Task SaveOutputFile(string fileName)
        {
            await using var fileStream = File.OpenWrite(fileName);
            await using var streamWriter = new StreamWriter(fileStream);
            await streamWriter.WriteLineAsync(outputTextbox.Text);
        }

        private void GetTemplates()
        {
            if (!Directory.Exists(SaveTemplateForm.TEMPLATES_FOLDER)) return;
            var templatesFiles = Directory.EnumerateFiles(SaveTemplateForm.TEMPLATES_FOLDER);
            templatesComboBox.Items.Clear();
            foreach (var templateFile in templatesFiles)
            {
                templatesComboBox.Items.Add(Path.GetFileNameWithoutExtension(templateFile));
            }
        }

        private async Task ApplyTemplate(string templateName)
        {
            using var streamReader = File.OpenText($"{SaveTemplateForm.TEMPLATES_FOLDER}/{templateName}.txt");
            var template = (await streamReader.ReadToEndAsync()).Split(SaveTemplateForm.TEMPLATE_SEPARATOR);
            if (template.Length < 2)
            {
                MessageBox.Show("Template invalid");
                return;
            }
            patternTextbox.Text = template[0];
            replaceTextbox.Text = template[1];
        }

        private async void inputTextbox_TextChanged(object sender, EventArgs e)
        {
            await FindAndReplace();
        }

        private async void patternTextbox_TextChanged(object sender, EventArgs e)
        {
            saveTemplateBut.Enabled = patternTextbox.Text != string.Empty;
            await FindAndReplace();
        }

        private async void replaceTextbox_TextChanged(object sender, EventArgs e)
        {
            await FindAndReplace();
        }

        private async void templatesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            await ApplyTemplate(templatesComboBox.Text);
            deleteTemplateBut.Enabled = !string.IsNullOrEmpty(templatesComboBox.Text);
        }

        private async void openFileBut_Click(object sender, EventArgs e)
        {
            openFileDialog.Title = "Select a text file";
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            currentFileName = openFileDialog.FileName;
            await ProcessInputFile(openFileDialog.FileName);
        }

        private async void saveFileBut_Click(object sender, EventArgs e)
        {
            saveFileDialog.FileName = currentFileName;
            saveFileDialog.Title = "Save to text file";
            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
            await SaveOutputFile(saveFileDialog.FileName);
        }

        private void saveTemplateBut_Click(object sender, EventArgs e)
        {
            var saveTemplateForm = new SaveTemplateForm(templatesComboBox.Items, templatesComboBox.Text, patternTextbox.Text, replaceTextbox.Text);
            saveTemplateForm.ShowDialog();
            if (saveTemplateForm.SavedTemplate == null) return;
            GetTemplates();
            templatesComboBox.Text = saveTemplateForm.SavedTemplate;
        }

        private void outToInBut_Click(object sender, EventArgs e)
        {
            inputTextbox.Text = outputTextbox.Text;
        }

        private void deleteTemplateBut_Click(object sender, EventArgs e)
        {
            var templateName = templatesComboBox.Text;
            if (MessageBox.Show($"Are you sure you want to delete {templateName}?", "Delete template?", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            var filePath = $"{SaveTemplateForm.TEMPLATES_FOLDER}/{templateName}.txt";
            if(File.Exists(filePath)) File.Delete(filePath);
            templatesComboBox.Items.Remove(templateName);
            deleteTemplateBut.Enabled = false;
        }
    }
}
