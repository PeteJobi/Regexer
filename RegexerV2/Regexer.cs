using NCalc;
using NCalc.Exceptions;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static RegexerV2.SyntaxStructure;

namespace RegexerV2
{
    public class Regexer
    {
        private bool _exactWhiteSpace;
        private readonly TimeSpan _regexTimeout;
        private bool hasNewLine, hasMl, hasSol;
        private readonly Parser _parser = new();
        private const string PrefixSpaceLabel = "__space__";
        private readonly StringBuilder patternBuilder = new();
        private readonly Dictionary<string, PatternData> patternMap = new();
        public static readonly RegexerResult CancelledResult = new("Cancelled");
        public static readonly RegexerResult NoMatchesResult = new("No matches found");

        public Regexer()
        {
            _regexTimeout = TimeSpan.FromSeconds(10);
            SetupCyclicRelationships();
        }

        public Regexer(TimeSpan regexTimeout)
        {
            _regexTimeout = regexTimeout;
            SetupCyclicRelationships();
        }

        public void SetExactWhiteSpace(bool exactWhiteSpace) => _exactWhiteSpace = exactWhiteSpace;

        public async Task<RegexerResult> AutoRegexAsync(string input, string find, string replace, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() => AutoRegexInner(input, find, replace, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return CancelledResult;
            }
        }

