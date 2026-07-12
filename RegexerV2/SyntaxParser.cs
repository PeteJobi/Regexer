using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static RegexerV2.SyntaxStructure;

namespace RegexerV2
{
    internal class SyntaxParser
    {
        public Token? ParseFind(string input, int index, Structure structure, BacktrackContext backtrackContext = default)
        {
            //if(index >= input.Length) return null;

            switch (structure)
            {
                case FreeText freeText:
                {
                    if (index > input.Length) return null;
                    var escapeEncountered = false;
                    var notContainEncountered = new int[freeText.CannotContain.Length];
                    var pos = index == backtrackContext.StartingIndex ? backtrackContext.EndingIndex : index;
                    while (pos < input.Length)
                    {
                        var currentChar = input[pos];
                        if (currentChar == '\\') escapeEncountered = !escapeEncountered;
                        else
                        {
                            var currentCharIsNotContainChar = false;
                            for (var i = 0; i < freeText.CannotContain.Length; i++)
                            {
                                if (currentChar == freeText.CannotContain[i].Value[notContainEncountered[i]] &&
                                    !escapeEncountered)
                                {
                                    notContainEncountered[i]++;
                                    currentCharIsNotContainChar = true;
                                }
                                else
                                {
                                    notContainEncountered[i] = 0;
                                }

                                escapeEncountered = false;
                                if (notContainEncountered[i] == freeText.CannotContain[i].Value.Length)
                                {
                                    return CreateToken(index, pos - index - freeText.CannotContain[i].Value.Length + 1, notContainEncountered[i]);
                                }
                            }
                            if (!currentCharIsNotContainChar && !CharAccepted(currentChar)) return null;
                        }

                        pos++;
                    }

                    return CreateToken(index, pos - index, 0);

                    Token? CreateToken(int i, int freeTextLength, int notContainLength) => freeText.MustNotBeEmpty && freeTextLength == 0 ? null :
                        new Token
                        {
                            Index = i,
                            Length = freeTextLength,
                            Text = input.Substring(i, freeTextLength),
                            BacktrackContext = freeText.Type != FreeTextType.PRE_PATTERN_TEXT || notContainLength == 0 ? default : new BacktrackContext(index, index + freeTextLength + notContainLength)
                        };

                    bool CharAccepted(char currentChar)
                    {
                        return freeText.Type switch
                        {
                            FreeTextType.LABEL => IsLabelChar(currentChar),
                            FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT => currentChar != '\r' && currentChar != '\n',
                            _ => true
                        };
                    }
                }
                case Symbol symbol:
                {
                    if (index + symbol.Value.Length > input.Length) return null;
                    for (var i = 0; i < symbol.Value.Length; i++)
                    {
                        if (input[i + index] != symbol.Value[i]) return null;
                    }

                    return new Token { Index = index, Length = symbol.Value.Length, Text = symbol.Value };
                }
                case And and:
                {
                    var results = new List<Token>();
                    var i = index;
                    var length = 0;
                    var backtracks = new Stack<(int ContentIndex, BacktrackContext Context)>();
                    var bcToUse = backtrackContext;
                    for (var n = 0; n < and.Contents.Length; n++)
                    {
                        var item = and.Contents[n];
                        var token = ParseFind(input, i, item, bcToUse);
                        if (token != null)
                        {
                            results.AddRange(token);
                            i += token.Length;
                            length += token.Length;
                            if (!token.BacktrackContext.Equals(default)) backtracks.Push((n, token.BacktrackContext));
                        }
                        else
                        {
                            if(backtracks.Count == 0) return null;
                            var (contentIndex, context) = backtracks.Pop();
                            bcToUse = context;
                            n = contentIndex - 1;
                            i = results[contentIndex].Index;
                            results = results[..contentIndex];
                            length = results.Sum(t => t.Length);
                            //token = ParseFind(input, i, and.Contents[ContentIndex]);
                            //if (token != null)
                            //{
                            //    results.AddRange(token);
                            //    i += token.Length;
                            //    length += token.Length;
                            //    if (!token.BacktrackContext.Equals(default)) backtracks.Push((ContentIndex, token.BacktrackContext));
                            //}
                        }
                    }

                    return new ComplexToken{ Index = index, Length = length, Children = results, Text = input.Substring(index, length), BacktrackContext = backtracks.Count > 0 ? backtracks.Pop().Context : default};
                }
                case OneOrMoreNoRepeat oneOrMore:
                {
                    var results = new List<Token>();
                    var i = index;
                    var length = 0;
                    var contents = new List<Structure>(oneOrMore.Contents);
                    while (contents.Count > 0)
                    {
                        var foundElementIndex = -1;
                        for (var e = 0; e < contents.Count; e++)
                        {
                            var element = contents[e];
                            var token = ParseFind(input, i, element);
                            if (token == null) continue;
                            results.Add(token);
                            i += token.Length;
                            length += token.Length;
                            foundElementIndex = e;
                            break;
                        }

                        if (foundElementIndex == -1)
                        {
                            if (results.Count == 0) return null;
                            break;
                        }
                        contents.RemoveAt(foundElementIndex);
                    }
                    return new ComplexToken{ Index = index, Length = length, Children = results, Text = input.Substring(index, length) };
                }
                case Or or:
                    foreach (var item in or.Contents)
                    {
                        var token = ParseFind(input, index, item);
                        if (token != null) return token;
                    }
                    return null;
                case Vary vary:
                {
                    var results = new List<Token>();
                    var backtracks = new Stack<(int ContentIndex, BacktrackContext Context)>();
                    var bcToUse = backtrackContext;
                    var amount = vary.Quantifier == StructureQuantifier.ZERO_OR_ONE ? 1 : int.MaxValue;
                    var i = index;
                    var length = 0;
                    while (amount > 0 && i < input.Length)
                    {
                        var token = ParseFind(input, i, vary.Content, bcToUse);
                        if(token == null || token.Length == 0)
                        {
                            //if (vary.Quantifier == StructureQuantifier.ZERO_OR_ONE) return null;
                            if (backtracks.Count == 0) break;
                            var (contentIndex, context) = backtracks.Pop();
                            bcToUse = context;
                            i = results[contentIndex].Index;
                            results = results[..contentIndex];
                            length = results.Sum(t => t.Length);
                            continue;
                        }
                        results.Add(token);
                        i += token.Length;
                        length += token.Length;
                        amount--;
                        if (!token.BacktrackContext.Equals(default)) backtracks.Push((results.Count - 1, token.BacktrackContext));
                    }
                    return new ComplexToken{ Index = index, Length = length, Children = results, Text = input.Substring(index, length), BacktrackContext = backtracks.Count > 0 ? backtracks.Pop().Context : default };
                }
                case Modifier modifier:
                {
                    if (index + modifier.Value.Length > input.Length) return null;
                    for (var i = 0; i < modifier.Value.Length; i++)
                    {
                        if (input[i + index] != modifier.Value[i]) return null;
                    }

                    return new Token { Index = index, Length = modifier.Value.Length, Text = modifier.Value };
                }
                default: throw new NotImplementedException();
            }
        }

