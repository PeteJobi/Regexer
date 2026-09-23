using FastColoredTextBoxNS;
using RegexerV2;
using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Range = FastColoredTextBoxNS.Range;

namespace RegexerUIV2
{
    public partial class RegexerForm : Form
    {
        private readonly Regexer regexer;
        private string currentFileName;
        private CancellationTokenSource tokenSource;
        private CancellationTokenSource? delayTokenSource;
        private FastColoredTextBox inputTextbox;
        private FastColoredTextBox inputMatchesTextbox;
        private FastColoredTextBox outputTextbox;
        private FastColoredTextBox outputMatchesTextbox;
        private FastColoredTextBox findTextbox;
        private FastColoredTextBox replaceTextbox;
        private int currentMatchRange;
        private readonly List<(Range inpRange, Range? outRange)> matchRanges = new();
        private readonly FctbManager fctbManager = new();
        private readonly List<bool> inputMatchLineBackgroundData = new();
        private readonly List<bool> outputMatchLineBackgroundData = new();
        private ScrollValues inputScroll;
        private ScrollValues outputScroll;
        private bool indieMatchIsScrolling;
        private readonly StringFormat _ellipsisFormat = new()
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };


        public RegexerForm()
        {
            InitializeComponent();
            InitializeFcTextBoxes();
            GetTemplates();
            regexer = new Regexer(TimeSpan.FromMinutes(10));
            tokenSource = new CancellationTokenSource();
            loadingProgressBar.Visible = false;
            prevBut.Enabled = false;
            nextBut.Enabled = false;
            matchNavLabel.Visible = false;
        }

        private void InitializeFcTextBoxes()
        {
            inputTextbox = CreateFcTextBoxes(nameof(inputTextbox), 0, inputTextbox_TextChanged);
            findTextbox = CreateFcTextBoxes(nameof(findTextbox), 1, findTextbox_TextChanged);
            replaceTextbox = CreateFcTextBoxes(nameof(replaceTextbox), 2, replaceTextbox_TextChanged);
            outputTextbox = CreateFcTextBoxes(nameof(outputTextbox), 3);
            inputMatchesTextbox = CreateFcTextBoxes(nameof(inputMatchesTextbox));
            outputMatchesTextbox = CreateFcTextBoxes(nameof(outputMatchesTextbox));

            inputTabs.TabPages["inputTab"].Controls.Add(inputTextbox);
            inputTabs.TabPages["inputMatchesTab"].Controls.Add(inputMatchesTextbox);
            outputTabs.TabPages["outputTab"].Controls.Add(outputTextbox);
            outputTabs.TabPages["outputMatchesTab"].Controls.Add(outputMatchesTextbox);
            tableLayoutPanel1.Controls.Add(findTextbox, 1, 1);
            tableLayoutPanel1.Controls.Add(replaceTextbox, 1, 3);

            fctbManager.SetupTextBoxStyles(inputTextbox, outputTextbox);
            fctbManager.SetupPatternTextBoxes(findTextbox, replaceTextbox);

            inputMatchesTextbox.PaintLine += InputMatchesTextbox_PaintLine;
            outputMatchesTextbox.PaintLine += OutputMatchesTextbox_PaintLine;

            inputTextbox.SizeChanged += InputTextbox_SizeChanged;
            outputTextbox.SizeChanged += OutputTextbox_SizeChanged;
            inputMatchesTextbox.SizeChanged += InputMatchesTextbox_SizeChanged;
            outputMatchesTextbox.SizeChanged += OutputMatchesTextbox_SizeChanged;
            inputTextbox.Scroll += InputTextbox_Scroll;
            outputTextbox.Scroll += OutputTextbox_Scroll;
            inputMatchesTextbox.Scroll += InputMatchesTextbox_Scroll;
            outputMatchesTextbox.Scroll += OutputMatchesTextbox_Scroll;
            inputDataGridView.MouseWheel += (_, args) => DataGridView_MouseWheel(inputDataGridView, false, args);
            outputDataGridView.MouseWheel += (_, args) => DataGridView_MouseWheel(outputDataGridView, true, args);
        }

