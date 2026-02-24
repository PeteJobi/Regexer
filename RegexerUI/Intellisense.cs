using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FastColoredTextBoxNS;
using Range = FastColoredTextBoxNS.Range;

namespace RegexerUI
{
    public class Intellisense: IEnumerable<AutocompleteItem>
    {
        private readonly bool _isReplace;
        private readonly FastColoredTextBox _textBox;
        private readonly AutocompleteMenu _autocompleteMenu;
        private bool _manuallyTriggered;
        private IEnumerable<string> _typedLabels;
        private readonly Func<IEnumerable<string>> _getTypedLabels;
        private bool _shouldRefreshLabelSuggestions;

        public Intellisense(FastColoredTextBox textBox, AutocompleteMenu autocompleteMenu, bool isReplace, Func<IEnumerable<string>> getTypedLabels)
        {
            _textBox = textBox;
            _isReplace = isReplace;
            _autocompleteMenu = autocompleteMenu;
            _getTypedLabels = getTypedLabels;
            textBox.KeyDown += (sender, args) =>
            {
                if (args is not { Control: true, KeyCode: Keys.Space }) return; //If CTRL+SPACE is not pressed, return
                _manuallyTriggered = true;
                _autocompleteMenu.Show(true);
                args.Handled = true;
            };
            textBox.LostFocus += (sender, args) => _shouldRefreshLabelSuggestions = true;
            _shouldRefreshLabelSuggestions = true;
        }
        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            var fragmentBeforeCursor = _autocompleteMenu.Fragment.GetIntersectionWith(new Range(_textBox, _autocompleteMenu.Fragment.Start, _textBox.Selection.End));
            var tagStart = GetUnclosedTagIndex(fragmentBeforeCursor.Text);
            if (tagStart == -1) yield break; //No unclosed tag "[[" around cursor, no suggestions

            var unclosedTagFragment = fragmentBeforeCursor.GetIntersectionWith(new Range(_textBox, new Place(tagStart, fragmentBeforeCursor.Start.iLine), fragmentBeforeCursor.End));
            //Debug.WriteLine(unclosedTagFragment.Text, fragmentBeforeCursor.Text);
            var structure = _isReplace ? IntellisenseStructure.ReplaceStructure : IntellisenseStructure.PatternStructure;
            var suggestionItems = GetSuggestionItems(structure, unclosedTagFragment.Text, string.Empty);
            if(!suggestionItems.Suggestions.Any()) yield break;
            _autocompleteMenu.Fragment.Start = _textBox.Selection.Start;
            _autocompleteMenu.Fragment.End = _textBox.Selection.End;
            if (_manuallyTriggered) _manuallyTriggered = false;
            else if (suggestionItems.DontShow)
            {
                _manuallyTriggered = false;
                yield break; //If the suggestionItems are 'DontShow', the suggestions won't show unless the user manually triggers it (by pressing CTRL+SPACE)
            }

            var closeTagAlreadyExists = false; //Prevent duplicate 'CloseTag' suggestions
            foreach (var suggestionItem in suggestionItems.Suggestions)
            {
                if (suggestionItem.Suggestion == "]]")
                {
                    if (closeTagAlreadyExists) continue;
                    closeTagAlreadyExists = true;
                }
                yield return new IntellisenseItem(suggestionItem.Suggestion, suggestionItem.TextToInsert, suggestionItem.TipTitle, suggestionItem.TipText);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int GetUnclosedTagIndex(string text)
        {
            var tagStack = new Stack<int>();
            var lastChar = '\0';
            for (var i = text.Length - 1; i >= 0; i--)
            {
                var curChar = text[i];
                switch (curChar)
                {
                    case '[' when lastChar == '[':
                    {
                        if (tagStack.Count == 0) return i;
                        tagStack.Pop();
                        break;
                    }
                    case ']' when lastChar == ']':
                        tagStack.Push(i);
                        break;
                }
                lastChar = curChar;
            }

            return -1;
        }

        private (List<SuggestionItem> Suggestions, bool DontShow) GetSuggestionItems(IntellisenseData? structure, string text, string regexPattern)
        {
            if (structure == null) return (new List<SuggestionItem>(), false);
            var match = Regex.Match(text, regexPattern + structure.Match + "$");
            var allSuggestions = new List<SuggestionItem>();
            if (match.Success)
            {
                allSuggestions.AddRange(structure.SuggestionItems.Where(s => s.ShouldHide == null || !s.ShouldHide(match.Groups)));
            }

            var dontShowStartIndex = structure.DontShow ? 0 : allSuggestions.Count; //dontShowStartIndex allows for putting all false-value 'DontShow' suggestions below the true-value ones
            foreach (var suggestionItem in structure.SuggestionItems)
            {
                if (suggestionItem.Suggestion == IntellisenseStructure.LabelSuggestions && match.Success)
                {
                    var labelSuggestions = GetLabelSuggestion(match.Groups["name"].Value);
                    allSuggestions.InsertRange(dontShowStartIndex, labelSuggestions);
                    dontShowStartIndex += labelSuggestions.Count;
                    continue;
                }
                var res = GetSuggestionItems(suggestionItem.IntellisenseData, text, regexPattern + structure.Match);
                if(res.DontShow) allSuggestions.AddRange(res.Suggestions);
                else
                {
                    allSuggestions.InsertRange(dontShowStartIndex, res.Suggestions);
                    dontShowStartIndex += res.Suggestions.Count;
                }
            }
            return (allSuggestions, dontShowStartIndex == 0);
        }

        private List<SuggestionItem> GetLabelSuggestion(string typedLabel)
        {
            if(_shouldRefreshLabelSuggestions)
            {
                _typedLabels = _getTypedLabels();
                _shouldRefreshLabelSuggestions = false;
            }
            return _typedLabels.Where(l => l != typedLabel && l.StartsWith(typedLabel, StringComparison.OrdinalIgnoreCase))
                .Select(l => new SuggestionItem(l, l[typedLabel.Length..], string.Empty, string.Empty)).ToList();
        }
    }

    public class IntellisenseItem: AutocompleteItem
    {
        public IntellisenseItem(string menuText, string textToInsert) : base(textToInsert, -1, menuText){}
        public IntellisenseItem(string menuText, string textToInsert, string tipTitle, string tipText) : base(textToInsert, -1, menuText, tipTitle, tipText){}

        public override CompareResult Compare(string fragmentText)
        {
            return CompareResult.Visible;
        }
    }

    public class IntellisenseData
    {
        public IntellisenseData(string match, bool dontShow = false)
        {
            Match = match;
            DontShow = dontShow;
        }

        public string Match { get; set; }
        public SuggestionItem[] SuggestionItems { get; set; }
        public bool DontShow { get; set; } //These suggestions will not be shown automatically. User will have to trigger them manually by pressing Ctrl+Space

        public override string ToString()
        {
            return Match;
        }
    }

    public class SuggestionItem
    {
        public SuggestionItem(string suggestion, string textToInsert, string tipTitle, string tipText)
        {
            Suggestion = suggestion;
            TextToInsert = textToInsert;
            TipTitle = tipTitle;
            TipText = tipText;
        }

        public string Suggestion { get; set; }
        public string TextToInsert { get; set; }
        public string TipTitle { get; set; }
        public string TipText { get; set; }
        public IntellisenseData? IntellisenseData { get; set; }
        public Func<GroupCollection, bool>? ShouldHide { get; set; }

        public override string ToString()
        {
            return Suggestion;
        }
    }
}