        public IEnumerable<Token>? ParsePattern(string input, int index, Structure[] structure, int si)
        {
            switch (structure[si])
            {
                case Symbol symbol:
                    {
                        if (index + symbol.Value.Length > input.Length) return null;
                        for (var i = 0; i < symbol.Value.Length; i++)
                        {
                            if (input[i + index] != symbol.Value[i]) return null;
                        }

                        var result = new Token { Index = index, Length = symbol.Value.Length, Text = symbol.Value, Name = symbol.Name };
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePattern(input, index + result.Length, structure, si + 1);
                            return parse?.Prepend(result);
                        }

                        return Enumerable.Repeat(result, 1);
                    }
                case Modifier modifier:
                    {
                        if (index + modifier.Value.Length > input.Length) return null;
                        for (var i = 0; i < modifier.Value.Length; i++)
                        {
                            if (input[i + index] != modifier.Value[i]) return null;
                        }

                        var result = new Token { Index = index, Length = modifier.Value.Length, Text = modifier.Value, Name = TokenName.Modifier };
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePattern(input, index + result.Length, structure, si + 1);
                            return parse?.Prepend(result);
                        }

                        return Enumerable.Repeat(result, 1);
                    }
                case OneOrMoreNoRepeat oneOrMore:
                {
                    var subResults = new List<Token>();
                    var i = index;
                    var contents = new List<Structure>(oneOrMore.Contents);
                    while (contents.Count > 0)
                    {
                        var foundElementIndex = -1;
                        for (var e = 0; e < contents.Count; e++)
                        {
                            var element = contents[e];
                            var token = ParsePattern(input, i, [element], 0)?.ToArray();
                            if (token == null) continue;
                            subResults.AddRange(token);
                            i += token.Sum(t => t.Length);
                            foundElementIndex = e;
                            break;
                        }

                        if (foundElementIndex == -1)
                        {
                            if (subResults.Count == 0) return null;
                            break;
                        }

                        contents.RemoveAt(foundElementIndex);
                    }

                    if (subResults.Count == 0) return null;
                    var resultLength = subResults.Sum(t => t.Length);
                    var result = oneOrMore.Name == TokenName.None ? subResults
                        : [new ComplexToken { Index = index, Length = resultLength, Children = subResults, Text = input.Substring(index, resultLength), Name = oneOrMore.Name }];
                    if (si < structure.Length - 1)
                    {
                        var parse = ParsePattern(input, index + resultLength, structure, si + 1);
                        return parse != null ? result.Concat(parse) : null;
                    }

                    return result;
                }
                case And and:
                {
                    var subResults = ParsePattern(input, index, and.Contents, 0)?.ToList();
                    if (subResults == null) return null;
                    var resultLength = subResults.Sum(t => t.Length);
                    var result = and.Name == TokenName.None ? subResults
                        : [new ComplexToken { Index = index, Length = resultLength, Children = subResults, Text = input.Substring(index, resultLength), Name = and.Name }];
                    if (si < structure.Length - 1)
                    {
                        var parse = ParsePattern(input, index + resultLength, structure, si + 1);
                        return parse != null ? result.Concat(parse) : null;
                    }

                    return result;
                }
                case Or or:
                {
                    foreach (var item in or.Contents)
                    {
                        var subResults = ParsePattern(input, index, [item], 0)?.ToArray();
                        if (subResults == null) continue;
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePattern(input, index + subResults.Sum(t => t.Length), structure, si + 1);
                            if (parse != null) return subResults.Concat(parse);
                        }
                        else return subResults;
                    }

