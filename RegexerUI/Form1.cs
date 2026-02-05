using System.Text.RegularExpressions;
using FastColoredTextBoxNS;
using Range = FastColoredTextBoxNS.Range;

namespace RegexerUI
{
    public partial class RegexerForm : Form
    {
        private Regexer.Regexer regexer;
        private string currentFileName;
        private CancellationTokenSource tokenSource;
        private CancellationTokenSource? delayTokenSource;
        private FastColoredTextBox inputTextbox;
        private FastColoredTextBox inputMatchesTextbox;
        private FastColoredTextBox outputTextbox;
        private FastColoredTextBox outputMatchesTextbox;
        private FastColoredTextBox patternTextbox;
        private FastColoredTextBox replaceTextbox;
        private int currentMatchRange;
        private readonly List<(Range inpRange, Range? outRange)> matchRanges = new();
        private readonly FctbManager fctbManager = new();
        private readonly List<bool> inputMatchLineBackgroundData = new();
        private readonly List<bool> outputMatchLineBackgroundData = new();

        public RegexerForm()
        {
            InitializeComponent();
            InitializeFCTextBoxes();
            GetTemplates();
            regexer = new Regexer.Regexer(TimeSpan.FromMinutes(10));
            tokenSource = new CancellationTokenSource();
            loadingProgressBar.Visible = false;
            prevBut.Enabled = false;
            nextBut.Enabled = false;
            fasterMLCheckBox.Visible = false;
        }

        public async Task FindAndReplace()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
            tokenSource = new CancellationTokenSource();
            if (inputTextbox.Text == string.Empty || patternTextbox.Text == string.Empty)
            {
                outputTextbox.Text = inputMatchesTextbox.Text = outputMatchesTextbox.Text = string.Empty;
                matchRanges.Clear();
                inputMatchLineBackgroundData.Clear();
                outputMatchLineBackgroundData.Clear();
                prevBut.Enabled = nextBut.Enabled = false;
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
                if (result.Output == "Cancelled") return;
                outputTextbox.Text = result.Output;
                matchRanges.Clear();
                inputTextbox.Range.ClearStyle(FctbManager.BaseStyle);
                outputTextbox.Range.ClearStyle(FctbManager.BaseStyle);
                inputMatchesTextbox.Text = string.Empty;
                outputMatchesTextbox.Text = string.Empty;
                if (result.Matches != null)
                {
                    inputMatchesTextbox.Text = string.Join('\n', result.Matches.Select(m => m.InputMatch.Text));
                    outputMatchesTextbox.Text = string.Join('\n', result.Matches.Select(m => m.OutputMatch?.Text));

                    var lastInputMatchEndIndex = 0;
                    var lastOutputMatchEndIndex = 0;
                    var alternate = false;
                    inputMatchLineBackgroundData.Clear();
                    outputMatchLineBackgroundData.Clear();
                    foreach (var matchPair in result.Matches)
                    {
                        var matchRange = inputTextbox.GetRange(matchPair.InputMatch.Index, matchPair.InputMatch.Index + matchPair.InputMatch.Length);
                        (Range inpRange, Range? outRange) pairRanges = (matchRange, null);
                        matchRange.SetStyle(FctbManager.BaseStyle);
                        alternate = !alternate;
                        var inputMatchRange = inputMatchesTextbox.GetRange(lastInputMatchEndIndex, lastInputMatchEndIndex += matchPair.InputMatch.Length + 2);
                        inputMatchLineBackgroundData.AddRange(Enumerable.Repeat(alternate, inputMatchRange.End.iLine - inputMatchRange.Start.iLine));
                        if (matchPair.OutputMatch != null)
                        {
                            matchRange = outputTextbox.GetRange(matchPair.OutputMatch.Index, matchPair.OutputMatch.Index + matchPair.OutputMatch.Length);
                            matchRange.SetStyle(FctbManager.BaseStyle);
                            pairRanges.outRange = matchRange;
                            var outputMatchRange = outputMatchesTextbox.GetRange(lastOutputMatchEndIndex, lastOutputMatchEndIndex += matchPair.OutputMatch.Length + 2);
                            outputMatchLineBackgroundData.AddRange(Enumerable.Repeat(alternate, outputMatchRange.End.iLine - outputMatchRange.Start.iLine));
                        }
                        matchRanges.Add(pairRanges);
                    }
                    inputMatchLineBackgroundData.Add(alternate);
                    outputMatchLineBackgroundData.Add(alternate);
                }
                prevBut.Enabled = nextBut.Enabled = matchRanges.Any();
            }
            catch (RegexMatchTimeoutException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
            catch (RegexParseException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                outputTextbox.Text = ex.ToString();
            }
            catch (ArithmeticException ex)
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
                await Task.Delay(TimeSpan.FromSeconds(1), token);
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
            await using var streamWriter = new StreamWriter(fileName);
            await streamWriter.WriteAsync(outputTextbox.Text);
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

        private async void inputTextbox_TextChanged(object? sender, EventArgs e)
        {
            await FindAndReplace();
        }

        private async void patternTextbox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            fctbManager.Highlight(patternTextbox, e.ChangedRange, false);
            saveTemplateBut.Enabled = patternTextbox.Text != string.Empty;
            fasterMLCheckBox.Visible = Regex.IsMatch(patternTextbox.Text, @"\[\[\w+?\|ml\]\]");
            await FindAndReplace();
        }

