using FastColoredTextBoxNS;
using RegexerUIV2.Properties;
using System.Collections;
using static RegexerV2.SyntaxStructure;

namespace RegexerUIV2
{
    public class Intellisense: IEnumerable<AutocompleteItem>
    {
        private readonly bool _isReplace;
        private readonly FastColoredTextBox _textBox;
        private readonly AutocompleteMenu _autocompleteMenu;
        private bool _manuallyTriggered;
        private IEnumerable<string> _typedLabels;
        private readonly Func<IEnumerable<string>> _getTypedLabels;
        private readonly Func<List<SuggestionToken>> _getSuggestionTokens;
        private readonly Func<bool> _showOnCommand;
        private bool _shouldRefreshLabelSuggestions;

        public Intellisense(FastColoredTextBox textBox, AutocompleteMenu autocompleteMenu, bool isReplace,
            Func<IEnumerable<string>> getTypedLabels, Func<List<SuggestionToken>> getSuggestionTokens, Func<bool> showOnCommand)
        {
            _textBox = textBox;
            _isReplace = isReplace;
            _autocompleteMenu = autocompleteMenu;
            _getTypedLabels = getTypedLabels;
            _getSuggestionTokens = getSuggestionTokens;
            _showOnCommand = showOnCommand;
            textBox.KeyDown += (sender, args) =>
            {
                if (args is not { Control: true, KeyCode: Keys.Space }) return; //If CTRL+SPACE is not pressed, return
                _manuallyTriggered = true;
                _autocompleteMenu.Show(true);
                args.Handled = true;
            };
            textBox.LostFocus += (sender, args) => _shouldRefreshLabelSuggestions = true;
            _shouldRefreshLabelSuggestions = true;
            autocompleteMenu.Selecting += (sender, args) =>
            {
                var intellisenseItem = (IntellisenseItem)args.Item;
                _autocompleteMenu.Fragment.Start = intellisenseItem.TextInsertionIndex != null 
                    ? textBox.GetRange(intellisenseItem.TextInsertionIndex.Value, intellisenseItem.TextInsertionIndex.Value).Start 
                    : _textBox.Selection.Start;
                _autocompleteMenu.Fragment.End = _textBox.Selection.End;
            };
        }