                    return null;
                }
                case Vary vary:
                    {
                        var subResults = new List<Token>();
                        var amount = vary.Quantifier == StructureQuantifier.ZERO_OR_ONE ? 1 : int.MaxValue;
                        var i = index;
                        var length = 0;
                        while (amount > 0 && i < input.Length)
                        {
                            var token = ParsePattern(input, i, [vary.Content], 0)?.ToArray();
                            var tokenLength = token?.Sum(t => t.Length) ?? 0;
                            if (token == null && subResults.Count == 0 && vary.ReturnNullIfEmpty) return null;
                            if (token == null || tokenLength == 0) break;
                            subResults.AddRange(token);
                            i += tokenLength;
                            length += tokenLength;
                            amount--;
                        }

                        if (vary.Quantifier == StructureQuantifier.ONE_OR_MORE && length == 0) return null;

                        var result = vary.Name == TokenName.None ? subResults
                            : [new ComplexToken { Index = index, Length = length, Children = subResults, Text = input.Substring(index, length), Name = vary.Name }];
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePattern(input, index + length, structure, si + 1);
                            if(parse != null) return result.Concat(parse);
                            if (vary.Quantifier == StructureQuantifier.ZERO_OR_ONE)
                            {
                                //Try again with zero occurrences of the vary content
                                return ParsePattern(input, index, structure, si + 1);
                            }
                        }