        private static FastColoredTextBox CreateFcTextBoxes(string name, int? tabIndex = null, EventHandler<TextChangedEventArgs>? eventHandler = null)
        {
            var fastTextBox = new FastColoredTextBox();
            fastTextBox.Dock = DockStyle.Fill;
            fastTextBox.Font = new Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            //fastTextBox.MaxLength = 524288;
            fastTextBox.Multiline = true;
            fastTextBox.Name = name;
            if (tabIndex != null) fastTextBox.TabIndex = tabIndex.Value;
            fastTextBox.WordWrap = false;
            fastTextBox.BorderStyle = BorderStyle.FixedSingle;
            fastTextBox.ShowLineNumbers = false;
            fastTextBox.ReadOnly = eventHandler == null;
            //fastTextBox.AllowSeveralTextStyleDrawing = true;
            if (eventHandler != null) fastTextBox.TextChanged += eventHandler;
            return fastTextBox;
        }

        public async Task FindAndReplace()
        {
            //Debug.WriteLine($"{inputTextbox.Height}...{inputTextbox.CharHeight}...{inputTextbox.TextHeight}...{inputTextbox.LinesCount}");
            await tokenSource.CancelAsync();
            tokenSource.Dispose();
            tokenSource = new CancellationTokenSource();
            if (inputTextbox.Text == string.Empty || findTextbox.Text == string.Empty)
            {
                prevBut.Enabled = nextBut.Enabled = matchNavLabel.Visible = false;
                Reset();
                return;
            }

            if (delayTokenSource == null)
            {
                delayTokenSource = new CancellationTokenSource();
                _ = ShowDelay(delayTokenSource.Token);
            }

            try
            {
                var result = await regexer.AutoRegex(inputTextbox.Text, findTextbox.Text, replaceTextbox.Text, tokenSource.Token);
                if (result.Output == "Cancelled") return;

                var scrollPositionX = outputTextbox.HorizontalScroll.Value;
                var scrollPositionY = outputTextbox.VerticalScroll.Value;
                Reset();
                outputTextbox.Text = result.Output;
                outputTextbox.HorizontalScroll.Value = scrollPositionX;
                outputTextbox.VerticalScroll.Value = scrollPositionY;
                outputTextbox.UpdateScrollbars();

                if (result.Matches != null)
                {
                    inputMatchesTextbox.Text = string.Join('\n', result.Matches.Select(m => m.InputMatch.Text));
                    outputMatchesTextbox.Text = string.Join('\n', result.Matches.Select(m => m.OutputMatch.Text));

                    var lastInputMatchEndIndex = 0;
                    var lastOutputMatchEndIndex = 0;
                    var alternate = false;
                    foreach (var matchPair in result.Matches)
                    {
                        var matchRange = inputTextbox.GetRange(matchPair.InputMatch.Index, matchPair.InputMatch.Index + matchPair.InputMatch.Length);
                        (Range inpRange, Range? outRange) pairRanges = (matchRange, null);
                        matchRange.SetStyle(FctbManager.BaseStyleIndex);
                        alternate = !alternate;
                        var inputMatchRange = inputMatchesTextbox.GetRange(lastInputMatchEndIndex, lastInputMatchEndIndex += matchPair.InputMatch.Length + 2);
                        inputMatchLineBackgroundData.AddRange(Enumerable.Repeat(alternate, inputMatchRange.End.iLine - inputMatchRange.Start.iLine));

                        matchRange = outputTextbox.GetRange(matchPair.OutputMatch.Index, matchPair.OutputMatch.Index + matchPair.OutputMatch.Length);
                        matchRange.SetStyle(FctbManager.BaseStyleIndex);
                        pairRanges.outRange = matchRange;
                        var outputMatchRange = outputMatchesTextbox.GetRange(lastOutputMatchEndIndex, lastOutputMatchEndIndex += matchPair.OutputMatch.Length + 2);
                        outputMatchLineBackgroundData.AddRange(Enumerable.Repeat(alternate, outputMatchRange.End.iLine - outputMatchRange.Start.iLine));

                        matchRanges.Add(pairRanges);
                    }
                    inputMatchLineBackgroundData.Add(alternate);
                    outputMatchLineBackgroundData.Add(alternate);
                    fctbManager.StyleMatches(inputTextbox, outputTextbox, result);
                    fctbManager.PopulateSubMatches(inputDataGridView, outputDataGridView, result, limitSubmatchesToolStripCheckBox.Checked);
                }
                if (matchRanges.Count > 0)
                {
                    currentMatchRange = -1;
                    prevBut.Enabled = nextBut.Enabled = matchNavLabel.Visible = true;
                    matchNavLabel.Text = $"{matchRanges.Count} matches";
                }
                else
                {
                    prevBut.Enabled = nextBut.Enabled = matchNavLabel.Visible = false;
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                outputTextbox.Text = ex.ToString();
                outputTextbox.Range.SetStyle(FctbManager.ErrorStyleIndex);
            }
            catch (RegexParseException ex)
            {
                outputTextbox.Text = ex.ToString();
                outputTextbox.Range.SetStyle(FctbManager.ErrorStyleIndex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                outputTextbox.Text = ex.ToString();
                outputTextbox.Range.SetStyle(FctbManager.ErrorStyleIndex);
            }
            catch (ArithmeticException ex)
            {
                outputTextbox.Text = ex.ToString();
                outputTextbox.Range.SetStyle(FctbManager.ErrorStyleIndex);
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
            findTextbox.Text = template[0];
            replaceTextbox.Text = template[1];
        }

        private void ClearMatchDataGrids()
        {
            inputDataGridView.Rows.Clear();
            inputDataGridView.Columns.Clear();
            outputDataGridView.Rows.Clear();
            outputDataGridView.Columns.Clear();
        }

        private void Reset()
        {
            outputTextbox.Text = inputMatchesTextbox.Text = outputMatchesTextbox.Text = string.Empty;
            matchRanges.Clear();
            inputMatchLineBackgroundData.Clear();
            outputMatchLineBackgroundData.Clear();
            fctbManager.ClearMatchStyles(inputTextbox, outputTextbox);
            ClearMatchDataGrids();
        }

        private async void inputTextbox_TextChanged(object? sender, EventArgs e)
        {
            await FindAndReplace();
        }

        private async void findTextbox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            fctbManager.HighlightAndSuggest(findTextbox, false);
            saveTemplateBut.Enabled = findTextbox.Text != string.Empty;
            await FindAndReplace();
        }

        private async void replaceTextbox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            fctbManager.HighlightAndSuggest(replaceTextbox, true);
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
            var saveTemplateForm = new SaveTemplateForm(templatesComboBox.Items, templatesComboBox.Text, findTextbox.Text, replaceTextbox.Text);
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
            if (currentMatchRange < 0) currentMatchRange = matchRanges.Count - 1;
            inputTextbox.DoRangeVisible(matchRanges[currentMatchRange].inpRange);
            if (matchRanges[currentMatchRange].outRange != null) outputTextbox.DoRangeVisible(matchRanges[currentMatchRange].outRange);
            UpdateMatchNavLabel();
        }

        private void nextBut_Click(object sender, EventArgs e)
        {
            currentMatchRange++;
            if (currentMatchRange == matchRanges.Count) currentMatchRange = 0;
            inputTextbox.DoRangeVisible(matchRanges[currentMatchRange].inpRange);
            if (matchRanges[currentMatchRange].outRange != null) outputTextbox.DoRangeVisible(matchRanges[currentMatchRange].outRange);
            UpdateMatchNavLabel();
        }

        private void UpdateMatchNavLabel()
        {
            matchNavLabel.Text = $"{currentMatchRange + 1} of {matchRanges.Count} matches";
        }

        private void wordWrapToolStripCheckBox_Click(object sender, EventArgs e)
        {
            inputTextbox.WordWrap = wordWrapToolStripCheckBox.Checked;
            outputTextbox.WordWrap = wordWrapToolStripCheckBox.Checked;
            inputMatchesTextbox.WordWrap = wordWrapToolStripCheckBox.Checked;
            outputMatchesTextbox.WordWrap = wordWrapToolStripCheckBox.Checked;
        }

        private void lineNumbersToolStripCheckbox_Click(object sender, EventArgs e)
        {
            inputTextbox.ShowLineNumbers = lineNumbersToolStripCheckbox.Checked;
            outputTextbox.ShowLineNumbers = lineNumbersToolStripCheckbox.Checked;
            inputMatchesTextbox.ShowLineNumbers = lineNumbersToolStripCheckbox.Checked;
            outputMatchesTextbox.ShowLineNumbers = lineNumbersToolStripCheckbox.Checked;
        }

        private async void limitSubmatchesToolStripCheckBox_Click(object sender, EventArgs e)
        {
            if (!limitSubmatchesToolStripCheckBox.Checked) await FindAndReplace();
        }

        private void InputMatchesTextbox_PaintLine(object? sender, PaintLineEventArgs e)
        {
            e.Graphics.FillRectangle(e.LineIndex >= inputMatchLineBackgroundData.Count ? FctbManager.TransparentBrush :
                inputMatchLineBackgroundData[e.LineIndex] ? FctbManager.BaseBrush : FctbManager.BaseBrushLight, e.LineRect);
        }

        private void OutputMatchesTextbox_PaintLine(object? sender, PaintLineEventArgs e)
        {
            e.Graphics.FillRectangle(e.LineIndex >= outputMatchLineBackgroundData.Count ? FctbManager.TransparentBrush :
                outputMatchLineBackgroundData[e.LineIndex] ? FctbManager.BaseBrush : FctbManager.BaseBrushLight, e.LineRect);
        }

        private void InputTextbox_SizeChanged(object? sender, EventArgs e)
        {
            inputScroll.MaximumX = int.Max(inputTextbox.AutoScrollMinSize.Width - inputTextbox.ClientSize.Width, 0);
            inputScroll.MaximumY = int.Max(inputTextbox.AutoScrollMinSize.Height - inputTextbox.ClientSize.Height, 0);
        }

        private void OutputTextbox_SizeChanged(object? sender, EventArgs e)
        {
            outputScroll.MaximumX = int.Max(outputTextbox.AutoScrollMinSize.Width - outputTextbox.ClientSize.Width, 0);
            outputScroll.MaximumY = int.Max(outputTextbox.AutoScrollMinSize.Height - outputTextbox.ClientSize.Height, 0);
        }

        private void InputTextbox_Scroll(object? sender, ScrollEventArgs e)
        {
            //Debug.WriteLine($"{e.OldValue}... {e.NewValue}... {inputTextbox.HorizontalScroll.Value}... {inputTextbox.Width}... {inputTextbox.Size.Width}... {outputTextbox.AutoScrollMinSize.Width}... {inputTextbox.ClientSize.Width}");
            if (!syncScrollToolStripCheckBox.Checked) return;
            var ratioX = inputTextbox.HorizontalScroll.Value / inputScroll.MaximumX;
            var ratioY = inputTextbox.VerticalScroll.Value / inputScroll.MaximumY;
            outputTextbox.HorizontalScroll.Value = (int)(ratioX * outputScroll.MaximumX);
            outputTextbox.VerticalScroll.Value = (int)(ratioY * outputScroll.MaximumY);
            outputTextbox.Invalidate();
        }

        private void OutputTextbox_Scroll(object? sender, ScrollEventArgs e)
        {
            if (!syncScrollToolStripCheckBox.Checked) return;
            var ratioX = outputTextbox.HorizontalScroll.Value / outputScroll.MaximumX;
            var ratioY = outputTextbox.VerticalScroll.Value / outputScroll.MaximumY;
            inputTextbox.HorizontalScroll.Value = (int)(ratioX * inputScroll.MaximumX);
            inputTextbox.VerticalScroll.Value = (int)(ratioY * inputScroll.MaximumY);
            inputTextbox.Invalidate();
        }

        private void InputMatchesTextbox_SizeChanged(object? sender, EventArgs e)
        {
            inputScroll.MatchesMaximumX = int.Max(inputMatchesTextbox.AutoScrollMinSize.Width - inputMatchesTextbox.ClientSize.Width, 0);
            inputScroll.MatchesMaximumY = int.Max(inputMatchesTextbox.AutoScrollMinSize.Height - inputMatchesTextbox.ClientSize.Height, 0);
        }

        private void OutputMatchesTextbox_SizeChanged(object? sender, EventArgs e)
        {
            outputScroll.MatchesMaximumX = int.Max(outputMatchesTextbox.AutoScrollMinSize.Width - outputMatchesTextbox.ClientSize.Width, 0);
            outputScroll.MatchesMaximumY = int.Max(outputMatchesTextbox.AutoScrollMinSize.Height - outputMatchesTextbox.ClientSize.Height, 0);
        }

        private void InputMatchesTextbox_Scroll(object? sender, ScrollEventArgs e)
        {
            //Debug.WriteLine($"{e.OldValue}... {e.NewValue}... {inputMatchesTextbox.VerticalScroll.Value}... {inputMatchesTextbox.Height}... {inputMatchesTextbox.Size.Height}");
            //Debug.WriteLine($"{inputMatchesTextbox.Height}...{inputMatchesTextbox.CharHeight}...{inputMatchesTextbox.TextHeight}...{inputMatchesTextbox.LinesCount}");
            if (!syncScrollToolStripCheckBox.Checked) return;
            var ratioX = inputMatchesTextbox.HorizontalScroll.Value / inputScroll.MatchesMaximumX;
            var ratioY = inputMatchesTextbox.VerticalScroll.Value / inputScroll.MatchesMaximumY;
            outputMatchesTextbox.HorizontalScroll.Value = (int)(ratioX * outputScroll.MatchesMaximumX);
            outputMatchesTextbox.VerticalScroll.Value = (int)(ratioY * outputScroll.MatchesMaximumY);
            outputMatchesTextbox.Invalidate();
        }

        private void OutputMatchesTextbox_Scroll(object? sender, ScrollEventArgs e)
        {
            if (!syncScrollToolStripCheckBox.Checked) return;
            var ratioX = outputMatchesTextbox.HorizontalScroll.Value / outputScroll.MatchesMaximumX;
            var ratioY = outputMatchesTextbox.VerticalScroll.Value / outputScroll.MatchesMaximumY;
            inputMatchesTextbox.HorizontalScroll.Value = (int)(ratioX * inputScroll.MatchesMaximumX);
            inputMatchesTextbox.VerticalScroll.Value = (int)(ratioY * inputScroll.MatchesMaximumY);
            inputMatchesTextbox.Invalidate();
        }

        private void inputTabs_Selected(object sender, TabControlEventArgs e)
        {
            if (syncTabsToolStripCheckBox.Checked) outputTabs.SelectTab(e.TabPageIndex);
        }

        private void outputTabs_Selected(object sender, TabControlEventArgs e)
        {
            if (syncTabsToolStripCheckBox.Checked) inputTabs.SelectTab(e.TabPageIndex);
        }

        private void IndieMatchesDataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            //Code initially generated by ChatGPT
            var dataGridView = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var cell = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.Value is not FctbManager.CellMatchData cellMatchData)
                return;