        public IEnumerator<AutocompleteItem> GetEnumerator()
        {
            if (_manuallyTriggered) _manuallyTriggered = false;
            else if (_showOnCommand())
            {
                _manuallyTriggered = false;
                yield break; //If the suggestionItems are only to be shown on user command, the suggestions won't show unless the user manually triggers it (by pressing CTRL+SPACE)
            }
            var suggestionItems = GetSuggestionItems();
            if(suggestionItems.Count == 0) yield break;

            foreach (var suggestionItem in suggestionItems)
            {
                yield return new IntellisenseItem(suggestionItem.Suggestion, suggestionItem.TextToInsert, suggestionItem.TextInsertionIndex, suggestionItem.TipTitle, suggestionItem.TipText);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private readonly FreeText dummyLabelStructure = new(FreeTextType.LABEL);
        private List<SuggestionItem> GetSuggestionItems()
        {
            var suggestionTokens = _getSuggestionTokens();
            var allSuggestions = new List<SuggestionItem>();
            var alreadyAdded = new HashSet<(TokenName, Structure)>();

            foreach (var suggestionToken in suggestionTokens)
            {
                switch (suggestionToken.Suggestion)
                {
                    case FreeText freeTextToken:
                        switch (freeTextToken.Type)
                        {
                            case FreeTextType.LABEL:
                                if (AlreadyAddedP(dummyLabelStructure)) break;
                                allSuggestions.Add(new SuggestionItem(TypeTextHere("match label"), string.Empty));
                                if (!_isReplace) break;
                                var labelSuggestions = GetLabelSuggestion(suggestionToken.LabelFreeTextEnteredSoFar, suggestionToken.PlacementIndex);
                                allSuggestions.AddRange(labelSuggestions);
                                break;
                            case FreeTextType.DIGITS:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], freeTextToken)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.ExactAmountQuantifier:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("exact or minimum amount"), string.Empty));
                                        break;
                                    case TokenName.MaxAmountQuantifier:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("maximum amount"), string.Empty));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                            case FreeTextType.PLAIN_TEXT:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], freeTextToken)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.Pattern:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("regex"), string.Empty));
                                        break;
                                    case TokenName.MatchMultiple:
                                    case TokenName.Duplicate:
                                    case TokenName.DuplicateSeparator:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("separator"), string.Empty));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                            case FreeTextType.EXPRESSION_WITH_IM:
                                allSuggestions.Add(new SuggestionItem(TypeTextHere("new number/expression"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("m <Original match value>", "m", GetResource("MatchValueTitle"), GetResource("MatchValueText")));
                                allSuggestions.Add(new SuggestionItem("i <Index of match>", "i", GetResource("MatchIndexTitle"), GetResource("MatchIndexText")));
                                allSuggestions.Add(new SuggestionItem("+", "+", GetResource("AdditionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("-", "-", GetResource("SubtractionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("*", "*", GetResource("MultiplicationText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("/", "/", GetResource("DivisionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("%", "%", GetResource("ModuloText"), string.Empty));
                                break;
                            case FreeTextType.EXPRESSION_WITH_I:
                                allSuggestions.Add(new SuggestionItem(TypeTextHere("duplication amount/expression"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("i <Index of match>", "i", GetResource("MatchIndexTitle"), GetResource("MatchIndexText")));
                                allSuggestions.Add(new SuggestionItem("+", "+", GetResource("AdditionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("-", "-", GetResource("SubtractionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("*", "*", GetResource("MultiplicationText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("/", "/", GetResource("DivisionText"), string.Empty));
                                allSuggestions.Add(new SuggestionItem("%", "%", GetResource("ModuloText"), string.Empty));
                                break;
                            case FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], freeTextToken)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.Unordered:
                                    case TokenName.MatchMultiple:
                                    case TokenName.MatchMultipleMatch:
                                    case TokenName.MultiLineMatch:
                                    case TokenName.Optional:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere(_isReplace ? "replacement text" : "line/phrase to match"), string.Empty));
                                        break;
                                    case TokenName.MatchMultipleReplace:
                                    case TokenName.MultiLineReplace:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("replacement text"), string.Empty));
                                        break;
                                    case TokenName.MultiLine:
                                        //This is 'replace' version of multiline (see MultiLineReplace). No need to suggest that single line text can appear before the multiline
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                        }
                        break;
                    case Whitespace whitespace:
                        switch (whitespace.Type)
                        {
                            case WhitespaceType.END_OF_LINE:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], whitespace)) break;
                                allSuggestions.Add(new SuggestionItem("(Enter new line)", "\r\n", string.Empty, string.Empty));
                                break;
                            //default:
                            //    throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case Symbol symbol:
                        switch (symbol.SuggestionName ?? symbol.Name)
                        {
                            case TokenName.Opener:
                                //Don't suggest opener
                                break;
                            case TokenName.Closer:
                                if(AlreadyAddedP(symbol)) break;
                                allSuggestions.Add(new SuggestionItem(symbol.Value, symbol.Value, GetResource("CloseTagTitle"), GetResource("CloseTagText")){ TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case TokenName.DemarcateAfterLabel:
                                if(AlreadyAddedP(symbol)) break;
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("| <Replace modifiers>", symbol.Value, GetResource("ReplaceModifierTitle"), GetResource("ReplaceModifierText"))
                                    : new SuggestionItem("| <Modifiers>", symbol.Value, GetResource("ModifierTitle"), GetResource("ModifierText")));
                                break;
                            case TokenName.Demarcate:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], symbol)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.Unordered:
                                        allSuggestions.Add(_isReplace
                                            ? new SuggestionItem("| <Replace text>", symbol.Value, GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                                            : new SuggestionItem("| <Match>", symbol.Value, GetResource("UnorderedMatchTitle"), GetResource("UnorderedMatchText")));
                                        break;
                                    case TokenName.MatchMultiple:
                                        allSuggestions.Add(_isReplace
                                            ? new SuggestionItem("| <Separator>", symbol.Value, GetResource("ReplaceMultiSeparatorTitle"), GetResource("ReplaceMultiSeparatorText"))
                                            : new SuggestionItem("|{ <Separator> }", "|{", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText")));
                                        break;
                                    case TokenName.Optional:
                                        allSuggestions.Add(_isReplace
                                            ? new SuggestionItem("| <Replace text>", symbol.Value, GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                                            : new SuggestionItem("| <Optional match>", symbol.Value, GetResource("OptionalMatchTitle"), GetResource("OptionalMatchText")));
                                        break;
                                    case TokenName.Duplicate:
                                        allSuggestions.Add(new SuggestionItem("| <Duplication amount> | <Separator>", "|", GetResource("DuplicationSeparatorTitle"), GetResource("DuplicationSeparatorText")));
                                        break;
                                    case TokenName.DuplicateSeparator:
                                        allSuggestions.Add(new SuggestionItem("| <Separator>", symbol.Value, GetResource("DuplicationSeparatorTitle"), GetResource("DuplicationSeparatorText")));
                                        break;
                                    case TokenName.Capitalize:
                                        allSuggestions.Add(new SuggestionItem("| <Capitalization type>", symbol.Value, GetResource("CapitalizationTitle"), GetResource("CapitalizationText")));
                                        break;
                                    case TokenName.Evaluate:
                                        allSuggestions.Add(new SuggestionItem("| <Expression>", symbol.Value, GetResource("ReplaceNumberTitle"), GetResource("ReplaceNumberText")));
                                        break;
                                    case TokenName.MatchMultipleMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultipleMatchMatchTitle"), GetResource("MultipleMatchMatchText")));
                                        break;
                                    case TokenName.MultiLineMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultiLineMatchTitle"), GetResource("MultiLineMatchText")));
                                        break;
                                    case TokenName.MultiLine:
                                        allSuggestions.Add(new SuggestionItem("| <Quantifier>", symbol.Value, GetResource("QuantifierTitle"), GetResource("QuantifierText")));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                            case TokenName.Demarcate2:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], symbol)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.MatchMultiple:
                                        allSuggestions.Add(new SuggestionItem("| <Quantifier>", symbol.Value, GetResource("QuantifierTitle"), GetResource("QuantifierText")));
                                        break;
                                    case TokenName.ExactAmountQuantifier:
                                        allSuggestions.Add(new SuggestionItem("| <Exact amount>", symbol.Value, GetResource("ExactAmountQuantifierTitle"), GetResource("ExactAmountQuantifierText")));
                                        break;
                                    case TokenName.MatchMultipleReorder:
                                        allSuggestions.Add(new SuggestionItem("| <Reorder>", symbol.Value, GetResource("MatchMultipleReorderTitle"), GetResource("MatchMultipleReorderText")));
                                        break;
                                    case TokenName.MultiLineReplace:
                                        allSuggestions.Add(new SuggestionItem("| <Replace text>", symbol.Value, GetResource("ReplaceMultiLineTitle"), GetResource("ReplaceMultiLineText")));
                                        break;
                                    case TokenName.MatchMultipleMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultipleMatchMatchTitle"), GetResource("MultipleMatchMatchText")));
                                        break;
                                    case TokenName.MultiLineMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultiLineMatchTitle"), GetResource("MultiLineMatchText")));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                            case TokenName.Demarcate3:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], symbol)) break;
                                switch (suggestionToken.Ancestors[^1])
                                {
                                    case TokenName.MatchMultipleReplace:
                                        allSuggestions.Add(new SuggestionItem("| <Replace text>", symbol.Value, GetResource("ReplaceMultiTitle"), GetResource("ReplaceMultiText")));
                                        break;
                                    case TokenName.MultiLineReplace:
                                        allSuggestions.Add(new SuggestionItem("| <Replace text>", symbol.Value, GetResource("ReplaceMultiLineTitle"), GetResource("ReplaceMultiLineText")));
                                        break;
                                    case TokenName.MatchMultipleMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultipleMatchMatchTitle"), GetResource("MultipleMatchMatchText")));
                                        break;
                                    case TokenName.MultiLineMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", symbol.Value, GetResource("MultiLineMatchTitle"), GetResource("MultiLineMatchText")));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                            default:
                                if (AlreadyAdded(suggestionToken.Ancestors[^1], symbol)) break;
                                switch (symbol.Value)
                                {
                                    case "<":
                                        switch (suggestionToken.Ancestors[^1])
                                        {
                                            case TokenName.ExactAmountQuantifier:
                                                allSuggestions.Add(new SuggestionItem("<..> (Exact amount match)", symbol.Value, GetResource("ExactAmountQuantTitle"), GetResource("ExactAmountQuantText")));
                                                break;
                                            case TokenName.MatchMultipleSortByCapture:
                                                allSuggestions.Add(new SuggestionItem("< (Sort in descending order)", symbol.Value, GetResource("MatchMultipleSortDescTitle"), GetResource("MatchMultipleSortDescText")));
                                                break;
                                        }
                                        break;
                                    case "-":
                                        allSuggestions.Add(new SuggestionItem("- (Range separator)", symbol.Value, GetResource("ExactAmountDashTitle"), GetResource("ExactAmountDashText")));
                                        break;
                                    case ">":
                                        switch (suggestionToken.Ancestors[^1])
                                        {
                                            case TokenName.ExactAmountQuantifier:
                                                allSuggestions.Add(new SuggestionItem("> (Close range)", symbol.Value, GetResource("ExactAmountCloseTitle"), GetResource("ExactAmountCloseText")));
                                                break;
                                            case TokenName.MatchMultipleSortByCapture:
                                                allSuggestions.Add(new SuggestionItem("> (Sort in ascending order)", symbol.Value, GetResource("MatchMultipleSortAscTitle"), GetResource("MatchMultipleSortAscText")));
                                                break;
                                        }
                                        break;
                                    case "{":
                                        switch (suggestionToken.Ancestors[^1])
                                        {
                                            case TokenName.Pattern:
                                                allSuggestions.Add(new SuggestionItem("{ <Regex>", symbol.Value, GetResource("CustomRegexTitle"), GetResource("CustomRegexText")));
                                                break;
                                            case TokenName.MatchMultiple:
                                                allSuggestions.Add(new SuggestionItem("{ <Separator>", symbol.Value, GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText")));
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException();
                                        }
                                        break;
                                    case "}":
                                        switch (suggestionToken.Ancestors[^1])
                                        {
                                            case TokenName.Pattern:
                                                allSuggestions.Add(new SuggestionItem("} (Close regex)", symbol.Value, GetResource("CustomRegexTitle"), GetResource("CustomRegexText")));
                                                break;
                                            case TokenName.MatchMultiple:
                                                allSuggestions.Add(new SuggestionItem("} (Close separator)", symbol.Value, GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText")));
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException();
                                        }
                                        break;
                                    case "<>":
                                        allSuggestions.Add(new SuggestionItem("<> (Reverse order)", symbol.Value, GetResource("MatchMultipleReverseTitle"), GetResource("MatchMultipleReverseText")));
                                        break;
                                    case ":":
                                        allSuggestions.Add(new SuggestionItem(": <Sub-match label>", symbol.Value, GetResource("MatchMultipleSortByCaptureTitle"), GetResource("MatchMultipleSortByCaptureText")));
                                        break;
                                    case ",":
                                        allSuggestions.Add(new SuggestionItem(", (Sort again)", symbol.Value, GetResource("MatchMultipleSortAgainTitle"), GetResource("MatchMultipleSortAgainText")));
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                break;
                        }
                        break;
                    case Modifier modifier:
                        if (AlreadyAdded(suggestionToken.Ancestors[^1], modifier)) break;
                        switch (modifier.Value)
                        {
                            case "ml":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("ml <Reorder> | <Replace text>", modifier.Value, GetResource("MultiLineRepTitle"), GetResource("MultiLineRepText")) { TextInsertionIndex = suggestionToken.PlacementIndex }
                                    : new SuggestionItem("ml <Quantifier> | <Multiple match>", modifier.Value, GetResource("MultiLineTitle"), GetResource("MultiLineText")){ TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case "u":
                                if (_isReplace)
                                {
                                    switch (suggestionToken.Ancestors[^1])
                                    {
                                        case TokenName.Unordered:
                                            allSuggestions.Add(new SuggestionItem("u <Replace text>", "u|", GetResource("ReplaceTitle"), GetResource("ReplaceText")));
                                            allSuggestions.Add(new SuggestionItem("u (No replacement)", "u]]", GetResource("NoUReplaceTitle"), GetResource("NoUReplaceText")));
                                            break;
                                        case TokenName.Capitalize:
                                            allSuggestions.Add(new SuggestionItem("u (Uppercase)", "u]]", GetResource("UppercaseText"), ""));
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                                else allSuggestions.Add(new SuggestionItem("u <Unordered text to match>", "u|", GetResource("UnorderedMatchTitle"), GetResource("UnorderedMatchText")));
                                break;
                            case "ui":
                                allSuggestions.Add(new SuggestionItem("ui <Inverse replace text>", "ui|", GetResource("ReplaceInverseTitle"), GetResource("ReplaceInverseText")){ TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case "m":
                                if (_isReplace)
                                {
                                    switch (suggestionToken.Ancestors[^1])
                                    {
                                        case TokenName.MatchMultiple:
                                            allSuggestions.Add(new SuggestionItem("m <Separator> | <Reorder> | <Replace text>", "m|", GetResource("ReplaceMultiTitle"), GetResource("ReplaceMultiText")));
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                                else allSuggestions.Add(new SuggestionItem("m <Separator> | <Quantifier> | <Multiple match>", "m|{", GetResource("MultipleMatchTitle"), GetResource("MultipleMatchText")));
                                break;
                            case "w":
                                allSuggestions.Add(new SuggestionItem("w (Word)", "w", GetResource("WordTitle"), GetResource("WordText")));
                                break;
                            case "d":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("d <Duplication amount> | <Separator>", "d|", GetResource("DuplicationTitle"), GetResource("DuplicationText"))
                                    : new SuggestionItem("d (Digit)", "d", GetResource("DigitTitle"), GetResource("DigitText")));
                                break;
                            case "s":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("s (Sentence case)", "s]]", GetResource("SentenceText"), "")
                                    : new SuggestionItem("s (Whitespace)", "s", GetResource("SpaceTitle"), GetResource("SpaceText")));
                                break;
                            case "g":
                                allSuggestions.Add(new SuggestionItem("g (Greedy)", "g", GetResource("GreedyTitle"), GetResource("GreedyText")));
                                break;
                            case "l":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("l (Lowercase)", "l]]", GetResource("LowercaseText"), "")
                                    : new SuggestionItem("l (Include line breaks)", "l", GetResource("LBTitle"), GetResource("LBText")));
                                break;
                            case "o":
                                if(suggestionToken.Ancestors[^1] == TokenName.Optional)
                                    allSuggestions.Add(new SuggestionItem("o <Optional match>", "o|", GetResource("OptionalMatchTitle"), GetResource("OptionalMatchText")));
                                else
                                    allSuggestions.Add(_isReplace
                                        ? new SuggestionItem("o <Replace text>", "o|", GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                                        : new SuggestionItem("o (Optional)", "o", GetResource("OptionalTitle"), GetResource("OptionalText")));
                                break;
                            case "oi":
                                allSuggestions.Add(new SuggestionItem("oi <Inverse replace text>", "oi|", GetResource("ReplaceInverseTitle"), GetResource("ReplaceInverseText")) { TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case "e":
                                allSuggestions.Add(new SuggestionItem("e <New number>", "e|", GetResource("ReplaceNumberTitle"), GetResource("ReplaceNumberText")));
                                break;
                            case "c":
                                allSuggestions.Add(new SuggestionItem("c <Capitalization>", "c|", GetResource("CapitalizationTitle"), GetResource("CapitalizationText")));
                                break;
                            case "fu":
                                allSuggestions.Add(new SuggestionItem("fu (First letter uppercase)", "fu]]", GetResource("FirstUppercaseText"), "") { TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case "fl":
                                allSuggestions.Add(new SuggestionItem("fl (First letter lowercase)", "fl]]", GetResource("FirstLowercaseText"), "") { TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                        }
                        break;
                }
            }
            alreadyAdded.Clear();
            return allSuggestions;

            bool AlreadyAdded(TokenName tokenName, Structure structure)
            {
                var exists = alreadyAdded.Contains((tokenName, structure));
                if(!exists) alreadyAdded.Add((tokenName, structure));
                return exists;
            }

            bool AlreadyAddedP(Structure structure) => AlreadyAdded(TokenName.Pattern, structure);

            static string TypeTextHere(ReadOnlySpan<char> text) => $"(Type {text} here)";

            static string GetResource(string name) => Resources.ResourceManager.GetString(name);
        }

        private List<SuggestionItem> GetLabelSuggestion(string typedLabel, int insertionIndex)
        {
            if(_shouldRefreshLabelSuggestions)
            {
                _typedLabels = _getTypedLabels();
                _shouldRefreshLabelSuggestions = false;
            }
            return _typedLabels.Where(l => l != typedLabel && l.StartsWith(typedLabel, StringComparison.OrdinalIgnoreCase))
                .Select(l => new SuggestionItem(l, l, string.Empty, string.Empty) { TextInsertionIndex = insertionIndex }).ToList();
        }
    }

    public class IntellisenseItem: AutocompleteItem
    {
        public int? TextInsertionIndex { get; set; }
        public IntellisenseItem(string menuText, string textToInsert) : base(textToInsert, -1, menuText){}

        public IntellisenseItem(string menuText, string textToInsert, int? textInsertionIndex, string tipTitle,
            string tipText) : base(textToInsert, -1, menuText, tipTitle, tipText)
        {
            TextInsertionIndex = textInsertionIndex;
        }

        public override CompareResult Compare(string fragmentText)
        {
            return CompareResult.Visible;
        }
    }

    public class SuggestionItem(string suggestion, string textToInsert, string tipTitle, string tipText)
    {
        public SuggestionItem(string suggestion, string textToInsert) : this(suggestion, textToInsert, string.Empty, string.Empty)
        {
        }

        public string Suggestion { get; set; } = suggestion;
        public string TextToInsert { get; set; } = textToInsert;
        public int? TextInsertionIndex { get; set; }
        public string TipTitle { get; set; } = tipTitle;
        public string TipText { get; set; } = tipText;

        public override string ToString()
        {
            return Suggestion;
        }
    }
}
