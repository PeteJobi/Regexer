using FastColoredTextBoxNS;
using RegexerUIV2.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        //This was the initial try at suggestions, but later scrapped and replaced with what is now GetSuggestionItems. After it's pushed, I will delete this.
        public (List<Token> AlreadyTyped, List<Structure> Suggestions) GetSuggestionItems2(Structure structure, string text, int index)
        {
            switch (structure)
            {
                case And and:
                    {
                        var alreadyTyped = new List<Token>();
                        var suggestions = new List<Structure>();
                        var i = index;
                        foreach (var child in and.Contents)
                        {
                            var result = GetSuggestionItems2(child, text, i);
                            switch (result)
                            {
                                case ({ Count: 0 }, { Count: 0 }):
                                    return ([], []);
                                case ({ Count: > 0 }, { Count: > 0 }):
                                    alreadyTyped.AddRange(result.AlreadyTyped);
                                    suggestions.AddRange(result.Suggestions);
                                    foreach (var token in result.AlreadyTyped) i += token.Length;
                                    break;
                                case ({ Count: > 0 }, { Count: 0 }):
                                    alreadyTyped.AddRange(result.AlreadyTyped);
                                    foreach (var token in result.AlreadyTyped) i += token.Length;
                                    break;
                                case ({ Count: 0 }, { Count: > 0 }):
                                    suggestions.AddRange(result.Suggestions);
                                    return (alreadyTyped, suggestions);
                            }
                        }

                        return (alreadyTyped, suggestions);
                    }
                case Or or:
                    {
                        var alreadyTyped = new List<Token>();
                        var suggestions = new List<Structure>();
                        foreach (var child in or.Contents)
                        {
                            var result = GetSuggestionItems2(child, text, index);
                            if (result.AlreadyTyped.Count > 0)
                            {
                                if (alreadyTyped.Count > 0) {}
                                alreadyTyped = result.AlreadyTyped;
                            }
                            if (result.Suggestions.Count > 0) suggestions.AddRange(result.Suggestions);
                        }

                        return (alreadyTyped, suggestions);
                    }
                case Symbol symbol:
                    {
                        for (var i = 0; i < symbol.Value.Length; i++)
                        {
                            if(index + i == text.Length) return ([], [symbol]);
                            if (symbol.Value[i] != text[index + i]) return ([], []);
                        }
                        return (
                            [new Token { Index = index, Length = symbol.Value.Length, Text = symbol.Value, Name = symbol.Name }],
                            []
                            );
                    }
                case Modifier modifier:
                    {
                        for (var i = 0; i < modifier.Value.Length; i++)
                        {
                            if(index + i == text.Length) return ([], [modifier]);
                            if (modifier.Value[i] != text[index + i]) return ([], []);
                        }
                        return (
                            [new Token { Index = index, Length = modifier.Value.Length, Text = modifier.Value, Name = TokenName.Modifier }],
                            []
                            );
                    }
                case Vary vary:
                    {
                        //return GetSuggestionItems2(vary.Content, text, index);
                        var alreadyTyped = new List<Token>();
                        var suggestions = new List<Structure>();
                        var amount = vary.Quantifier == StructureQuantifier.ZERO_OR_ONE ? 1 : int.MaxValue;
                        var i = index;
                        var length = 0;
                        while (amount > 0 && i < text.Length)
                        {
                            var result = GetSuggestionItems2(vary.Content, text, index);
                            var tokenLength = result.AlreadyTyped.Sum(t => t.Length);
                            if (result is ([], []) && alreadyTyped.Count == 0 && vary.ReturnNullIfNull) return ([], []);
                            if (result is ([], [])) break;
                            alreadyTyped.AddRange(result.AlreadyTyped);
                            suggestions = result.Suggestions;
                            i += tokenLength;
                            length += tokenLength;
                            amount--;
                        }

                        if (vary.Quantifier == StructureQuantifier.ONE_OR_MORE && length == 0) return ([], []);
                        return (alreadyTyped, i == index ? [vary.Content] : suggestions);
                    }
                case Whitespace whitespace:
                    {
                        var pos = index;
                        if (whitespace.Type == WhitespaceType.START_OF_LINE)
                        {
                            if (pos != 0)
                            {
                                if (pos >= text.Length - 1) return ([], []);
                                if (text[pos] != '\r' || text[pos + 1] != '\n') return ([], []);
                                pos += 2;
                            }
                        }
                        while (pos < text.Length)
                        {
                            var currentChar = text[pos];
                            if (currentChar == '\r' && pos < text.Length - 1 && text[pos + 1] == '\n')
                            {
                                if (whitespace.Type != WhitespaceType.NONE) return (pos == index ? [] : [CreateToken()], []);
                                pos += 2;
                                continue;
                            }

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                if (whitespace.Type == WhitespaceType.END_OF_LINE) return ([CreateToken()], []);
                                return (pos == index ? [] : [CreateToken()], []);
                            }
                            pos++;
                        }

                        return (pos == index ? [] : [CreateToken()], [whitespace]);

                        Token CreateToken() => new() { Index = index, Length = pos - index, Text = text.Substring(index, pos - index), Name = whitespace.Name };
                    }
                case FreeText freeText:
                    {
                        var escapeEncountered = false;
                        var notContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var unescapedNotContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var pos = index;

                        while (pos < text.Length)
                        {
                            var currentChar = text[pos];
                            if (currentChar == '\\')
                            {
                                escapeEncountered = !escapeEncountered;
                            }
                            else
                            {
                                var currentCharIsUnescapedNotContainChar = false;
                                for (var i = 0; i < freeText.CannotContain.Length; i++)
                                {
                                    if (currentChar == freeText.CannotContain[i].Value[notContainEncountered[i]])
                                    {
                                        notContainEncountered[i]++;
                                        if (notContainEncountered[i] == freeText.CannotContain[i].Value.Length)
                                        {
                                            notContainEncountered[i] = 0;
                                        }
                                    }
                                    else
                                    {
                                        notContainEncountered[i] = 0;
                                    }

                                    if (currentChar == freeText.CannotContain[i].Value[unescapedNotContainEncountered[i]] && !escapeEncountered)
                                    {
                                        unescapedNotContainEncountered[i]++;
                                        currentCharIsUnescapedNotContainChar = true;
                                    }
                                    else unescapedNotContainEncountered[i] = 0;

                                    if (unescapedNotContainEncountered[i] == freeText.CannotContain[i].Value.Length)
                                    {
                                        return ([CreateToken(index, pos - index)], []);
                                    }
                                }

                                escapeEncountered = false;

                                if (!currentCharIsUnescapedNotContainChar && !CharAccepted(currentChar)) return ([], []);
                            }

                            pos++;
                        }

                        return (pos == index ? [] : [CreateToken(index, pos - index)], [freeText]);

                        Token CreateToken(int i, int freeTextLength) => new() { Index = i, Length = freeTextLength, Text = text.Substring(i, freeTextLength), Name = freeText.Name };

                        bool CharAccepted(char currentChar)
                        {
                            return freeText.Type switch
                            {
                                FreeTextType.LABEL => char.IsLetterOrDigit(currentChar) || currentChar == '_',
                                FreeTextType.DIGITS => char.IsDigit(currentChar),
                                FreeTextType.EXPRESSION_WITH_I => IsExpressionChar(currentChar) || currentChar == 'i',
                                FreeTextType.EXPRESSION_WITH_IM => IsExpressionChar(currentChar) || currentChar == 'i' || currentChar == 'm',
                                FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT => currentChar != '\r' && currentChar != '\n',
                                _ => true
                            };

                            static bool IsExpressionChar(char ch) => char.IsDigit(ch) || ch is '+' or '-' or '*' or '/' or '(' or ')' or '%';
                        }
                    }
                default: throw new NotImplementedException();
            }
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
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere(_isReplace ? "replacement text" : "line/phrase to match"), string.Empty));
                                        break;
                                    case TokenName.Optional:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("replacement text"), string.Empty));
                                        break;
                                    case TokenName.MatchMultipleReplace:
                                        allSuggestions.Add(new SuggestionItem(TypeTextHere("replacement text"), string.Empty));
                                        break;
                                    case TokenName.MultiLine:
                                        //This is replace version of multiline (see MultiLineReplace). No need to suggest that single line text can appear before the multiline
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
                                if (!AlreadyAdded(suggestionToken.Ancestors[^1], whitespace)) break;
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
                                            ? new SuggestionItem("| <Separator | Replace text>", "|", GetResource("ReplaceMultiTitle"), GetResource("ReplaceMultiText"))
                                            : new SuggestionItem("|{ <Separator>", "|{", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText")));
                                        break;
                                    case TokenName.MatchMultipleReplace:
                                        allSuggestions.Add(new SuggestionItem("| <Replace text>", "|", GetResource("ReplaceMultiReplaceTitle"), GetResource("ReplaceMultiReplaceText")));
                                        break;
                                    case TokenName.MatchMultipleSpread:
                                        allSuggestions.Add(new SuggestionItem("| (Capture from multi match)", "|m]]", GetResource("MultiMatchCaptureTitle"), GetResource("MultiMatchCaptureText")));
                                        break;
                                    case TokenName.MatchMultipleMatch:
                                        allSuggestions.Add(new SuggestionItem("| <Match>", "|", GetResource("MultipleMatchMatchTitle"), GetResource("MultipleMatchMatchText")));
                                        break;
                                    case TokenName.Optional:
                                        allSuggestions.Add(_isReplace
                                            ? new SuggestionItem("| <Replace text>", "|")
                                            : new SuggestionItem("| <Optional>", "|o]]", GetResource("CustomRegexOptionalTitle"), GetResource("CustomRegexOptionalText")));
                                        break;
                                    case TokenName.Duplicate:
                                        allSuggestions.Add(new SuggestionItem("| <Duplication amount | Separator>", "|", GetResource("DuplicationSeparatorTitle"), GetResource("DuplicationSeparatorText")));
                                        break;
                                    case TokenName.DuplicateSeparator:
                                        allSuggestions.Add(new SuggestionItem("| <Separator>", "|", GetResource("DuplicationSeparatorTitle"), GetResource("DuplicationSeparatorText")));
                                        break;
                                    case TokenName.Capitalize:
                                        allSuggestions.Add(new SuggestionItem("| <Capitalization type>", "|", GetResource("CapitalizationTitle"), GetResource("CapitalizationText")));
                                        break;
                                    case TokenName.Evaluate:
                                        allSuggestions.Add(new SuggestionItem("| <Expression>", "|", GetResource("ReplaceNumberTitle"), GetResource("ReplaceNumberText")));
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
                                        allSuggestions.Add(new SuggestionItem("<..> (Exact amount match)", symbol.Value, GetResource("CustomRegexExactTitle"), GetResource("CustomRegexExactText")));
                                        break;
                                    case "-":
                                        allSuggestions.Add(new SuggestionItem("- (Range separator)", symbol.Value, GetResource("CustomRegexExactDashTitle"), GetResource("CustomRegexExactDashText")));
                                        break;
                                    case ">":
                                        allSuggestions.Add(new SuggestionItem("> (Close range)", symbol.Value, GetResource("CustomRegexExactCloseTitle"), GetResource("CustomRegexExactCloseText")));
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
                                                allSuggestions.Add(new SuggestionItem("} <Close regex>", "}", GetResource("CustomRegexTitle"), GetResource("CustomRegexText")));
                                                break;
                                            case TokenName.MatchMultiple:
                                                allSuggestions.Add(new SuggestionItem("} <Close separator>", "}", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText")));
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException();
                                        }
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
                                    ? new SuggestionItem("ml (Multi line replace)", "ml]]", GetResource("MultiLineRepTitle"), GetResource("MultiLineRepText")) { TextInsertionIndex = suggestionToken.PlacementIndex }
                                    : new SuggestionItem("ml (Multi line match)", "ml]]\r\n", GetResource("MultiLineTitle"), GetResource("MultiLineText")){ TextInsertionIndex = suggestionToken.PlacementIndex });
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
                                            allSuggestions.Add(new SuggestionItem("u <Uppercase>", "u]]", GetResource("UppercaseText"), ""));
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
                                            allSuggestions.Add(new SuggestionItem("m <Separator | Replace text>", "m|", GetResource("ReplaceMultiTitle"), GetResource("ReplaceMultiText")));
                                            break;
                                        case TokenName.MatchMultipleSpread:
                                            allSuggestions.Add(new SuggestionItem("m (Capture from multi match)", "m]]", GetResource("MultiMatchCaptureTitle"), GetResource("MultiMatchCaptureText")));
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                                else allSuggestions.Add(new SuggestionItem("m <Separator | Multiple text to match>", "m|{", GetResource("MultipleMatchTitle"), GetResource("MultipleMatchText")));
                                break;
                            case "w":
                                allSuggestions.Add(new SuggestionItem("w <Word>", "w", GetResource("CustomRegexWordTitle"), GetResource("CustomRegexWordText")));
                                break;
                            case "d":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("d <Duplication amount | Separator>", "d|", GetResource("DuplicationTitle"), GetResource("DuplicationText"))
                                    : new SuggestionItem("d <Digit>", "d", GetResource("CustomRegexDigitTitle"), GetResource("CustomRegexDigitText")));
                                break;
                            case "s":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("s <Sentence case>", "s]]", GetResource("SentenceText"), "")
                                    : new SuggestionItem("s <Whitespace>", "s", GetResource("CustomRegexSpaceTitle"), GetResource("CustomRegexSpaceText")));
                                break;
                            case "g":
                                allSuggestions.Add(new SuggestionItem("g <Greedy match>", "g", GetResource("CustomRegexGreedyTitle"), GetResource("CustomRegexGreedyText")));
                                break;
                            case "l":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("l <Lowercase>", "l]]", GetResource("LowercaseText"), "")
                                    : new SuggestionItem("l <Include line breaks>", "l", GetResource("CustomRegexLBTitle"), GetResource("CustomRegexLBText")));
                                break;
                            case "o":
                                allSuggestions.Add(_isReplace
                                    ? new SuggestionItem("o <Replace text>", "o|", GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                                    : new SuggestionItem("o <Optional match>", "o]]", GetResource("CustomRegexOptionalTitle"), GetResource("CustomRegexOptionalText")));
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
                                allSuggestions.Add(new SuggestionItem("fu <First letter uppercase>", "fu]]", GetResource("FirstUppercaseText"), "") { TextInsertionIndex = suggestionToken.PlacementIndex });
                                break;
                            case "fl":
                                allSuggestions.Add(new SuggestionItem("fl <First letter lowercase>", "fl]]", GetResource("FirstLowercaseText"), "") { TextInsertionIndex = suggestionToken.PlacementIndex });
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

            string TypeTextHere(ReadOnlySpan<char> text) => $"(Type {text} here)";

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
        public IntellisenseData? IntellisenseData { get; set; }
        public Func<GroupCollection, bool>? ShouldHide { get; set; }

        public override string ToString()
        {
            return Suggestion;
        }
    }
}