        private RegexerResult AutoRegexInner(string input, string find, string replace, CancellationToken cancellationToken)
        {
            var patternStructure = _parser.ParsePattern(find, false).ToArray();
            cancellationToken.ThrowIfCancellationRequested();

            patternBuilder.Clear();
            patternMap.Clear();
            matchIndicesMap.Clear();
            hasNewLine = hasMl = hasSol = false;

            ProcessFindFullStructure(find, patternStructure);
            if (hasNewLine || hasMl) patternBuilder.Insert(0, $@"(?<{PrefixSpaceLabel}>[^\S\r\n]*)".AsSpan());
            if(hasSol) patternBuilder.Insert(0, @"(?<=(?:^|\n))".AsSpan());
            find = patternBuilder.ToString();
            var matches = Regex.Matches(input, find, RegexOptions.None, _regexTimeout);
            try
            {
                if (matches.Count == 0) return NoMatchesResult;
            }catch(RegexMatchTimeoutException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            cancellationToken.ThrowIfCancellationRequested();

            patternStructure = [.. _parser.ParsePattern(replace, true)];
            cancellationToken.ThrowIfCancellationRequested();

            patternBuilder.Clear();

            var lastInputMatchEnd = 0;
            var outputOffset = 0;
            var results = new RegexerMatchPair[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var prefixSpaceLength = 0;
                patternBuilder.Append(input.AsSpan(lastInputMatchEnd, matches[i].Index - lastInputMatchEnd));
                if (hasNewLine || hasMl)
                {
                    patternBuilder.Append(matches[i].Groups[PrefixSpaceLabel]);
                    prefixSpaceLength = matches[i].Groups[PrefixSpaceLabel].Length;
                }
                outputOffset += matches[i].Index - lastInputMatchEnd;
                var outputIndieMatches = new List<IndividualMatch>();
                var length = prefixSpaceLength + ProcessReplaceFullStructure(replace, patternStructure, new MatchCaptureData(matches[i], i, matches.Count), outputIndieMatches, outputOffset + prefixSpaceLength);
                cancellationToken.ThrowIfCancellationRequested();
                results[i] = new RegexerMatchPair
                {
                    InputMatch = new RegexerMatch(matches[i].Index, matches[i].Length, matches[i].Value)
                    {
                        IndividualMatches = GetInputIndividualMatches(matches[i], cancellationToken)
                    },
                    OutputMatch = new RegexerMatch(outputOffset, length, patternBuilder.ToString(outputOffset, length))
                    {
                        IndividualMatches = outputIndieMatches
                    }
                };
                lastInputMatchEnd = matches[i].Index + matches[i].Length;
                outputOffset += length;
            }
            patternBuilder.Append(input.AsSpan(lastInputMatchEnd));
            return new RegexerResult(patternBuilder.ToString()) { Matches = results };
            //var result = new RegexerResult { Output = patternBuilder.ToString(), Matches = results };
            //Debug.WriteLine($"Output: {result.Output}");
            //foreach (var pair in result.Matches)
            //{
            //    Debug.WriteLine($"Input: {input.Substring(pair.InputMatch.Index, pair.InputMatch.Length)}, Output: {result.Output.Substring(pair.OutputMatch.Index, pair.OutputMatch.Length)}");
            //    foreach (var match in pair.InputMatch.IndividualMatches)
            //    {
            //        Debug.WriteLine($"  Input Match: {match.Label} -> {string.Join('|', match.Captures.Select(m => input.Substring(m.Index, m.Length)))}");
            //    }
            //    foreach (var match in pair.OutputMatch.IndividualMatches)
            //    {
            //        Debug.WriteLine($"  Output Match: {match.Label} -> {string.Join('|', match.Captures.Select(m => result.Output.Substring(m.Index, m.Length)))}");
            //    }
            //}
            //return result;
        }

        public RegexerResult AutoRegex(string input, string find, string replace) => AutoRegexInner(input, find, replace, CancellationToken.None);

        private void ProcessFindFullStructure(string pattern, IEnumerable<Token> structure)
        {
            foreach (var token in structure)
            {
                switch (token)
                {
                    case FreeTextToken freeTextToken:
                        ProcessFindFreeText(pattern, freeTextToken);
                        break;
                    case ComplexToken complexToken:
                        switch (complexToken.Name)
                        {
                            case TokenName.KeyPattern:
                                ProcessFindKeyPattern(pattern, complexToken);
                                break;
                            case TokenName.Pattern:
                                ProcessFindPatternToken(pattern, complexToken);
                                break;
                            case TokenName.MultiLine:
                                ProcessFindMultiLineToken(pattern, complexToken);
                                break;
                            case TokenName.UnorderedGroup:
                                ProcessFindUnorderedGroupToken(pattern, complexToken);
                                break;
                            default:
                                throw new NotImplementedException($"Unsupported complex token: {complexToken.Name}");
                        }
                        break;
                }
            }
        }

        private int ProcessReplaceFullStructure(string pattern, IEnumerable<Token> structure, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var totalLength = 0;
            foreach (var token in structure)
            {
                int length;
                switch (token)
                {
                    case FreeTextToken freeTextToken:
                        length = ProcessReplaceFreeText(pattern, freeTextToken, matchCaptureData.Match);
                        break;
                    case ComplexToken complexToken:
                        switch (complexToken.Name)
                        {
                            case TokenName.KeyPattern:
                                length = ProcessReplaceKeyPattern(pattern, complexToken, matchCaptureData, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.Pattern:
                                length = ProcessReplacePatternToken(pattern, complexToken, matchCaptureData, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.MatchMultiple:
                                length = ProcessReplaceMatchMultipleToken(pattern, complexToken, matchCaptureData, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.MultiLine:
                                length = ProcessReplaceMultiLineToken(pattern, complexToken, matchCaptureData, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.UnorderedGroup:
                                length = ProcessReplaceUnorderedGroupToken(pattern, complexToken, matchCaptureData, outputIndieMatches, outputOffset);
                                break;
                            default:
                                throw new NotImplementedException($"Unsupported complex token: {complexToken.Name}");
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Unsupported token: {token.Name}");
                }
                outputOffset += length - token.Length;
                totalLength += length;
            }

            return totalLength;
        }

        private readonly StringBuilder spaceBuilder = new();
        private void ProcessFindFreeText(string pattern, FreeTextToken freeTextToken)
        {
            var escapeCount = 0;
            spaceBuilder.Clear();
            for (var i = freeTextToken.Index; i < freeTextToken.Index + freeTextToken.Length; i++)
            {
                if (freeTextToken.EscapeIndices.Count > escapeCount && i == freeTextToken.EscapeIndices[escapeCount])
                {
                    if (escapeCount < freeTextToken.EscapeIndices.Count) escapeCount++;
                    continue;
                }

                var c = pattern[i];
                if(c == '\r' && i < pattern.Length - 1 && pattern[i + 1] == '\n')
                {
                    hasNewLine = true;
                    AppendWhiteSpaceIfAny();
                    patternBuilder.Append(NewLineWithPrefixSpace());
                    i++; //skip \n
                } else if(char.IsWhiteSpace(c))
                {
                    spaceBuilder.Append(c);
                }
                else
                {
                    AppendWhiteSpaceIfAny();
                    var regexEscaped = RegexEscapedChar(c);
                    if (regexEscaped == null) patternBuilder.Append(c);
                    else patternBuilder.Append(regexEscaped);
                }
            }

            AppendWhiteSpaceIfAny();

            static string? RegexEscapedChar(char c)
            {
                return c switch
                {
                    '.' => @"\.",
                    '*' => @"\*",
                    '+' => @"\+",
                    '?' => @"\?",
                    '(' => @"\(",
                    ')' => @"\)",
                    '[' => @"\[",
                    ']' => @"\]",
                    '{' => @"\{",
                    '}' => @"\}",
                    '\\' => @"\\",
                    '^' => @"\^",
                    '$' => @"\$",
                    '|' => @"\|",
                    _ => null
                };
            }

            void AppendWhiteSpaceIfAny()
            {
                if (spaceBuilder.Length == 0) return;
                if (_exactWhiteSpace) patternBuilder.Append(spaceBuilder);
                else patternBuilder.Append(@"[^\S\r\n]+");
                spaceBuilder.Clear();
            }
        }

        private int ProcessReplaceFreeText(string pattern, Token token, Match match)
        {
            var escapeCount = 0;
            var prefixSpaceCount = 0;
            var freeTextToken = token as FreeTextToken;
            for (var i = token.Index; i < token.Index + token.Length; i++)
            {
                if (freeTextToken != null && freeTextToken.EscapeIndices.Count > escapeCount && i == freeTextToken.EscapeIndices[escapeCount])
                {
                    if (escapeCount < freeTextToken.EscapeIndices.Count) escapeCount++;
                    continue;
                }

                var c = pattern[i];
                if(c == '\r' && i < pattern.Length - 1 && pattern[i + 1] == '\n')
                {
                    patternBuilder.AppendLine();
                    if(hasNewLine || hasMl)
                    {
                        patternBuilder.Append(match.Groups[PrefixSpaceLabel].Value);
                        prefixSpaceCount += match.Groups[PrefixSpaceLabel].Length;
                    }
                    i++; //skip \n
                } else
                {
                    patternBuilder.Append(c);
                }
            }

            return token.Length - escapeCount + prefixSpaceCount;
        }

        private void ProcessPlainText(string pattern, FreeTextToken freeTextToken)
        {
            if (freeTextToken.Name != TokenName.PlainText) throw new InvalidOperationException();
            var escapeCount = 0;
            for (var i = freeTextToken.Index; i < freeTextToken.Index + freeTextToken.Length; i++)
            {
                if (freeTextToken.EscapeIndices.Count > escapeCount && i == freeTextToken.EscapeIndices[escapeCount])
                {
                    if (escapeCount < freeTextToken.EscapeIndices.Count) escapeCount++;
                    continue;
                }

                patternBuilder.Append(pattern[i]);
            }
        }

        private ReadOnlySpan<char> NewLineWithPrefixSpace() => $"\r\n\\k<{PrefixSpaceLabel}>".AsSpan();

        private void ProcessFindKeyPattern(string pattern, ComplexToken keyPatternToken)
        {
            var content = pattern.AsSpan(keyPatternToken.Children[1].Index, keyPatternToken.Children[1].Length);
            switch (content)
            {
                case "^":
                    hasSol = true;
                    return;
                case "$":
                    patternBuilder.Append(@"(?=($|\r))");
                    return;
            }
        }

        private readonly IDictionary<int, HashSet<int>> matchIndicesMap = new Dictionary<int, HashSet<int>>();
        private int ProcessReplaceKeyPattern(string pattern, ComplexToken keyPatternToken, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var children = keyPatternToken.Children;
            var content = pattern.AsSpan(children[1].Index, children[1].Length);
            var index = (content is "##" ? matchCaptureData.CaptureIndex : matchCaptureData.MatchIndex) + 1;
            var count = content is "##" ? matchCaptureData.CaptureCount : matchCaptureData.MatchCount;

            if (children.Count == 3) //Simple key pattern with no modifiers
            {
                patternBuilder.Append(index);
                return index.ToString().Length;
            }

            switch (children[2].Name)
            {
                case TokenName.Evaluate:
                    var replacement = Evaluate(pattern, ((ComplexToken)children[2]).Children[^1], index, ReadOnlySpan<char>.Empty);
                    patternBuilder.Append(replacement);
                    return replacement.ToString().Length;
                case TokenName.Addition:
                    var optionalToken = (ComplexToken)children[2];
                    if (!matchIndicesMap.TryGetValue(keyPatternToken.Index, out var matchIndices))
                    {
                        matchIndices = [];
                        matchIndicesMap.Add(keyPatternToken.Index, matchIndices);
                        var paramsToken = (ComplexToken)optionalToken.Children[3];
                        switch (paramsToken.Name)
                        {
                            case TokenName.StartMiddleEnd:
                                foreach (var modifierToken in paramsToken.Children)
                                {
                                    var modifier = pattern.AsSpan(modifierToken.Index, modifierToken.Length);
                                    switch (modifier)
                                    {
                                        case "x":
                                            matchIndices.Add(1);
                                            break;
                                        case "y":
                                            for (var i = 2; i < count; i++)
                                            {
                                                matchIndices.Add(i);
                                            }
                                            break;
                                        case "z":
                                            matchIndices.Add(count);
                                            break;
                                    }
                                }
                                break;
                            case TokenName.Indices:
                                for (var i = 0; i < paramsToken.Children.Count; i += 2)
                                {
                                    var indexToken = paramsToken.Children[i];
                                    var indexSpan = pattern.AsSpan(indexToken.Index, indexToken.Length);
                                    matchIndices.Add(int.Parse(indexSpan));
                                }
                                break;
                            case TokenName.StartIntervalEnd:
                                var startToken = paramsToken.Children[2];
                                var startSpan = pattern.AsSpan(startToken.Index, startToken.Length);
                                var start = int.Parse(startSpan);
                                var interval = 1;
                                var end = count;
                                for (var i = 3; i < paramsToken.Children.Count - 1; i += 4)
                                {
                                    var modSpan = pattern.AsSpan(paramsToken.Children[i + 1].Index, paramsToken.Children[i + 1].Length);
                                    var valueSpan = pattern.AsSpan(paramsToken.Children[i + 3].Index, paramsToken.Children[i + 3].Length);
                                    var value = int.Parse(valueSpan);
                                    if (modSpan is "+") interval = value;
                                    else if (modSpan is "e") end = value;
                                }
                                if(interval == 0) interval = 1;
                                if(end > count) end = count;
                                for (var i = start; i <= end; i += interval)
                                {
                                    matchIndices.Add(i);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (!matchIndices.Contains(index))
                    {
                        for (var i = 5; i < children.Count; i++)
                        {
                            var singleLineToken = (ComplexToken)children[i];
                            foreach (var child in singleLineToken.Children)
                            {
                                if (child.Name != TokenName.Pattern) continue;
                                var labelToken = ((ComplexToken)child).Children[1];
                                var label = pattern.Substring(labelToken.Index, labelToken.Length);
                                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
                                if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, []));
                            }
                        }
                        return 0;
                    }

                    var outputStart = keyPatternToken.Index + outputOffset;
                    var offset = 0;
                    var singleLineStart = optionalToken.Children.Count > 2 ? optionalToken.Children[2].Index : 0;
                    var outputLength = 0;
                    for (var i = 5; i < optionalToken.Children.Count; i++)
                    {
                        var singleLineToken = (ComplexToken)optionalToken.Children[i];
                        var length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, matchCaptureData, outputIndieMatches, outputStart - singleLineStart - offset);
                        offset += singleLineToken.Length - length;
                        outputLength += length;
                    }
                    return outputLength;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessFindPatternToken(string pattern, ComplexToken patternToken)
        {
            string? label = null;
            var labelIndex = 1;
            var elements = patternToken.Children;
            var restriction = @"[^\r\n]".AsSpan();
            var quantifier = "+?".AsSpan();
            PatternData? patternData = null;
            if (elements[1].Name == TokenName.Label)
            {
                label = pattern.Substring(elements[1].Index, elements[1].Length);
                if (patternMap.ContainsKey(label))
                {
                    patternBuilder.Append($"\\k<{label}>");
                    return;
                }
                patternData = new PatternData(label);
                patternMap.Add(label, patternData);
            }
            else //Multi-match or regex pattern with no label
            {
                labelIndex = -1; //1 - label - Demarcate/StartPlainTextSymbol
            }

            if (elements.Count > labelIndex + 2)
            {
                if (elements[labelIndex + 2].Name == TokenName.RestrictionQuantifierNewLine)
                {
                    ProcessRestrictionQuantifierNewLine((ComplexToken)elements[3], ref restriction, ref quantifier);
                    patternBuilder.Append($"(?<{label}>{restriction}{quantifier})");
                }
                else if (elements[labelIndex + 2].Name == TokenName.MatchMultiple)
                {
                    ProcessMatchMultiple((ComplexToken)elements[labelIndex + 2], restriction, quantifier);
                    patternData?.IsMultiMatch = true;
                }
                else if (elements[labelIndex + 2].Name == TokenName.Optional)
                {
                    ProcessOptionalMatch((ComplexToken)elements[labelIndex + 2]);
                }
                else if (elements[labelIndex + 2].Name == TokenName.PlainText)
                {
                    ProcessRegex((FreeTextToken)elements[labelIndex + 2]);
                }
                else if (elements[labelIndex + 2].Name == TokenName.Closer)
                {
                    patternBuilder.Append($"(?<{label}>{restriction}{quantifier})");
                }
                else if (elements[labelIndex + 3].Name == TokenName.PlainText)
                {
                    ProcessRegex((FreeTextToken)elements[labelIndex + 3]);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                patternBuilder.Append($"(?<{label}>{restriction}{quantifier})");
            }
            return;

            void ProcessRestrictionQuantifierNewLine(ComplexToken restrictionQuantifierToken, ref ReadOnlySpan<char> restriction, ref ReadOnlySpan<char> quantifier)
            {
                var modifiers = restrictionQuantifierToken.Children;
                var restrictionChar = '\0';
                var hasL = false;
                foreach (var modifier in modifiers)
                {
                    if (modifier.Name is TokenName.Quantifier or TokenName.ExactAmountQuantifier)
                    {
                        ProcessQuantifier(pattern, (ComplexToken)modifier, ref quantifier);
                    }
                    else
                    {
                        var modifierSpan = pattern.AsSpan(modifier.Index, modifier.Length);
                        switch (modifierSpan)
                        {
                            case "l":
                                hasL = true;
                                continue;
                            case "w":
                                restrictionChar = 'w';
                                continue;
                            case "d":
                                restrictionChar = 'd';
                                patternData!.IsDigit = true;
                                continue;
                            case "s":
                                restrictionChar = 's';
                                continue;
                            default: throw new NotSupportedException($"Unsupported restriction modifier: {modifierSpan.ToString()}");
                        }
                    }
                }

                restriction = restrictionChar switch
                {
                    'w' => (hasL ? @"[\w\r\n]" : "\\w").AsSpan(),
                    'd' => (hasL ? @"[\d\r\n]" : "\\d").AsSpan(),
                    's' => (hasL ? "\\s" : @"[^\S\r\n]").AsSpan(),
                    _ => hasL ? @"[\S\s]" : restriction
                };
            }

            void ProcessMatchMultiple(ComplexToken matchMultipleToken, ReadOnlySpan<char> restriction, ReadOnlySpan<char> quantifier)
            {
                //Basically: $"(?:{singleLineStructure}(?:{separator}{singleLineStructure})quantifierInner)quantifierOuter";
                //Or without separator: $"(?:{singleLineStructure})quantifierOuter";
                var matchMultipleElements = matchMultipleToken.Children;
                patternBuilder.Append("(?:");
                var singleLineStructureStartIndex = patternBuilder.Length;
                if (label != null) patternBuilder.Append($"(?<{label}>");
                var hasSeparator = matchMultipleElements[3].Length > 0;
                var quantifierInner = "*?".AsSpan();
                var quantifierOuter = ReadOnlySpan<char>.Empty;
                var extrasIndex = 6;
                if (matchMultipleElements.Count > extrasIndex)
                {
                    if (matchMultipleElements[extrasIndex].Name is TokenName.Quantifier or TokenName.ExactAmountQuantifier)
                    {
                        if (hasSeparator) ProcessQuantifierWithSeparator(pattern, (ComplexToken)matchMultipleElements[extrasIndex], ref quantifierInner, ref quantifierOuter);
                        else ProcessQuantifier(pattern, (ComplexToken)matchMultipleElements[extrasIndex], ref quantifierOuter);
                        extrasIndex += 2;
                    }
                    if (matchMultipleElements.Count > extrasIndex)
                    {
                        for (var i = extrasIndex; i < matchMultipleElements.Count; i++)
                        {
                            ProcessFindFullStructure(pattern, ((ComplexToken)matchMultipleElements[i]).Children);
                        }
                    }
                    else patternBuilder.Append($"{restriction}{quantifier}");
                }
                else patternBuilder.Append($"{restriction}{quantifier}");
                if (label != null) patternBuilder.Append(')');
                var singleLineStructureLength = patternBuilder.Length - singleLineStructureStartIndex;
                if (hasSeparator)
                {
                    patternBuilder.Append("(?:");
                    ProcessPlainText(pattern, (FreeTextToken)matchMultipleElements[3]);
                    patternBuilder.Append(patternBuilder, singleLineStructureStartIndex, singleLineStructureLength);
                    patternBuilder.Append($"){quantifierInner}");
                }
                patternBuilder.Append($"){quantifierOuter}");
            }

            void ProcessOptionalMatch(ComplexToken optionalToken)
            {
                var matchMultipleElements = optionalToken.Children;
                if (label != null) patternBuilder.Append($"(?<{label}>");
                else patternBuilder.Append("(?:");
                for (var i = 2; i < matchMultipleElements.Count; i++)
                {
                    ProcessFindFullStructure(pattern, ((ComplexToken)matchMultipleElements[i]).Children);
                }
                patternBuilder.Append(")?");
            }

            void ProcessRegex(FreeTextToken regexToken)
            {
                if(label != null) patternBuilder.Append($"(?<{label}>");
                ProcessPlainText(pattern, regexToken);
                if(label != null) patternBuilder.Append(')');
            }
        }

        private static void ProcessQuantifier(string pattern, ComplexToken quantifierToken, ref ReadOnlySpan<char> quantifier)
        {
            switch (quantifierToken.Name)
            {
                case TokenName.Quantifier:
                {
                    var (hasOptional, hasGreedy) = GetQuantifierFlags(pattern, quantifierToken);
                    quantifier = (hasOptional, hasGreedy) switch
                    {
                        (true, false) => "*?".AsSpan(),
                        (false, true) => "+".AsSpan(),
                        (true, true) => "*".AsSpan(),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    break;
                }
                case TokenName.ExactAmountQuantifier:
                {
                    GetQuantifierBounds(pattern, quantifierToken, out var lower, out var upper);
                    quantifier = !upper.IsEmpty ? $"{{{lower},{upper}}}".AsSpan() : $"{{{lower}}}".AsSpan();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ProcessQuantifierWithSeparator(string pattern, ComplexToken quantifierToken, ref ReadOnlySpan<char> quantifierInner, ref ReadOnlySpan<char> quantifierOuter)
        {
            switch (quantifierToken.Name)
            {
                case TokenName.Quantifier:
                {
                    var (hasOptional, hasGreedy) = GetQuantifierFlags(pattern, quantifierToken);
                    switch (hasOptional, hasGreedy)
                    {
                        case (true, false):
                                quantifierInner = "*?".AsSpan();
                                quantifierOuter = "?".AsSpan();
                                break;
                        case (false, true):
                                quantifierInner = "*".AsSpan();
                                quantifierOuter = ReadOnlySpan<char>.Empty;
                                break;
                        case (true, true):
                                quantifierInner = "*".AsSpan();
                                quantifierOuter = "?".AsSpan();
                                break;
                    }
                    break;
                }
                case TokenName.ExactAmountQuantifier:
                {
                    GetQuantifierBounds(pattern, quantifierToken, out var lower, out var upper);
                    var lowerInt = int.Parse(lower);
                    if(lowerInt == 0) quantifierOuter = "?".AsSpan();
                    else
                    {
                        lower = (lowerInt - 1).ToString().AsSpan();
                        quantifierOuter = ReadOnlySpan<char>.Empty;
                    }
                    if (!upper.IsEmpty)
                    {
                        upper = int.Max(0, int.Parse(upper) - 1).ToString().AsSpan();
                        quantifierInner = $"{{{lower},{upper}}}".AsSpan();
                    }
                    else
                    {
                        if (lowerInt == 0) quantifierOuter = $"{{{lower}}}".AsSpan();
                        quantifierInner = $"{{{lower}}}".AsSpan();
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static (bool Optional, bool Greedy) GetQuantifierFlags(string pattern, ComplexToken quantifierToken)
        {
            var hasGreedy = false;
            var hasOptional = false;

            foreach (var mod in quantifierToken.Children)
            {
                var modText = pattern.AsSpan(mod.Index, mod.Length);
                switch (modText)
                {
                    case "g":
                        hasGreedy = true;
                        continue;
                    case "o":
                        hasOptional = true;
                        continue;
                }
            }

            return (hasOptional, hasGreedy);
        }

        private static void GetQuantifierBounds(string pattern, ComplexToken quantifierToken, out ReadOnlySpan<char> lower, out ReadOnlySpan<char> upper)
        {
            lower = pattern.AsSpan(quantifierToken.Children[1].Index, quantifierToken.Children[1].Length);
            upper = quantifierToken.Children.Count > 3 
                ? pattern.AsSpan(quantifierToken.Children[3].Index, quantifierToken.Children[3].Length)
                : ReadOnlySpan<char>.Empty;
        }

        private readonly StringBuilder _evalBuilder = new();
        private int Evaluate(string pattern, Token token, int i, ReadOnlySpan<char> m)
        {
            _evalBuilder.Clear();
            for (var j = token.Index; j < token.Index + token.Length; j++)
            {
                var symbol = pattern[j];
                if (symbol == 'i') _evalBuilder.Append(i + 1);
                else if (symbol == 'm') _evalBuilder.Append(int.Parse(m));
                else _evalBuilder.Append(symbol);
            }

            return EvaluateString(_evalBuilder.ToString());
        }
        private static int EvaluateString(string expression)
        {
            try
            {
                var expr = new Expression(expression);
                return expr.Evaluate<int>();
            }
            catch (NCalcEvaluationException e)
            {
                throw new ArithmeticException($"Expression \"{expression}\" could not be evaluated.", e);
            }
        }

        private int ProcessReplacePatternToken(string pattern, ComplexToken patternToken, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var elements = patternToken.Children;
            var label = pattern.Substring(elements[1].Index, elements[1].Length);
            if (!patternMap.TryGetValue(label, out var patternData))
            {
                patternBuilder.Append(pattern.AsSpan(patternToken.Index, patternToken.Length)); //Use the pattern text as is
                return patternToken.Length;
            }

            var outputCaptures = new List<MatchData>();
            var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, outputCaptures));
            else outputCaptures = outputIndieMatches[n.Index].Captures;

            if (elements.Count <= 3) //Simple pattern with no transform
            {
                if (!TryGetCapture(out var capture)) return 0;
                patternBuilder.Append(capture.ValueSpan);
                if(capture.Length > 0) outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, capture.Length, capture.Value));
                return capture.Length;
            }

            var children = ((ComplexToken)elements[3]).Children;
            int length;
            switch (elements[3].Name)
            {
                case TokenName.Optional:
                    var modifier = pattern.AsSpan(children[0].Index, children[0].Length);

                    if ((modifier is not "o" || !TryGetCapture(out var capture) || capture.Value == string.Empty) && (modifier is not "oi" || (TryGetCapture(out capture) && capture.Value != string.Empty)))
                    {
                        for (var i = 2; i < children.Count - 1; i++)
                        {
                            var singleLineToken = (ComplexToken)children[i];
                            foreach (var child in singleLineToken.Children)
                            {
                                if (child.Name != TokenName.Pattern) continue;
                                var labelToken = ((ComplexToken)child).Children[1];
                                label = pattern.Substring(labelToken.Index, labelToken.Length);
                                n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
                                if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, []));
                            }
                        }
                        return 0;
                    }

                    var outputMatchData = new MatchData(0, 0, string.Empty);
                    outputCaptures.Add(outputMatchData);
                    outputMatchData.Index = patternToken.Index + outputOffset;
                    var offset = 0;
                    var singleLineStart = children.Count > 2 ? children[2].Index : 0;
                    for (var i = 2; i < children.Count; i++)
                    {
                        var singleLineToken = (ComplexToken)children[i];
                        length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, matchCaptureData, outputIndieMatches, outputMatchData.Index - singleLineStart - offset);
                        offset += singleLineToken.Length - length;
                        outputMatchData.Length += length;
                    }
                    outputMatchData.Text = patternBuilder.ToString(outputMatchData.Index, outputMatchData.Length);
                    return outputMatchData.Length;

                case TokenName.Duplicate:
                    _evalBuilder.Clear();
                    for (var i = children[2].Index; i < children[2].Index + children[2].Length; i++)
                    {
                        var symbol = pattern[i];
                        if (symbol == 'i') _evalBuilder.Append(matchCaptureData.MatchIndex + 1);
                        else _evalBuilder.Append(symbol);
                    }

                    var duplicationAmount = EvaluateByte(_evalBuilder.ToString());
                    if (duplicationAmount == 0 || !TryGetCapture(out capture)) return 0;
                    patternBuilder.Append(capture.ValueSpan);
                    length = capture.Length;
                    if (duplicationAmount == 1)
                    {
                        outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length, capture.Value));
                        return length;
                    }

                    var separatorStart = patternToken.Index + outputOffset + capture.Length;
                    var separatorLength = 0;
                    if (children.Count > 4)
                    {
                        separatorLength = ProcessReplaceFreeText(pattern, (FreeTextToken)children[4], matchCaptureData.Match);
                        length += separatorLength;
                    }
                    patternBuilder.Append(capture.ValueSpan);
                    length += capture.Length;

                    for (var i = 0; i < duplicationAmount - 2; i++)
                    {
                        patternBuilder.Append(patternBuilder, separatorStart, separatorLength);
                        length += separatorLength;
                        patternBuilder.Append(capture.ValueSpan);
                        length += capture.Length;
                    }

                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length, patternBuilder.ToString(patternToken.Index + outputOffset, length)));
                    return length;

                case TokenName.Capitalize:
                    var capitalizeModifier = pattern.AsSpan(children[2].Index, children[2].Length);
                    if (!TryGetCapture(out capture)) return 0;
                    patternBuilder.Append(capitalizeModifier switch
                    {
                        "u" => capture.Value.ToUpper(), //u: Upper case
                        "l" => capture.Value.ToLower(), //l: Lower case
                        "fu" => capture.Value[..1].ToUpper() + capture.Value[1..], //fu: First letter upper case
                        "fl" => capture.Value[..1].ToLower() + capture.Value[1..], //fl: First letter lower case
                        _ => capture.Value[..1].ToUpper() + capture.Value[1..].ToLower() //s: Sentence case
                    });
                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, capture.Length,
                        patternBuilder.ToString(patternToken.Index + outputOffset, capture.Length)));
                    return capture.Length;

                case TokenName.Evaluate:
                    if (!TryGetCapture(out capture)) return 0;
                    var replacement = Evaluate(pattern, children[2], matchCaptureData.MatchIndex, capture.ValueSpan);
                    patternBuilder.Append(replacement);
                    length = replacement.ToString().Length;
                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length,
                        patternBuilder.ToString(patternToken.Index + outputOffset, length)));
                    return length;
            }

            return 0;

            bool TryGetCapture(out Capture capture)
            {
                var captures = matchCaptureData.Match.Groups[label].Captures;
                if (matchCaptureData.CaptureIndex >= captures.Count)
                {
                    capture = default;
                    return false;
                }
                capture = captures[matchCaptureData.CaptureIndex];
                return true;
            }

            static byte EvaluateByte(string expression)
            {
                var evaluation = EvaluateString(expression);
                if (evaluation > byte.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("The maximum amount of duplications allowed is 255.", default(Exception));
                }

                return (byte)evaluation;
            }
        }

        private int ProcessReplaceMatchMultipleToken(string pattern, ComplexToken patternToken, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var elements = patternToken.Children;
            var label = pattern.Substring(elements[1].Index, elements[1].Length);
            if (!patternMap.TryGetValue(label, out var patternData))
            {
                patternBuilder.Append(pattern.AsSpan(patternToken.Index, patternToken.Length)); //Use the pattern text as is
                return patternToken.Length;
            }

            var outputCaptures = new List<MatchData>();
            var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, outputCaptures));
            else outputCaptures = outputIndieMatches[n.Index].Captures;

            var inputCaptures = matchCaptureData.Match.Groups[label].Captures;
            const int sortIndex = 6;
            var singleLineIndex = 7;
            int[] order = [.. Enumerable.Range(0, inputCaptures.Count)];
            var children = patternToken.Children;
            if (children.Count > sortIndex && children[sortIndex].Name == TokenName.MatchMultipleReorder)
            {
                singleLineIndex = 8;
                GetMatchMultipleOrderIndices((ComplexToken)children[sortIndex], matchCaptureData, label, ref order);
            }

            var totalLength = 0;
            var outputStart = patternToken.Index + outputOffset;
            foreach (var s in order)
            {
                var outputMatchData = new MatchData(outputStart + totalLength, 0, string.Empty);
                outputCaptures.Add(outputMatchData);
                if (children.Count > singleLineIndex)
                {
                    var offset = 0;
                    for (var i = singleLineIndex; i < children.Count - 1; i++)
                    {
                        var singleLineToken = (ComplexToken)children[i];
                        var length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, matchCaptureData with { CaptureIndex = s, CaptureCount = inputCaptures.Count },
                            outputIndieMatches, outputMatchData.Index - children[singleLineIndex].Index - offset);
                        offset += singleLineToken.Length - length;
                        outputMatchData.Length += length;
                    }
                }
                else
                {
                    patternBuilder.Append(inputCaptures[s]);
                    outputMatchData.Length += inputCaptures[s].Length;
                }

                outputMatchData.Text = patternBuilder.ToString(outputMatchData.Index, outputMatchData.Length);
                totalLength += outputMatchData.Length;

                if (s == order[^1]) break;
                totalLength += ProcessReplaceFreeText(pattern, (FreeTextToken)children[5], matchCaptureData.Match);
            }