            e.Handled = true;
            e.PaintBackground(e.CellBounds, true);

            var font = e.CellStyle.Font;
            var labelFont = new Font(font, FontStyle.Italic);

            var neededHeight = MeasureCellHeight(e.Graphics, cellMatchData.MatchDataCaptures, font, labelFont, padding: 4);

            if (dataGridView.Rows[e.RowIndex].Height < neededHeight)
                dataGridView.Rows[e.RowIndex].Height = neededHeight;

            float y = e.CellBounds.Top + 4;
            float x = e.CellBounds.Left + 4;

            foreach (var line in cellMatchData.MatchDataCaptures)
            {
                if (line is { Index: 0, Length: 0 }) continue;
                // Draw main text
                var mainSize = e.Graphics.MeasureString(line.Text, font);
                e.Graphics.DrawString(line.Text, font, cellMatchData.TextBrush, new RectangleF(x, y, e.CellBounds.Right - x - 4, font.Height), _ellipsisFormat);

                // Draw position
                var positionText = FctbManager.CellMatchData.PositionString(line);
                e.Graphics.DrawString(positionText, labelFont, FctbManager.MatchPositionBrush, new RectangleF(x + mainSize.Width, y, e.CellBounds.Right - x - 4 - mainSize.Width, font.Height), _ellipsisFormat);

                y += Math.Max(mainSize.Height, labelFont.Height) + 2;
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            return;

            static int MeasureCellHeight(Graphics g, List<MatchData> lines, Font mainFont, Font labelFont, int padding)
            {
                float height = padding;

                foreach (var line in lines)
                {
                    if (line is { Index: 0, Length: 0 }) continue;
                    var mainSize = g.MeasureString(line.Text, mainFont);
                    var labelSize = g.MeasureString(FctbManager.CellMatchData.PositionString(line), labelFont);

                    height += Math.Max(mainSize.Height, labelSize.Height) + 2;
                }

                height += padding;
                return (int)Math.Ceiling(height);
            }
        }

