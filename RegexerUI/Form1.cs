using System.Text.RegularExpressions;
using FastColoredTextBoxNS;

namespace RegexerUI
{
    public partial class RegexerForm : Form
    {
        private Regexer.Regexer regexer;
        private string currentFileName;
        private CancellationTokenSource tokenSource;
        private CancellationTokenSource? delayTokenSource;
        private FastColoredTextBox inputTextbox;
        private FastColoredTextBox outputTextbox;
        private FastColoredTextBox patternTextbox;
        private FastColoredTextBox replaceTextbox;
        private readonly TextStyle _highlightStyle = new(null, Brushes.Gainsboro, FontStyle.Bold);

        public RegexerForm()
        {
            InitializeComponent();
            GetTemplates();
            regexer = new Regexer.Regexer(TimeSpan.FromMinutes(1));
            tokenSource = new CancellationTokenSource();
            loadingProgressBar.Visible = false;
            InitializeFCTextBoxes();
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
                if (result == null) return;
                outputTextbox.Text = result.Result;
                if (result.Matches != null)
                {
                    foreach (var matchPair in result.Matches)
                    {
                        var matchRange = inputTextbox.GetRange(matchPair.InputMatch.Index, matchPair.InputMatch.Index + matchPair.InputMatch.Length);
                        matchRange.SetStyle(_highlightStyle);
                        if (matchPair.OutputMatch == null) continue;
                        matchRange = outputTextbox.GetRange(matchPair.OutputMatch.Index, matchPair.OutputMatch.Index + matchPair.OutputMatch.Length);
                        matchRange.SetStyle(_highlightStyle);
                    }
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
            catch (RegexParseException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
            finally
            {
                delayTokenSource?.Cancel();
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

        private async void patternTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            saveTemplateBut.Enabled = patternTextbox.Text != string.Empty;
            e.ChangedRange.ClearStyle(_highlightStyle);
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[(\w+\|)?u\|[^\r\n]+\]\]");
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[\w+?\|\w{1,2}]\]");
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[\w+?]\]");
            await FindAndReplace();
        }

        private async void replaceTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            e.ChangedRange.ClearStyle(_highlightStyle);
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[\w+\|[^\r\n]+\]\]");
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[\w+?\|\w{1,2}]\]");
            e.ChangedRange.SetStyle(_highlightStyle, @"\[\[\w+?]\]");
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

        void InitializeFCTextBoxes()
        {
            // 
            // inputTextbox
            // 
            inputTextbox = new();
            inputTextbox.Dock = DockStyle.Fill;
            inputTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            inputTextbox.Location = new Point(3, 23);
            //inputTextbox.MaxLength = 524288;
            inputTextbox.Multiline = true;
            inputTextbox.Name = "inputTextbox";
            tableLayoutPanel1.SetRowSpan(inputTextbox, 3);
            //inputTextbox.ScrollBars = ScrollBars.Both;
            inputTextbox.Size = new Size(415, 612);
            inputTextbox.TabIndex = 0;
            inputTextbox.WordWrap = false;
            inputTextbox.BorderStyle = BorderStyle.FixedSingle;
            inputTextbox.ShowLineNumbers = false;
            inputTextbox.TextChanged += inputTextbox_TextChanged;
            // 
            // outputTextbox
            // 
            outputTextbox = new();
            outputTextbox.Dock = DockStyle.Fill;
            outputTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            outputTextbox.Location = new Point(845, 23);
            outputTextbox.Multiline = true;
            outputTextbox.Name = "outputTextbox";
            outputTextbox.ReadOnly = true;
            tableLayoutPanel1.SetRowSpan(outputTextbox, 3);
            outputTextbox.Size = new Size(416, 612);
            outputTextbox.TabIndex = 2;
            outputTextbox.WordWrap = false;
            outputTextbox.ShowLineNumbers = false;
            outputTextbox.ReadOnly = true;
            outputTextbox.BorderStyle = BorderStyle.FixedSingle;
            // 
            // replaceTextbox
            // 
            replaceTextbox = new();
            replaceTextbox.Dock = DockStyle.Fill;
            replaceTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            replaceTextbox.Location = new Point(424, 342);
            replaceTextbox.Multiline = true;
            replaceTextbox.Name = "replaceTextbox";
            replaceTextbox.Size = new Size(415, 293);
            replaceTextbox.TabIndex = 2;
            replaceTextbox.WordWrap = false;
            replaceTextbox.BorderStyle = BorderStyle.FixedSingle;
            replaceTextbox.ShowLineNumbers = false;
            replaceTextbox.TextChanged += replaceTextbox_TextChanged;
            // 
            // patternTextbox
            // 
            patternTextbox = new();
            patternTextbox.Dock = DockStyle.Fill;
            patternTextbox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            patternTextbox.Location = new Point(424, 23);
            patternTextbox.Multiline = true;
            patternTextbox.Name = "patternTextbox";
            patternTextbox.Size = new Size(415, 293);
            patternTextbox.TabIndex = 1;
            patternTextbox.WordWrap = false;
            patternTextbox.BorderStyle = BorderStyle.FixedSingle;
            patternTextbox.ShowLineNumbers = false;
            patternTextbox.TextChanged += patternTextbox_TextChanged;

            tableLayoutPanel1.Controls.Add(inputTextbox, 0, 1);
            tableLayoutPanel1.Controls.Add(outputTextbox, 2, 1);
            tableLayoutPanel1.Controls.Add(replaceTextbox, 1, 3);
            tableLayoutPanel1.Controls.Add(patternTextbox, 1, 1);
        }
    }
}