            return totalLength;
        }

        private void GetMatchMultipleOrderIndices(ComplexToken reorderToken, MatchCaptureData matchCaptureData, string patternLabel, ref int[] order)
        {
            var sortParamsTokens = reorderToken.Children;
            var paramCaptures = new List<CaptureCollection>();
            var sortParams = new List<(int ArrayIndex, bool Ascending, bool IsDigit)>();
            for (var i = 1; i < sortParamsTokens.Count; i += 2)
            {
                if (sortParamsTokens[i].Name == TokenName.MatchMultipleSortByCapture && sortParamsTokens[i] is ComplexToken complexToken)
                {
                    var paramTokens = complexToken.Children;
                    string captureLabel;
                    if (paramTokens.Count > 2)
                    {
                        if (!matchCaptureData.Match.Groups.ContainsKey(paramTokens[2].Text)) continue;
                        captureLabel = paramTokens[2].Text;
                    }
                    else captureLabel = patternLabel;
                    paramCaptures.Add(matchCaptureData.Match.Groups[captureLabel].Captures);
                    sortParams.Add((sortParams.Count, paramTokens[0].Text == ">", patternMap[captureLabel].IsDigit));
                }
                else
                {
                    Array.Reverse(order);
                    break;
                }
            }

            if (paramCaptures.Count > 0)
            {
                order = SortMultiCriteria(paramCaptures, sortParams, matchCaptureData.Match.Groups[patternLabel].Captures.Count);
            }
        }