        private void IndieMatchesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            ((DataGridView)sender).ClearSelection();
        }

        private void inputDataGridView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            var totalColumnsWidth = inputDataGridView.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            var rowHeadersWidth = inputDataGridView.RowHeadersVisible ? inputDataGridView.RowHeadersWidth : 0;
            var usableClientWidth = inputDataGridView.ClientSize.Width - rowHeadersWidth;
            inputScroll.IndieMaximumX = Math.Max(0, totalColumnsWidth - usableClientWidth);
        }

        private void outputDataGridView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            var totalColumnsWidth = outputDataGridView.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            var rowHeadersWidth = outputDataGridView.RowHeadersVisible ? outputDataGridView.RowHeadersWidth : 0;
            var usableClientWidth = outputDataGridView.ClientSize.Width - rowHeadersWidth;
            outputScroll.IndieMaximumX = Math.Max(0, totalColumnsWidth - usableClientWidth);
        }

        private void inputDataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            //Debug.WriteLine($"{e.OldValue}... {e.NewValue}... {inputDataGridView.HorizontalScrollingOffset}... {inputDataGridView.VerticalScrollingOffset}... {inputDataGridView.Width}...");
            var isHorizontal = e.ScrollOrientation == ScrollOrientation.HorizontalScroll;
            if (indieMatchIsScrolling || !syncScrollToolStripCheckBox.Checked || (isHorizontal && inputScroll.IndieMaximumX < 0)) return;
            indieMatchIsScrolling = true;
            if (isHorizontal)
            {
                var ratio = inputDataGridView.HorizontalScrollingOffset / inputScroll.IndieMaximumX;
                outputDataGridView.HorizontalScrollingOffset = (int)(ratio * outputScroll.IndieMaximumX);
            }
            else outputDataGridView.FirstDisplayedScrollingRowIndex = inputDataGridView.FirstDisplayedScrollingRowIndex;
            indieMatchIsScrolling = false;
        }

        private void outputDataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            var isHorizontal = e.ScrollOrientation == ScrollOrientation.HorizontalScroll;
            if (indieMatchIsScrolling || !syncScrollToolStripCheckBox.Checked || (isHorizontal && outputScroll.IndieMaximumX < 0)) return;
            indieMatchIsScrolling = true;
            //Debug.WriteLine($"{e.OldValue}... {e.NewValue}... {outputDataGridView.HorizontalScrollingOffset}... {outputDataGridView.VerticalScrollingOffset}... {outputDataGridView.Width}...");
            if (isHorizontal)
            {
                var ratio = outputDataGridView.HorizontalScrollingOffset / outputScroll.IndieMaximumX;
                inputDataGridView.HorizontalScrollingOffset = (int)(ratio * inputScroll.IndieMaximumX);
            }
            else inputDataGridView.FirstDisplayedScrollingRowIndex = outputDataGridView.FirstDisplayedScrollingRowIndex;
            indieMatchIsScrolling = false;
        }

        private void DataGridView_MouseWheel(DataGridView dataGridView, bool isOutput, MouseEventArgs args)
        {
            if ((ModifierKeys & Keys.Shift) != Keys.Shift) return;

            ((HandledMouseEventArgs)args).Handled = true;

            var scrollAmount = SystemInformation.MouseWheelScrollLines * 15;
            var newValue = dataGridView.HorizontalScrollingOffset - Math.Sign(args.Delta) * scrollAmount;
            var newScrollOffset = int.Clamp(newValue, 0, isOutput ? outputScroll.IndieMaximumX : inputScroll.IndieMaximumX);
            var scrollEvent = new ScrollEventArgs(args.Delta < 0 ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement,
                dataGridView.HorizontalScrollingOffset, newScrollOffset, ScrollOrientation.HorizontalScroll);
            dataGridView.HorizontalScrollingOffset = newScrollOffset;

            if (isOutput) outputDataGridView_Scroll(dataGridView, scrollEvent);
            else inputDataGridView_Scroll(dataGridView, scrollEvent);
        }

        private void clearPatternsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            findTextbox.Text = string.Empty;
            replaceTextbox.Text = string.Empty;
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            inputTextbox.Text = string.Empty;
            findTextbox.Text = string.Empty;
            replaceTextbox.Text = string.Empty;
        }

        private async void exactWhitespaceCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            regexer.SetExactWhiteSpace(exactWhitespaceToolStripCheckbox.Checked);
            await FindAndReplace();
        }

        private void suggestOnCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (suggestOnCommandToolStripMenuItem.Checked) return;
            suggestOnCommandToolStripMenuItem.Checked = true;
            fctbManager.SetSuggestionMode(true);
            alwaysSuggestToolStripMenuItem.Checked = false;
        }

        private void alwaysSuggestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (alwaysSuggestToolStripMenuItem.Checked) return;
            alwaysSuggestToolStripMenuItem.Checked = true;
            fctbManager.SetSuggestionMode(false);
            suggestOnCommandToolStripMenuItem.Checked = false;
        }

        private async void monoColouredToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (monoColouredToolStripMenuItem.Checked) return;
            monoColouredToolStripMenuItem.Checked = true;
            fctbManager.SetHighlightMode(true);
            multiColouredToolStripMenuItem.Checked = false;
            await FindAndReplace();
        }

        private async void multiColouredToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (multiColouredToolStripMenuItem.Checked) return;
            multiColouredToolStripMenuItem.Checked = true;
            fctbManager.SetHighlightMode(false);
            monoColouredToolStripMenuItem.Checked = false;
            await FindAndReplace();
        }

        private void inputDataGridView_RowHeadersWidthChanged(object sender, EventArgs e) => inputDataGridView_ColumnWidthChanged(inputDataGridView, null!);

        private void outputDataGridView_RowHeadersWidthChanged(object sender, EventArgs e) => outputDataGridView_ColumnWidthChanged(outputDataGridView, null!);
    }

    internal struct ScrollValues
    {
        public double MaximumX { get; set; }
        public double MaximumY { get; set; }
        public double MatchesMaximumX { get; set; }
        public double MatchesMaximumY { get; set; }
        public double IndieMaximumX { get; set; }
    }
}