        private async void replaceTextbox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            fctbManager.Highlight(replaceTextbox, e.ChangedRange, true);
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
            if (File.Exists(filePath)) File.Delete(filePath);
            templatesComboBox.Items.Remove(templateName);
            deleteTemplateBut.Enabled = false;
        }

        private void prevBut_Click(object sender, EventArgs e)
        {
            currentMatchRange--;
            if (currentMatchRange == -1) currentMatchRange = matchRanges.Count - 1;
            inputTextbox.DoRangeVisible(matchRanges[currentMatchRange].inpRange);
            if (matchRanges[currentMatchRange].outRange != null) outputTextbox.DoRangeVisible(matchRanges[currentMatchRange].outRange);
        }

        private void nextBut_Click(object sender, EventArgs e)
        {
            currentMatchRange++;
            if (currentMatchRange == matchRanges.Count) currentMatchRange = 0;
            inputTextbox.DoRangeVisible(matchRanges[currentMatchRange].inpRange);
            if (matchRanges[currentMatchRange].outRange != null) outputTextbox.DoRangeVisible(matchRanges[currentMatchRange].outRange);
        }

        private void fasterMLCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            regexer.EnableFasterML(fasterMLCheckBox.Checked);
        }

        private void InputMatchesTextbox_PaintLine(object? sender, PaintLineEventArgs e)
        {
            e.Graphics.FillRectangle(e.LineIndex >= inputMatchLineBackgroundData.Count ? FctbManager.TransparentBrush :
                inputMatchLineBackgroundData[e.LineIndex] ? FctbManager.MatchStyle.BackgroundBrush : FctbManager.MatchLightStyle.BackgroundBrush, e.LineRect);
        }

        private void OutputMatchesTextbox_PaintLine(object? sender, PaintLineEventArgs e)
        {
            e.Graphics.FillRectangle(e.LineIndex >= outputMatchLineBackgroundData.Count ? FctbManager.TransparentBrush :
                outputMatchLineBackgroundData[e.LineIndex] ? FctbManager.MatchStyle.BackgroundBrush : FctbManager.MatchLightStyle.BackgroundBrush, e.LineRect);
        }

        void InitializeFCTextBoxes()
        {
            inputTextbox = CreateFCTextBoxes(nameof(inputTextbox), 0, inputTextbox_TextChanged);
            patternTextbox = CreateFCTextBoxes(nameof(patternTextbox), 1, patternTextbox_TextChanged);
            replaceTextbox = CreateFCTextBoxes(nameof(replaceTextbox), 2, replaceTextbox_TextChanged);
            outputTextbox = CreateFCTextBoxes(nameof(outputTextbox), 3);
            inputMatchesTextbox = CreateFCTextBoxes(nameof(inputMatchesTextbox));
            outputMatchesTextbox = CreateFCTextBoxes(nameof(outputMatchesTextbox));

            inputTabs.TabPages["inputTab"].Controls.Add(inputTextbox);
            inputTabs.TabPages["inputMatchesTab"].Controls.Add(inputMatchesTextbox);
            outputTabs.TabPages["outputTab"].Controls.Add(outputTextbox);
            outputTabs.TabPages["outputMatchesTab"].Controls.Add(outputMatchesTextbox);
            tableLayoutPanel1.Controls.Add(patternTextbox, 1, 1);
            tableLayoutPanel1.Controls.Add(replaceTextbox, 1, 3);

            inputMatchesTextbox.PaintLine += InputMatchesTextbox_PaintLine;
            outputMatchesTextbox.PaintLine += OutputMatchesTextbox_PaintLine;
        }

        FastColoredTextBox CreateFCTextBoxes(string name, int? tabIndex = null, EventHandler<TextChangedEventArgs>? eventHandler = null)
        {
            var fastTextBox = new FastColoredTextBox();
            fastTextBox.Dock = DockStyle.Fill;
            fastTextBox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            //fastTextBox.MaxLength = 524288;
            fastTextBox.Multiline = true;
            fastTextBox.Name = name;
            if(tabIndex != null) fastTextBox.TabIndex = tabIndex.Value;
            fastTextBox.WordWrap = false;
            fastTextBox.BorderStyle = BorderStyle.FixedSingle;
            fastTextBox.ShowLineNumbers = false;
            fastTextBox.ReadOnly = eventHandler == null;
            fastTextBox.AllowSeveralTextStyleDrawing = true;
            if (eventHandler != null) fastTextBox.TextChanged += eventHandler;
            return fastTextBox;
        }
    }
}