        private void ProcessFindMultiLineToken(string pattern, ComplexToken multiLineToken)
        {
            //Basically: $"(?:{precedingSpace}{singleLineStructure}(?:{NewLineWithPrefixSpace()}{precedingSpace}{singleLineStructure})quantifierInner)quantifierOuter";
            //Or with preceding new line: $"(?:{NewLineWithPrefixSpace()}{precedingSpace}{singleLineStructure})quantifierOuter";
            var elements = multiLineToken.Children;
            var label = pattern.Substring(elements[2].Index, elements[2].Length);
            if (patternMap.ContainsKey(label))
            {
                patternBuilder.Append($"\\k<{label}Lines>");
                return;
            }
            patternMap.Add(label, new PatternData(label){ IsMultiLine = true });
            var spaceStartIndexOffset = pattern[elements[0].Index] == '\r' && pattern[elements[0].Index + 1] == '\n' ? 2 : 0;
            var precedingSpace = pattern.AsSpan(elements[0].Index + spaceStartIndexOffset, elements[0].Length - spaceStartIndexOffset);
            var hasPrecedingNewLine = spaceStartIndexOffset != 0;

            patternBuilder.Append("(?:");
            if (!hasPrecedingNewLine) patternBuilder.Append("(?<=(?:^|\\n))"); //See MultiLineGreedy Test
            var singleLineStructureStartIndex = patternBuilder.Length;
            if (hasPrecedingNewLine) patternBuilder.Append(NewLineWithPrefixSpace()).Append(precedingSpace);
            else patternBuilder.Append(precedingSpace); //At start of pattern, no need to match a new line before the first line
            patternBuilder.Append($"(?<{label}Lines>");
            var restriction = @"[^\r\n]".AsSpan();
            var quantifier = "*".AsSpan();
            var quantifierInner = "*?".AsSpan();
            var quantifierOuter = hasPrecedingNewLine ? "+?" : ReadOnlySpan<char>.Empty;
            var extrasIndex = 6;
            if (elements.Count - 2 > extrasIndex)
            {
                if (elements[extrasIndex].Name is TokenName.Quantifier or TokenName.ExactAmountQuantifier)
                {
                    if (hasPrecedingNewLine) ProcessQuantifier(pattern, (ComplexToken)elements[extrasIndex], ref quantifierOuter);
                    else ProcessQuantifierWithSeparator(pattern, (ComplexToken)elements[extrasIndex], ref quantifierInner, ref quantifierOuter);
                    extrasIndex += 2;
                }
                if (elements.Count - 2 > extrasIndex)
                {
                    for (var i = extrasIndex; i < elements.Count - 2; i++)
                    {
                        ProcessFindFullStructure(pattern, ((ComplexToken)elements[i]).Children);
                    }
                }
                else patternBuilder.Append($"{restriction}{quantifier}");
            }
            else patternBuilder.Append($"{restriction}{quantifier}");
            patternBuilder.Append(')');
            var singleLineStructureLength = patternBuilder.Length - singleLineStructureStartIndex;
            if (!hasPrecedingNewLine)
            {
                patternBuilder.Append("(?:").Append(NewLineWithPrefixSpace()).Append(precedingSpace);
                patternBuilder.Append(patternBuilder, singleLineStructureStartIndex, singleLineStructureLength);
                patternBuilder.Append($"){quantifierInner}");
            }
            patternBuilder.Append($"){quantifierOuter}");

            hasMl = true;
        }