                        return result;
                    }
                case Whitespace whitespace:
                    {
                        var pos = index;
                        if (whitespace.Type == WhitespaceType.START_OF_LINE)
                        {
                            if (pos != 0)
                            {
                                if(pos >= input.Length - 1) return null;
                                if(input[pos] != '\r' || input[pos + 1] != '\n') return null;
                                pos += 2;
                            }
                        }
                        while (pos < input.Length)
                        {
                            var currentChar = input[pos];
                            if (currentChar == '\r' && pos < input.Length - 1 && input[pos + 1] == '\n')
                            {
                                if (whitespace.Type != WhitespaceType.NONE) break;
                                pos += 2;
                                continue;
                            }

                            if (!char.IsWhiteSpace(currentChar))
                            {
                                if (whitespace.Type == WhitespaceType.END_OF_LINE) return null;
                                break;
                            }
                            pos++;
                        }

                        var result = CreateToken();
                        if (si < structure.Length - 1)
                        {
                            var parse = ParsePattern(input, index + result.Length, structure, si + 1);
                            return parse?.Prepend(result);
                        }

                        return Enumerable.Repeat(result, 1);

                        Token CreateToken() => new() { Index = index, Length = pos - index, Text = input.Substring(index, pos - index), Name = whitespace.Name };
                    }
                case FreeText freeText:
                    {
                        if (index > input.Length) return null;
                        var escapeEncountered = false;
                        var notContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var unescapedNotContainEncountered = new int[freeText.CannotContain.Length]; //stackalloc instead of array?
                        var pos = index;
                        var escapeIndices = new List<int>[] { [], [] };
                        var acceptedEscapeIndices = new List<int>();

                        while (pos < input.Length)
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
                                            index,
                                            pos - index - freeText.CannotContain[i].Value.Length + 1,
                                            freeText.CannotContain[i].Value,
                                            out var @continue);
                                        if (!@continue) return result;
                                        unescapedNotContainEncountered[i] = 0;
                                        pos -= freeText.CannotContain[i].Value.Length - 1;
                                    }
                                }

                                escapeEncountered = false;
                                
                                if (!currentCharIsUnescapedNotContainChar && !CharAccepted(currentChar)) return null;
                            }

                            pos++;
                        }

                        //var endingResult = CreateToken(index, pos - index);
                        //if (si >= structure.Length - 1) return endingResult.Length == 0 ? [] : [endingResult];
                        return CreateTokenOrContinue(index, pos - index, string.Empty, out _);


                        IEnumerable<Token>? CreateTokenOrContinue(int i, int freeTextLength, string notContain, out bool shouldContinue)
                        {
                            shouldContinue = false;
                            if (freeText.MustNotBeEmpty && freeTextLength == 0) return null;
                            var result = CreateToken(i, freeTextLength);
                            if (si >= structure.Length - 1) return result.Length == 0 ? [] : [result];
                            var parse = ParsePattern(input, i + result.Length, structure, si + 1);
                            if (parse != null) return result.Length == 0 && false ? parse : parse.Prepend(result);
                            if (freeText.Type == FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT && notContain == freeText.ProceedIfInvalidAfterEncountered?.Value) return [result];
                            //if (freeText.Type == FreeTextType.PRE_PATTERN_TEXT && notContain == freeText.ProceedIfInvalidAfterEncountered?.Value) return [result];
                            //if (parse != null && (freeText.Type != FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT || parse.Length > 0 || notContain != freeText.ContinueIfInvalidAfterEncountered?.Value)) return parse.Prepend(result);
                            //if (parse != null && ((freeText.ContinueIfInvalidAfterEncountered != null && notContain != freeText.ContinueIfInvalidAfterEncountered.Value) || parse.Length > 0)) return parse.Prepend(result);
                            //if (parse is { Length: > 0 }) return parse.Prepend(result);
                            shouldContinue = freeText.Type is not (FreeTextType.LABEL or FreeTextType.DIGITS);
                            return null;
                        }

                        FreeTextToken CreateToken(int i, int freeTextLength) => new() { Index = i, Length = freeTextLength, Text = input.Substring(i, freeTextLength), Name = freeText.Name, EscapeIndices = acceptedEscapeIndices };

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

        private bool IsLabelChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    }
}
