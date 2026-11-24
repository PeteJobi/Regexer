using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static RegexerV2.SyntaxStructure;

namespace RegexerV2
{
    internal class SyntaxParser
    {
        public (IEnumerable<Token>? AlreadyTyped, List<SuggestionToken> Suggestions) ParsePatternWithSuggestions(string input, int start, int end, int index, Structure[] structure, int si, bool withSuggestions)
        {
            switch (structure[si])
            {
                case Symbol symbol:
                    {
                        if (!withSuggestions && index + symbol.Value.Length > end) return (null, []);
                        for (var i = 0; i < symbol.Value.Length; i++)
                        {
                            if (withSuggestions && index + i >= end) return (null, [new SuggestionToken(symbol){ PlacementIndex = index }]);
                            if (input[i + index] != symbol.Value[i]) return (null, []);
                        }

                        var result = new Token { Index = index, Length = symbol.Value.Length, Text = symbol.Value, Name = symbol.Name == TokenName.None ? TokenName.Symbol : symbol.Name };
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + result.Length, structure, si + 1, withSuggestions);
                            return (parse.AlreadyTyped?.Prepend(result), parse.Suggestions);
                        }

                        return (Enumerable.Repeat(result, 1), []);
                    }
                case Modifier modifier:
                    {
                        if (!withSuggestions && index + modifier.Value.Length > end) return (null, []);
                        for (var i = 0; i < modifier.Value.Length; i++)
                        {
                            if (withSuggestions && index + i >= end) return (null, [new SuggestionToken(modifier){ PlacementIndex = index }]);
                            if (input[i + index] != modifier.Value[i]) return (null, []);
                        }

                        var result = new Token { Index = index, Length = modifier.Value.Length, Text = modifier.Value, Name = TokenName.Modifier };
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + result.Length, structure, si + 1, withSuggestions);
                            return (parse.AlreadyTyped?.Prepend(result), parse.Suggestions);
                        }