        private int ProcessReplaceMultiLineToken(string pattern, ComplexToken multiLineToken, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var children = multiLineToken.Children;
            var label = pattern.Substring(children[3].Index, children[3].Length);
            if (!patternMap.TryGetValue(label, out var patternData) || !patternData.IsMultiLine)
            {
                patternBuilder.Append(pattern.AsSpan(multiLineToken.Index, multiLineToken.Length)); //Use the pattern text as is
                return multiLineToken.Length;
            }

            var lineCaptures = matchCaptureData.Match.Groups[$"{label}Lines"].Captures;

            var outputCaptures = new List<MatchData>();
            var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, outputCaptures));
            else outputCaptures = outputIndieMatches[n.Index].Captures;

            const int sortIndex = 6;
            var singleLineIndex = 7;
            int[] order = [.. Enumerable.Range(0, lineCaptures.Count)];
            if (children.Count > sortIndex && children[sortIndex].Name == TokenName.MatchMultipleReorder)
            {
                singleLineIndex = 8;
                GetMatchMultipleOrderIndices((ComplexToken)children[sortIndex], matchCaptureData, $"{label}Lines", ref order);
            }

            var spaceStartIndexOffset = pattern[children[0].Index] == '\r' && pattern[children[0].Index + 1] == '\n' ? 2 : 0;
            var precedingSpace = pattern.AsSpan(children[0].Index + spaceStartIndexOffset, children[0].Length - spaceStartIndexOffset);

            var totalLength = 0;
            foreach (var s in order)
            {
                var whiteSpaceLength = 0;
                if (s != order[0] || spaceStartIndexOffset > 0)
                {
                    patternBuilder.AppendLine();
                    whiteSpaceLength += 2;
                }
                if (hasNewLine || hasMl)
                {
                    patternBuilder.Append(matchCaptureData.Match.Groups[PrefixSpaceLabel].Value);
                    whiteSpaceLength += matchCaptureData.Match.Groups[PrefixSpaceLabel].Length;
                }
                patternBuilder.Append(precedingSpace);
                whiteSpaceLength += precedingSpace.Length;
                var outputMatchData = new MatchData(totalLength + multiLineToken.Index + whiteSpaceLength + outputOffset, 0, string.Empty);
                outputCaptures.Add(outputMatchData);
                outputMatchData.Length += ProcessReplaceFreeText(pattern, (FreeTextToken)children[1], matchCaptureData.Match);

                if (children.Count - 3 > singleLineIndex)
                {
                    var offset = -outputMatchData.Length;
                    for (var i = singleLineIndex; i < children.Count - 3; i++)
                    {
                        var singleLineToken = (ComplexToken)children[i];
                        var length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, matchCaptureData with { CaptureIndex = s, CaptureCount = lineCaptures.Count },
                            outputIndieMatches, outputMatchData.Index - children[singleLineIndex].Index - offset);
                        offset += singleLineToken.Length - length;
                        outputMatchData.Length += length;
                    }
                }
                else
                {
                    patternBuilder.Append(lineCaptures[s]);
                    outputMatchData.Length += lineCaptures[s].Length;
                }

                outputMatchData.Length += ProcessReplaceFreeText(pattern, (FreeTextToken)children[^2], matchCaptureData.Match);
                outputMatchData.Text = patternBuilder.ToString(outputMatchData.Index, outputMatchData.Length);
                patternBuilder.Append(pattern.AsSpan(children[^1].Index, children[^1].Length));
                whiteSpaceLength += children[^1].Length;
                totalLength += outputMatchData.Length + whiteSpaceLength;
            }

            return totalLength;
        }

        private readonly StringBuilder tempPatternBuilder = new();
        private void ProcessFindUnorderedGroupToken(string pattern, ComplexToken unorderedGroupToken)
        {
            var inAnyOrder = new StringBuilder();
            var noDuplicates = new StringBuilder("(?!.*(");
            var nothingElseBesidesThem = new StringBuilder("(");

            for (var i = 0; i < unorderedGroupToken.Children.Count; i++)
            {
                var unorderedElements = ((ComplexToken)unorderedGroupToken.Children[i]).Children;
                var label = unorderedElements[2].Name == TokenName.Label
                    ? pattern.Substring(unorderedElements[2].Index, unorderedElements[2].Length)
                    : null;

                int singleLineTokenIndex;
                if (label != null)
                {
                    if (patternMap.ContainsKey(label))
                    {
                        patternBuilder.Append($"\\k<{label}>?");
                        continue;
                    }

                    patternMap.Add(label, new PatternData(label) { IsUnordered = true });
                    singleLineTokenIndex = 6;
                }
                else singleLineTokenIndex = 4;
                var singleLinePatternIndex = patternBuilder.Length;
                for (var j = singleLineTokenIndex; j < unorderedElements.Count - 1; j++)
                {
                    ProcessFindFullStructure(pattern, ((ComplexToken)unorderedElements[j]).Children);
                }
                tempPatternBuilder.Clear();
                tempPatternBuilder.Append(patternBuilder, singleLinePatternIndex, patternBuilder.Length - singleLinePatternIndex);
                patternBuilder.Remove(singleLinePatternIndex, patternBuilder.Length - singleLinePatternIndex);
                inAnyOrder.Append("(?=.*(").Append(tempPatternBuilder).Append(")?)");
                noDuplicates.Append("\\s*").Append(tempPatternBuilder);
                nothingElseBesidesThem.Append("\\s*");
                if (label == null) nothingElseBesidesThem.Append(tempPatternBuilder);
                else nothingElseBesidesThem.Append($"(?<{label}>").Append(tempPatternBuilder).Append(')');
                if (i < unorderedGroupToken.Children.Count - 1)
                {
                    noDuplicates.Append('|');
                    nothingElseBesidesThem.Append('|');
                }
            }
            noDuplicates.Append(")+.*\\1)");
            nothingElseBesidesThem.Append(")*");

            patternBuilder.Append(inAnyOrder).Append(noDuplicates).Append(nothingElseBesidesThem)/*.Append(@"(?:\r\n)?")*/;
        }

        private int ProcessReplaceUnorderedGroupToken(string pattern, ComplexToken unorderedGroupToken, MatchCaptureData matchCaptureData, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var totalLength = 0;
            var outputStart = unorderedGroupToken.Index + outputOffset;
            Token? firstEntryWhitespace = null;
            for (var i = 0; i < unorderedGroupToken.Children.Count; i++)
            {
                var unorderedElements = ((ComplexToken)unorderedGroupToken.Children[i]).Children;
                var label = pattern.Substring(unorderedElements[2].Index, unorderedElements[2].Length);
                if (!patternMap.TryGetValue(label, out var patternData) || !patternData.IsUnordered)
                {
                    patternBuilder.Append(pattern.AsSpan(unorderedGroupToken.Children[i].Index, unorderedGroupToken.Children[i].Length)); //Use the pattern text as is
                    totalLength += unorderedGroupToken.Children[i].Length;
                    continue;
                }

                var inputMatch = matchCaptureData.Match.Groups[label];
                var outputMatchData = new MatchData(0, 0, string.Empty);
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
                if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, [outputMatchData]));
                else outputIndieMatches[n.Index].Captures.Add(outputMatchData);

                var modifier = pattern.AsSpan(unorderedElements[4].Index, unorderedElements[4].Length);
                if ((modifier is not "u" || !inputMatch.Success) && (modifier is not "ui" || inputMatch.Success))
                {
                    if(i == 0) firstEntryWhitespace = unorderedElements[0];
                    outputOffset -= unorderedGroupToken.Children[i].Length;
                    for (var j = 6; j < unorderedElements.Count - 1; j++)
                    {
                        var singleLineToken = (ComplexToken)unorderedElements[j];
                        foreach (var child in singleLineToken.Children)
                        {
                            if(child.Name != TokenName.Pattern) continue;
                            var labelToken = ((ComplexToken)child).Children[1];
                            label = pattern.Substring(labelToken.Index, labelToken.Length);
                            n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
                            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, []));
                        }
                    }
                    continue;
                }
                var whitespaceLength = ProcessReplaceFreeText(pattern, firstEntryWhitespace ?? unorderedElements[0], matchCaptureData.Match);
                firstEntryWhitespace = null;
                outputMatchData.Index = outputStart + totalLength + whitespaceLength;
                if (unorderedElements.Count > 6)
                {
                    var offset = 0;
                    var singleLineStart = unorderedElements[6].Index;
                    for (var j = 6; j < unorderedElements.Count - 1; j++)
                    {
                        var singleLineToken = (ComplexToken)unorderedElements[j];
                        var length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, matchCaptureData, outputIndieMatches, outputMatchData.Index - singleLineStart - offset);
                        offset += singleLineToken.Length - length;
                        outputMatchData.Length += length;
                    }
                }
                else
                {
                    patternBuilder.Append(inputMatch.Value);
                    outputMatchData.Length += inputMatch.Length;
                }
                outputMatchData.Text = patternBuilder.ToString(outputMatchData.Index, outputMatchData.Length);
                totalLength += outputMatchData.Length + whitespaceLength;
                outputOffset -= unorderedGroupToken.Children[i].Length - outputMatchData.Length - whitespaceLength;
            }

            return totalLength;
        }

        private List<IndividualMatch> GetInputIndividualMatches(Match match, CancellationToken cancellationToken)
        {
            var individualMatches = new List<IndividualMatch>();
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var label in patternMap.Keys)
            {
                var n = GetAlphabeticalOrderIndex(individualMatches, l => l.Label, label);
                var patternData = patternMap[label];
                var captures = new List<MatchData>();
                if (patternData.IsMultiLine)
                {
                    var lines = match.Groups[$"{label}Lines"].Captures;
                    for (var i = 0; i < lines.Count; i++)
                    {
                        captures.Add(new MatchData(lines[i].Index, lines[i].Length, lines[i].Value));
                    }
                    individualMatches.Insert(n.Index, new IndividualMatch(label, captures));
                    continue;
                }

                captures = [];
                var matchCaptures = match.Groups[label].Captures;
                for (var i = 0; i < matchCaptures.Count; i++)
                {
                    if(matchCaptures[i].Length > 0) captures.Add(new MatchData(matchCaptures[i].Index, matchCaptures[i].Length, matchCaptures[i].Value));
                }
                individualMatches.Insert(n.Index, new IndividualMatch(label, captures));
                cancellationToken.ThrowIfCancellationRequested();
            }

            return individualMatches;
        }

        private static (int Index, bool DidNotExist) GetAlphabeticalOrderIndex<T>(List<T> list, Func<T, string> path, string value)
        {
            if (list.Count == 0)
                return (0, true);

            int left = 0, right = list.Count - 1;
            var lastMatchIndex = -1;

            // Binary search to find the value or its insertion point
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var comparison = string.Compare(path(list[mid]), value, StringComparison.Ordinal);

                switch (comparison)
                {
                    case 0:
                        lastMatchIndex = mid;
                        left = mid + 1; // Continue searching to the right for the last occurrence
                        break;
                    case < 0:
                        left = mid + 1;
                        break;
                    default:
                        right = mid - 1;
                        break;
                }
            }

            // If found, return the index of the last match
            if (lastMatchIndex != -1) return (lastMatchIndex, false);

            // If not found, return the insertion point (left position)
            return (left, true);
        }

        private static int[] SortMultiCriteria(List<CaptureCollection> captureCollections, List<(int ArrayIndex, bool Ascending, bool IsDigit)> criteria, int length) //Initial code by Gemini Auto
        {
            // Allocate the index array once
            var indices = new int[length];
            for (var i = 0; i < length; i++)
            {
                indices[i] = i;
            }

            // Sort the indices using Array.Sort with an inline comparison delegate
            Array.Sort(indices, (i1, i2) =>
            {
                foreach (var (arrIdx, ascending, isDigit) in criteria)
                {
                    var targetArray = captureCollections[arrIdx];
                    var val1 = targetArray[i1].Value;
                    var val2 = targetArray[i2].Value;

                    var comparison = isDigit 
                        ? int.Parse(val1).CompareTo(int.Parse(val2))
                        : string.Compare(val1, val2, StringComparison.Ordinal);

                    if (comparison != 0) return ascending ? comparison : -comparison;
                }

                return 0; // All criteria matched; preserve original relative order
            });

            return indices;
        }

        private class PatternData(string label)
        {
            public string Label { get; set; } = label;
            public bool IsUnordered { get; set; }
            public bool IsDigit { get; set; }
            public bool IsMultiLine { get; set; }
            public bool IsMultiMatch { get; set; }
        }

        public class Parser
        {
            private readonly SyntaxParser _parser = new();
            private readonly Structure[] _findStructureArray = [FindStructure];
            private readonly Structure[] _replaceStructureArray = [ReplaceStructure];

            public IEnumerable<Token> ParsePattern(string pattern, bool isReplace)
            {
                return _parser.ParsePatternWithSuggestions(pattern, 0, pattern.Length, 0, isReplace ? _replaceStructureArray : _findStructureArray, 0, false).AlreadyTyped!;
            }

            public IEnumerable<Token> ParsePattern(string pattern, int patternStart, int patternEnd, bool isReplace)
            {
                return _parser.ParsePatternWithSuggestions(pattern, patternStart, patternEnd, patternStart, isReplace ? _replaceStructureArray : _findStructureArray, 0, false).AlreadyTyped!;
            }

            public (IEnumerable<Token>? AlreadyTyped, List<SuggestionToken> Suggestions) ParsePatternWithSuggestions(string pattern, bool isReplace)
            {
                return _parser.ParsePatternWithSuggestions(pattern, 0, pattern.Length, 0, isReplace ? _replaceStructureArray : _findStructureArray, 0, true);
            }

            public (IEnumerable<Token>? AlreadyTyped, List<SuggestionToken> Suggestions) ParsePatternWithSuggestions(string pattern, int patternStart, int patternEnd, bool isReplace)
            {
                return _parser.ParsePatternWithSuggestions(pattern, patternStart, patternEnd, patternStart, isReplace ? _replaceStructureArray : _findStructureArray, 0, true);
            }
        }
    }
    public class RegexerResult(string output)
    {
        public string Output { get; set; } = output;
        public RegexerMatchPair[]? Matches { get; set; }
    }

    public class RegexerMatchPair
    {
        public required RegexerMatch InputMatch { get; set; }
        public required RegexerMatch OutputMatch { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not RegexerMatchPair pair) return false;
            if (InputMatch.Index != pair.InputMatch.Index) return false;
            if (InputMatch.Length != pair.InputMatch.Length) return false;
            if (InputMatch.Text != pair.InputMatch.Text) return false;
            if (OutputMatch.Index != pair.OutputMatch.Index) return false;
            if (OutputMatch.Length != pair.OutputMatch.Length) return false;
            if (OutputMatch.Text != pair.OutputMatch.Text) return false;
            if (InputMatch.IndividualMatches.Count != pair.InputMatch.IndividualMatches.Count) return false;
            if (OutputMatch.IndividualMatches.Count != pair.OutputMatch.IndividualMatches.Count) return false;
            for (var i = 0; i < InputMatch.IndividualMatches.Count; i++)
            {
                if (InputMatch.IndividualMatches[i].Label != pair.InputMatch.IndividualMatches[i].Label) return false;
                if (InputMatch.IndividualMatches[i].Captures.Count != pair.InputMatch.IndividualMatches[i].Captures.Count) return false;
                for (var j = 0; j < InputMatch.IndividualMatches[i].Captures.Count; j++)
                {
                    if (InputMatch.IndividualMatches[i].Captures[j].Index != pair.InputMatch.IndividualMatches[i].Captures[j].Index) return false;
                    if (InputMatch.IndividualMatches[i].Captures[j].Length != pair.InputMatch.IndividualMatches[i].Captures[j].Length) return false;
                    if (InputMatch.IndividualMatches[i].Captures[j].Text != pair.InputMatch.IndividualMatches[i].Captures[j].Text) return false;
                }
            }
            for (var i = 0; i < OutputMatch.IndividualMatches.Count; i++)
            {
                if (OutputMatch.IndividualMatches[i].Label != pair.OutputMatch.IndividualMatches[i].Label) return false;
                if (OutputMatch.IndividualMatches[i].Captures.Count != pair.OutputMatch.IndividualMatches[i].Captures.Count) return false;
                for (var j = 0; j < OutputMatch.IndividualMatches[i].Captures.Count; j++)
                {
                    if (OutputMatch.IndividualMatches[i].Captures[j].Index != pair.OutputMatch.IndividualMatches[i].Captures[j].Index) return false;
                    if (OutputMatch.IndividualMatches[i].Captures[j].Length != pair.OutputMatch.IndividualMatches[i].Captures[j].Length) return false;
                    if (OutputMatch.IndividualMatches[i].Captures[j].Text != pair.OutputMatch.IndividualMatches[i].Captures[j].Text) return false;
                }
            }
            return true;
        }
    }

    public class MatchData(int index, int length, string text)
    {
        public int Index { get; set; } = index;
        public int Length { get; set; } = length;
        public string Text { get; set; } = text;

        public override string ToString()
        {
            return $"{Index}, {Length}, {Text}";
        }
    }

    public class RegexerMatch(int index, int length, string text) : MatchData(index, length, text)
    {
        public required List<IndividualMatch> IndividualMatches { get; set; }

        //public override string ToString()
        //{
        //    return $"{Index}, {Length}, {string.Join(", ", IndividualMatches.Select(i => $"<{i}>"))}";
        //}
    }

    public class IndividualMatch(string label, List<MatchData> captures)
    {
        public string Label { get; set; } = label;
        public List<MatchData> Captures { get; set; } = captures;

        public override string ToString()
        {
            return $"{Label} -> {string.Join(", ", Captures.Select(c => $"[{c}]"))}";
        }
    }

    internal struct MatchCaptureData(Match match, int matchIndex, int captureIndex, int matchCount, int captureCount)
    {
        internal MatchCaptureData(Match match, int matchIndex, int matchCount): this(match, matchIndex, 0, matchCount, 1){}

        public Match Match { get; set; } = match;
        public int MatchIndex { get; set; } = matchIndex;
        public int CaptureIndex { get; set; } = captureIndex;
        public int MatchCount { get; set; } = matchCount;
        public int CaptureCount { get; set; } = captureCount;
    }
}