                        return (Enumerable.Repeat(result, 1), []);
                    }
                case OneOrMoreNoRepeat oneOrMore:
                    {
                        var suggestions = new List<SuggestionToken>();
                        var subResults = new List<Token>();
                        var i = index;
                        var contents = new List<Structure>(oneOrMore.Contents);
                        while (contents.Count > 0)
                        {
                            var foundElementIndex = -1;
                            for (var e = 0; e < contents.Count; e++)
                            {
                                var element = contents[e];
                                var token = ParsePatternWithSuggestions(input, start, end, i, [element], 0, withSuggestions);
                                suggestions.AddRange(AddAncestorToSuggestions(token.Suggestions, oneOrMore.Name));
                                if (token.AlreadyTyped == null) continue;
                                subResults.AddRange(token.AlreadyTyped);
                                i += token.AlreadyTyped.Sum(t => t.Length);
                                foundElementIndex = e;
                                break;
                            }

                            if (foundElementIndex == -1) break;

                            contents.RemoveAt(foundElementIndex);
                        }

                        if (withSuggestions && i == end)
                        {
                            foreach (var content in contents)
                            {
                                suggestions.AddRange(AddAncestorToSuggestions(ParsePatternWithSuggestions(input, start, end, i, [content], 0, withSuggestions).Suggestions, oneOrMore.Name));
                            }
                        }

                        if (subResults.Count == 0) return (null, suggestions);
                        var resultLength = subResults.Sum(t => t.Length);
                        var result = oneOrMore.Name == TokenName.None ? subResults
                            : [new ComplexToken { Index = index, Length = resultLength, Children = subResults, Text = input.Substring(index, resultLength), Name = oneOrMore.Name }];
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + resultLength, structure, si + 1, withSuggestions);
                            suggestions.AddRange(parse.Suggestions);
                            return (parse.AlreadyTyped != null ? result.Concat(parse.AlreadyTyped) : null, suggestions);
                        }

                        return (result, suggestions);
                    }
                case And and:
                    {
                        var subResults = ParsePatternWithSuggestions(input, start, end, index, and.Contents, 0, withSuggestions);
                        if(and.Name != TokenName.SingleLineStructure) AddAncestorToSuggestions(subResults.Suggestions, and.SuggestionName ?? and.Name);
                        if (subResults.AlreadyTyped == null) return subResults with { Suggestions = subResults.Suggestions };
                        var resultLength = subResults.AlreadyTyped.Sum(t => t.Length);
                        var result = and.Name == TokenName.None ? subResults.AlreadyTyped
                            : [new ComplexToken { Index = index, Length = resultLength, Children = subResults.AlreadyTyped.ToList(), Text = input.Substring(index, resultLength), Name = and.Name }];
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + resultLength, structure, si + 1, withSuggestions);
                            subResults.Suggestions.AddRange(parse.Suggestions);
                            return (parse.AlreadyTyped != null ? result.Concat(parse.AlreadyTyped) : null, subResults.Suggestions);
                        }

                        return (result, subResults.Suggestions);
                    }
                case Or or:
                    {
                        List<Token>? alreadyTyped = null;
                        var suggestions = new List<SuggestionToken>();
                        foreach (var item in or.Contents)
                        {
                            var subResults = ParsePatternWithSuggestions(input, start, end, index, [item], 0, withSuggestions);
                            suggestions.AddRange(AddAncestorToSuggestions(subResults.Suggestions, or.Name));
                            if(subResults.AlreadyTyped == null) continue;
                            if (si < structure.Length - 1)
                            {
                                var parse = ParsePatternWithSuggestions(input, start, end, index + subResults.AlreadyTyped.Sum(t => t.Length), structure, si + 1, withSuggestions);
                                suggestions.AddRange(parse.Suggestions);
                                alreadyTyped ??= parse.AlreadyTyped != null ? [..subResults.AlreadyTyped, ..parse.AlreadyTyped] : null;
                            } 
                            else alreadyTyped ??= [..subResults.AlreadyTyped];
                            if (!withSuggestions && alreadyTyped != null) return (alreadyTyped, suggestions);
                        }

                        return (alreadyTyped, suggestions);
                    }
                case Vary vary:
                    {
                        var suggestions = new List<SuggestionToken>();
                        var subResults = new List<Token>();
                        var amount = vary.Quantifier == StructureQuantifier.ZERO_OR_ONE ? 1 : int.MaxValue;
                        var i = index;
                        var length = 0;
                        while (amount > 0 && i < end)
                        {
                            Structure[] structuresToParse = subResults.Count > 0 && vary.Separator != null ? [vary.Separator, vary.Content] : [vary.Content];
                            var token = ParsePatternWithSuggestions(input, start, end, i, structuresToParse, 0, withSuggestions);
                            suggestions.AddRange(AddAncestorToSuggestions(token.Suggestions, vary.Name));
                            var tokenLength = token.AlreadyTyped?.Sum(t => t.Length) ?? 0;
                            if ((token.AlreadyTyped == null || tokenLength == 0) && subResults.Count == 0 && vary.ReturnNullIfNull) return (null, suggestions);
                            if (token.AlreadyTyped == null || tokenLength == 0) break;
                            subResults.AddRange(token.AlreadyTyped);
                            i += tokenLength;
                            length += tokenLength;
                            amount--;
                        }

                        if (withSuggestions)
                        {
                            if (index == end) suggestions = AddAncestorToSuggestions(ParsePatternWithSuggestions(input, start, end, index, [vary.Content], 0, withSuggestions).Suggestions, vary.Name);
                            else if(i == end)
                            {
                                if(suggestions.Count == 0 && vary.Content.Name == TokenName.SingleLineStructure) suggestions.Add(new SuggestionToken(SingleLineFreeText));
                                else if(subResults.Count > 0 && vary.Separator != null)
                                    suggestions.AddRange(AddAncestorToSuggestions(ParsePatternWithSuggestions(input, start, end, i, [vary.Separator], 0, withSuggestions).Suggestions, vary.Name));
                            }
                        }

                        if (vary.Quantifier == StructureQuantifier.ONE_OR_MORE && length == 0) return (null, suggestions);

                        var result = vary.Name == TokenName.None ? subResults
                            : [new ComplexToken { Index = index, Length = length, Children = subResults, Text = input.Substring(index, length), Name = vary.Name }];
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + length, structure, si + 1, withSuggestions);
                            suggestions.AddRange(parse.Suggestions);
                            if (parse.AlreadyTyped != null)
                            {
                                parse.AlreadyTyped = result.Concat(parse.AlreadyTyped);
                                if(!withSuggestions) return (parse.AlreadyTyped, suggestions);
                            }
                            if (vary.Quantifier == StructureQuantifier.ZERO_OR_ONE && length > 0)
                            {
                                //Try again with zero occurrences of the vary content
                                var retryParse = ParsePatternWithSuggestions(input, start, end, index, structure, si + 1, withSuggestions);
                                suggestions.AddRange(retryParse.Suggestions);
                                parse.AlreadyTyped ??= retryParse.AlreadyTyped;
                            }
                            return (parse.AlreadyTyped, suggestions);
                        }

                        return (result, suggestions);
                    }
                case Whitespace whitespace:
                    {
                        var pos = index;
                        if (whitespace.Type == WhitespaceType.START_OF_LINE)
                        {
                            if(pos >= end - 1) return (null, []);
                            if (input[pos] == '\r' && input[pos + 1] == '\n') pos += 2;
                            else if (pos != start) return (null, []);
                        }
                        while (pos < end)
                        {
                            var currentChar = input[pos];
                            if (currentChar == '\r' && pos < end - 1 && input[pos + 1] == '\n')
                            {
                                if (whitespace.Type != WhitespaceType.NONE) break;
                                pos += 2;
                                continue;
                            }

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                if (whitespace.Type == WhitespaceType.END_OF_LINE) return (null, []);
                                break;
                            }
                            pos++;
                        }

                        var suggestions = pos == end ? [new SuggestionToken(whitespace)] : new List<SuggestionToken>();

                        var result = CreateToken();
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePatternWithSuggestions(input, start, end, index + result.Length, structure, si + 1, withSuggestions);
                            suggestions.AddRange(parse.Suggestions);
                            return (parse.AlreadyTyped?.Prepend(result), suggestions);
                        }

                        return (Enumerable.Repeat(result, 1), suggestions);

                        Token CreateToken() => new() { Index = index, Length = pos - index, Text = input.Substring(index, pos - index), Name = whitespace.Name };
                    }
                case Anchor anchor:
                    {
                        switch (anchor.Type)
                        {
                            case AnchorType.START_OF_PATTERN:
                                return index != start ? (null, []) : ParsePatternWithSuggestions(input, start, end, index, [anchor.Content], 0, withSuggestions);
                            case AnchorType.END_OF_PATTERN:
                                var token = ParsePatternWithSuggestions(input, start, end, index, [anchor.Content], 0, withSuggestions);
                                var tokenLength = token.AlreadyTyped?.Sum(t => t.Length) ?? 0;
                                return token with
                                {
                                    AlreadyTyped = index + tokenLength != end ? null : token.AlreadyTyped
                                };
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                case FreeText freeText:
                    {
                        if (index > end) return (null, [new SuggestionToken(freeText)]);
                        var suggestions = new List<SuggestionToken>();
                        var escapeEncountered = false;
                        var notContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var unescapedNotContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var pos = index;
                        var escapeIndices = new List<int>[freeText.CannotContain.Length];
                        for (var i = 0; i < escapeIndices.Length; i++) escapeIndices[i] = [];
                        var acceptedEscapeIndices = new List<int>();

                        while (pos < end)
                        {
                            var currentChar = input[pos];
                            if (currentChar == '\\')
                            {
                                escapeEncountered = !escapeEncountered;
                                if (escapeEncountered)
                                {
                                    foreach (var escapeIndicesList in escapeIndices) escapeIndicesList.Add(pos);
                                }
                            }
                            else
                            {
                                var currentCharIsUnescapedNotContainChar = false;
                                for (var i = 0; i < freeText.CannotContain.Length; i++)
                                {
                                    if (currentChar == freeText.CannotContain[i].Value[notContainEncountered[i]])
                                    {
                                        notContainEncountered[i]++;
                                        if(notContainEncountered[i] == freeText.CannotContain[i].Value.Length)
                                        {
                                            acceptedEscapeIndices.AddRange(escapeIndices[i]);
                                            escapeIndices[i].Clear();
                                            notContainEncountered[i] = 0;
                                        }
                                    }
                                    else
                                    {
                                        notContainEncountered[i] = 0;
                                        escapeIndices[i].Clear();
                                    }

                                    if (currentChar == freeText.CannotContain[i].Value[unescapedNotContainEncountered[i]] && !escapeEncountered)
                                    {
                                        unescapedNotContainEncountered[i]++;
                                        currentCharIsUnescapedNotContainChar = true;
                                    }
                                    else unescapedNotContainEncountered[i] = 0;

                                    if (unescapedNotContainEncountered[i] == freeText.CannotContain[i].Value.Length)
                                    {
                                        var result = CreateTokenOrContinue(
                                            pos,
                                            pos - index - freeText.CannotContain[i].Value.Length + 1,
                                            freeText.CannotContain[i].Value,
                                            out var @continue);
                                        if (!@continue) return result;
                                        unescapedNotContainEncountered[i] = 0;
                                        pos -= freeText.CannotContain[i].Value.Length - 1;
                                    }
                                }

                                escapeEncountered = false;
                                
                                if (!currentCharIsUnescapedNotContainChar && !CharAccepted(currentChar)) return (null, []);
                            }

                            pos++;
                        }

                        //Remove any possible notContainEncountered that were not completed at the end of the input
                        if (freeText.Type == FreeTextType.LABEL)
                        {
                            var longestNotContained = 0;
                            foreach (var t in unescapedNotContainEncountered)
                            {
                                if (t > longestNotContained) longestNotContained = t;
                            }
                            pos -= longestNotContained;
                        }
                        return CreateTokenOrContinue(pos, pos - index, string.Empty, out _);


                        (IEnumerable<Token>? AlreadyTyped, List<SuggestionToken> Suggestions) CreateTokenOrContinue(int pos, int freeTextLength, string notContain, out bool shouldContinue)
                        {
                            shouldContinue = false;
                            if(freeText.Type is not FreeTextType.PRE_PATTERN_TEXT && pos == end && withSuggestions)
                            {
                                var suggestionToken =  new SuggestionToken(freeText);
                                if(freeText.Type == FreeTextType.LABEL)
                                {
                                    suggestionToken.LabelFreeTextEnteredSoFar = input.Substring(index, freeTextLength);
                                    suggestionToken.PlacementIndex = index;
                                }
                                suggestions.Add(suggestionToken);
                            }
                            if (freeText.MustNotBeEmpty && freeTextLength == 0) return (null, suggestions);
                            var result = CreateToken(freeTextLength);
                            if (si >= structure.Length - 1) return result.Length == 0 ? ([], suggestions) : ([result], suggestions);
                            var parse = ParsePatternWithSuggestions(input, start, end, index + result.Length, structure, si + 1, withSuggestions);
                            if(pos < end || freeText.Type is not FreeTextType.PRE_PATTERN_TEXT and not FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT) suggestions.AddRange(parse.Suggestions);
                            if (parse.AlreadyTyped != null) return (parse.AlreadyTyped.Prepend(result), suggestions);
                            if (freeText.Type == FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT && notContain == freeText.ProceedIfInvalidAfterEncountered?.Value)
                                return ([result], suggestions);
                            shouldContinue = freeText.Type is not (FreeTextType.LABEL or FreeTextType.DIGITS);
                            return (null, suggestions);
                        }

                        FreeTextToken CreateToken(int freeTextLength) => new() { Index = index, Length = freeTextLength, Text = input.Substring(index, freeTextLength), Name = freeText.Name, EscapeIndices = acceptedEscapeIndices };

                        bool CharAccepted(char currentChar)
                        {
                            return freeText.Type switch
                            {
                                FreeTextType.LABEL => char.IsLetterOrDigit(currentChar) || currentChar == '_',
                                FreeTextType.DIGITS => char.IsDigit(currentChar),
                                FreeTextType.EXPRESSION_WITH_IC => IsExpressionChar(currentChar) || currentChar == 'i' || currentChar == 'c',
                                FreeTextType.EXPRESSION_WITH_ICJD => IsExpressionChar(currentChar) || CharIsIcjd(currentChar),
                                FreeTextType.EXPRESSION_WITH_ICJDM => IsExpressionChar(currentChar) || CharIsIcjd(currentChar) || currentChar == 'm',
                                FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT => currentChar != '\r' && currentChar != '\n',
                                _ => true
                            };

                            static bool IsExpressionChar(char ch) => char.IsDigit(ch) || ch is '+' or '-' or '*' or '/' or '(' or ')' or '%';
                            static bool CharIsIcjd(char ch) => ch is 'i' or 'c' or 'j' or 'd';
                        }
                    }
                default: throw new NotImplementedException();
            }
        }

        private List<SuggestionToken> AddAncestorToSuggestions(List<SuggestionToken> suggestions, TokenName ancestorTokenName)
        {
            if (ancestorTokenName == TokenName.None) return suggestions;
            foreach (var suggestion in suggestions)
            {
                suggestion.Ancestors.Insert(0, ancestorTokenName);
            }
            return suggestions;
        }
    }
}
