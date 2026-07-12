using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using NCalc;
using NCalc.Exceptions;
using static RegexerV2.SyntaxStructure;

namespace RegexerV2
{
    public class Regexer
    {
        private readonly bool exactWhiteSpace;
        private readonly TimeSpan _regexTimeout;
        private bool hasNewLine, hasMl;
        private readonly SyntaxParser parser = new();
        private const string PrefixSpaceLabel = "__space__";
        private readonly StringBuilder patternBuilder = new();
        private readonly Dictionary<string, PatternData> patternMap = new();

        public Regexer()
        {
            _regexTimeout = TimeSpan.FromSeconds(10);
            SetupCyclicRelationships();
        }

        public Regexer(bool exactWhiteSpace, TimeSpan regexTimeout)
        {
            this.exactWhiteSpace = exactWhiteSpace;
            _regexTimeout = regexTimeout;
            SetupCyclicRelationships();
        }

        public async Task<RegexerResult> AutoRegex(string input, string find, string replace, CancellationToken cancellationToken)
        {
            return await await Task.WhenAny(
                Cancel(cancellationToken),
                Task.Run(() => AutoRegex(input, find, replace), cancellationToken));
        }

        private async Task<RegexerResult> Cancel(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (TaskCanceledException) { }
            return new RegexerResult { Output = "Cancelled" };
        }

        public RegexerResult AutoRegex(string input, string find, string replace)
        {
            var patternStructure = parser.ParsePattern(find, 0, [FindStructure], 0);

            patternBuilder.Clear();
            patternMap.Clear();
            hasNewLine = hasMl = false;
            ProcessFindFullStructure(find, patternStructure!);
            if (!exactWhiteSpace && (hasNewLine || hasMl)) patternBuilder.Insert(0, $@"(?<{PrefixSpaceLabel}>^[^\S\r\n]*)".AsSpan());
            find = patternBuilder.ToString();
            var matches = Regex.Matches(input, find, RegexOptions.Singleline | RegexOptions.Multiline, _regexTimeout);
            if (matches.Count == 0) return new RegexerResult { Output = input };

            patternStructure = parser.ParsePattern(replace, 0, [ReplaceStructure], 0);

            patternBuilder.Clear();
            var patternStructureArray = patternStructure as Token[] ?? patternStructure!.ToArray();

            var lastInputMatchEnd = 0;
            var outputOffset = 0;
            var results = new RegexerMatchPair[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                var prefixSpaceLength = 0;
                patternBuilder.Append(input.AsSpan(lastInputMatchEnd, matches[i].Index - lastInputMatchEnd));
                if (!exactWhiteSpace)
                {
                    patternBuilder.Append(matches[i].Groups[PrefixSpaceLabel]);
                    prefixSpaceLength = matches[i].Groups[PrefixSpaceLabel].Length;
                }
                outputOffset += matches[i].Index - lastInputMatchEnd;
                var outputIndieMatches = new List<IndividualMatch>();
                var length = prefixSpaceLength + ProcessReplaceFullStructure(replace, patternStructureArray, matches[i], i, outputIndieMatches, outputOffset + prefixSpaceLength);
                results[i] = new RegexerMatchPair
                {
                    InputMatch = new RegexerMatch(matches[i].Index, matches[i].Length, matches[i].Value)
                    {
                        IndividualMatches = GetInputIndividualMatches(matches[i])
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
            return new RegexerResult { Output = patternBuilder.ToString(), Matches = results };
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

        private int ProcessReplaceFullStructure(string pattern, IEnumerable<Token> structure, Match match, int matchIndex, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var totalLength = 0;
            foreach (var token in structure)
            {
                int length;
                switch (token)
                {
                    case FreeTextToken freeTextToken:
                        length = ProcessReplaceFreeText(pattern, freeTextToken, match);
                        break;
                    case ComplexToken complexToken:
                        switch (complexToken.Name)
                        {
                            case TokenName.Pattern:
                                length = ProcessReplacePatternToken(pattern, complexToken, match, matchIndex, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.MultiLine:
                                length = ProcessReplaceMultiLineToken(pattern, complexToken, match, outputIndieMatches, outputOffset);
                                break;
                            case TokenName.UnorderedGroup:
                                length = ProcessReplaceUnorderedGroupToken(pattern, complexToken, match, matchIndex, outputIndieMatches, outputOffset);
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
                if (exactWhiteSpace) patternBuilder.Append(spaceBuilder);
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
                    if(!exactWhiteSpace)
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

        private ReadOnlySpan<char> NewLineWithPrefixSpace() => exactWhiteSpace ? "\r\n".AsSpan() : $@"\r\n\k<{PrefixSpaceLabel}>".AsSpan();

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

            if (elements[^2].Name == TokenName.Optional)
            {
                patternBuilder.Append('?');
                patternData?.IsOptional = true;
            }
            return;

            void ProcessRestrictionQuantifierNewLine(ComplexToken restrictionQuantifierToken, ref ReadOnlySpan<char> restriction, ref ReadOnlySpan<char> quantifier)
            {
                var modifiers = restrictionQuantifierToken.Children;
                var restrictionChar = '\0';
                var hasL = false;
                foreach (var modifier in modifiers)
                {
                    if (modifier is ComplexToken exactAmount)
                    {
                        var amount1 = pattern.AsSpan(exactAmount.Children[1].Index, exactAmount.Children[1].Length);
                        if (exactAmount.Children.Count > 3)
                        {
                            var amount2 = pattern.AsSpan(exactAmount.Children[3].Index, exactAmount.Children[3].Length);
                            quantifier = $"{{{amount1},{amount2}}}".AsSpan();
                        }
                        else quantifier = $"{{{amount1}}}".AsSpan();
                        continue;
                    }
                    var modifierSpan = pattern.AsSpan(modifier.Index, modifier.Length);
                    switch (modifierSpan)
                    {
                        case "g":
                            quantifier = "+".AsSpan();
                            continue;
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

                restriction = restrictionChar switch
                {
                    'w' => (hasL ? @"[\w\r\n]" : "\\w").AsSpan(),
                    'd' => (hasL ? @"[\d\r\n]" : "\\d").AsSpan(),
                    's' => (hasL ? "\\s" : @"[^\S\r\n]").AsSpan(),
                    _ => restriction
                };
            }

            void ProcessMatchMultiple(ComplexToken matchMultipleToken, ReadOnlySpan<char> restriction, ReadOnlySpan<char> quantifier)
            {
                //Basically $"{singleLineStructure}(?:{separator}{singleLineStructure})*?";
                var matchMultipleElements = matchMultipleToken.Children;
                var singleLineStructureStartIndex = patternBuilder.Length;
                if (label != null) patternBuilder.Append($"(?<{label}>");
                if (matchMultipleElements.Count > 6)
                {
                    for (var i = 6; i < matchMultipleElements.Count; i++)
                    {
                        ProcessFindFullStructure(pattern, ((ComplexToken)matchMultipleElements[i]).Children);
                    }
                }
                else patternBuilder.Append($"{restriction}{quantifier}");
                if (label != null) patternBuilder.Append(')');
                var singleLineStructureLength = patternBuilder.Length - singleLineStructureStartIndex;
                patternBuilder.Append("(?:");
                ProcessPlainText(pattern, (FreeTextToken)matchMultipleElements[3]);
                patternBuilder.Append(patternBuilder, singleLineStructureStartIndex, singleLineStructureLength);
                patternBuilder.Append(")*?");
            }

            void ProcessRegex(FreeTextToken regexToken)
            {
                _ = label == null ? patternBuilder.Append("(?:") : patternBuilder.Append($"(?<{label}>");
                ProcessPlainText(pattern, regexToken);
                patternBuilder.Append(')');
            }
        }

        private readonly StringBuilder evalBuilder = new();
        private int ProcessReplacePatternToken(string pattern, ComplexToken patternToken, Match match, int matchIndex, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var elements = patternToken.Children;
            var label = pattern.Substring(elements[1].Index, elements[1].Length);
            if (!patternMap.TryGetValue(label, out var patternData))
            {
                patternBuilder.Append(pattern.AsSpan(patternToken.Index, patternToken.Length)); //Use the pattern text as is
                return patternToken.Length;
            }

            var inputMatch = match.Groups[label];
            var outputCaptures = new List<MatchData>();
            var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, outputCaptures));
            else outputCaptures = outputIndieMatches[n.Index].Captures;

            if (elements.Count <= 3) //Simple pattern with no transform
            {
                patternBuilder.Append(inputMatch.Value);
                outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, inputMatch.Length, inputMatch.Value));
                return inputMatch.Length;
            }

            var children = ((ComplexToken)elements[3]).Children;
            int length;
            switch (elements[3].Name)
            {
                case TokenName.Optional:
                    if (!patternData.IsOptional)
                    {
                        patternBuilder.Append(pattern.AsSpan(patternToken.Index, patternToken.Length)); //Use the pattern text as is
                        return patternToken.Length;
                    }
                    var modifier = pattern.AsSpan(children[0].Index, children[0].Length);
                    var outputMatchData = new MatchData(0, 0, string.Empty);
                    outputCaptures.Add(outputMatchData);

                    if ((modifier is not "o" || !inputMatch.Success) && (modifier is not "oi" || inputMatch.Success))
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

                    outputMatchData.Index = patternToken.Index + outputOffset;
                    var offset = 0;
                    var singleLineStart = children.Count > 2 ? children[2].Index : 0;
                    for (var i = 2; i < children.Count; i++)
                    {
                        var singleLineToken = (ComplexToken)children[i];
                        length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, match, matchIndex, outputIndieMatches, outputMatchData.Index - singleLineStart - offset);
                        offset += singleLineToken.Length - length;
                        outputMatchData.Length += length;
                    }
                    outputMatchData.Text = patternBuilder.ToString(outputMatchData.Index, outputMatchData.Length);
                    return outputMatchData.Length;

                case TokenName.Duplicate:
                    evalBuilder.Clear();
                    for (var i = children[2].Index; i < children[2].Index + children[2].Length; i++)
                    {
                        var symbol = pattern[i];
                        if (symbol == 'i') evalBuilder.Append(matchIndex + 1);
                        else evalBuilder.Append(symbol);
                    }

                    var duplicationAmount = EvaluateByte(evalBuilder.ToString());
                    if (duplicationAmount == 0)
                    {
                        outputCaptures.Add(new MatchData(0, 0, string.Empty));
                        return 0;
                    }
                    patternBuilder.Append(inputMatch.Value);
                    length = inputMatch.Length;
                    if (duplicationAmount == 1)
                    {
                        outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length, inputMatch.Value));
                        return length;
                    }

                    var separatorStart = patternToken.Index + outputOffset + inputMatch.Length;
                    var separatorLength = 0;
                    if (children.Count > 4)
                    {
                        separatorLength = ProcessReplaceFreeText(pattern, (FreeTextToken)children[4], match);
                        length += separatorLength;
                    }
                    patternBuilder.Append(inputMatch.Value);
                    length += inputMatch.Length;

                    for (var i = 0; i < duplicationAmount - 2; i++)
                    {
                        patternBuilder.Append(patternBuilder, separatorStart, separatorLength);
                        length += separatorLength;
                        patternBuilder.Append(inputMatch.Value);
                        length += inputMatch.Length;
                    }

                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length, patternBuilder.ToString(patternToken.Index + outputOffset, length)));
                    return length;

                case TokenName.Capitalize:
                    var capitalizeModifier = pattern.AsSpan(children[2].Index, children[2].Length);
                    patternBuilder.Append(capitalizeModifier switch
                    {
                        "u" => inputMatch.Value.ToUpper(), //u: Upper case
                        "l" => inputMatch.Value.ToLower(), //l: Lower case
                        "fu" => inputMatch.Value[..1].ToUpper() + inputMatch.Value[1..], //fu: First letter upper case
                        "fl" => inputMatch.Value[..1].ToLower() + inputMatch.Value[1..], //fl: First letter lower case
                        _ => inputMatch.Value[..1].ToUpper() + inputMatch.Value[1..].ToLower() //s: Sentence case
                    });
                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, inputMatch.Length,
                        patternBuilder.ToString(patternToken.Index + outputOffset, inputMatch.Length)));
                    return inputMatch.Length;

                case TokenName.Evaluate:
                    evalBuilder.Clear();
                    for (var i = children[2].Index; i < children[2].Index + children[2].Length; i++)
                    {
                        var symbol = pattern[i];
                        if (symbol == 'i') evalBuilder.Append(matchIndex + 1);
                        else if (symbol == 'm') evalBuilder.Append(int.Parse(inputMatch.Value));
                        else evalBuilder.Append(symbol);
                    }

                    var replacement = Evaluate(evalBuilder.ToString());
                    patternBuilder.Append(replacement);
                    length = replacement.ToString().Length;
                    outputCaptures.Add(new MatchData(patternToken.Index + outputOffset, length,
                        patternBuilder.ToString(patternToken.Index + outputOffset, length)));
                    return length;

                case TokenName.MatchMultiple:
                    var spreadAmount = inputMatch.Captures.Count;
                    length = 0;
                    singleLineStart = 0;
                    for (var s = 0; s < spreadAmount; s++)
                    {
                        if (children.Count > 4)
                        {
                            for (var i = 4; i < children.Count; i++)
                            {
                                var singleLineToken = (ComplexToken)children[i];
                                foreach (var token in singleLineToken.Children)
                                {
                                    switch (token.Name)
                                    {
                                        case TokenName.FreeText:
                                            length += ProcessReplaceFreeText(pattern, token, match);
                                            break;
                                        case TokenName.MatchMultipleSpread:
                                            var spreadToken = (ComplexToken)token;
                                            var spreadLabel = pattern.Substring(spreadToken.Children[1].Index, spreadToken.Children[1].Length);
                                            List<MatchData> spreadOutputCaptures;
                                            n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, spreadLabel);
                                            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(spreadLabel, spreadOutputCaptures = []));
                                            else spreadOutputCaptures = outputIndieMatches[n.Index].Captures;
                                            var spreadCapture = match.Groups[spreadLabel].Captures[s];
                                            patternBuilder.Append(spreadCapture.Value);
                                            spreadOutputCaptures.Add(new MatchData(patternToken.Index + outputOffset + length, spreadCapture.Length, spreadCapture.Value));
                                            length += spreadCapture.Length;
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                            }
                        }
                        else
                        {
                            patternBuilder.Append(inputMatch.Captures[s]);
                            length += inputMatch.Captures[s].Length;
                        }
                        outputCaptures.Add(new MatchData(patternToken.Index + outputOffset + singleLineStart, length - singleLineStart, patternBuilder.ToString(patternToken.Index + outputOffset + singleLineStart, length - singleLineStart)));

                        if (s == spreadAmount - 1) break;
                        length += ProcessReplaceFreeText(pattern, (FreeTextToken)children[2], match);
                        singleLineStart = length;
                    }

                    return length;
            }

            return 0;

            static byte EvaluateByte(string expression)
            {
                var evaluation = Evaluate(expression);
                if (evaluation > byte.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("The maximum amount of duplications allowed is 255.", default(Exception));
                }

                return (byte)evaluation;
            }

            static int Evaluate(string expression)
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
        }

        private void ProcessFindMultiLineToken(string pattern, ComplexToken multiLineToken)
        {
            var elements = multiLineToken.Children;
            var label = pattern.Substring(elements[2].Index, elements[2].Length);
            var spaceStartIndexOffset = pattern[elements[0].Index] == '\r' && pattern[elements[0].Index + 1] == '\n' ? 2 : 0;
            var precedingSpace = pattern.AsSpan(elements[0].Index + spaceStartIndexOffset, elements[0].Length - spaceStartIndexOffset);
            if (spaceStartIndexOffset > 0) patternBuilder.Append(NewLineWithPrefixSpace());
            patternBuilder.Append(precedingSpace);

            if (patternMap.ContainsKey(label))
            {
                patternBuilder.Append($"\\k<{label}FirstLine>");
                return;
            }
            patternMap.Add(label, new PatternData(label){ IsMultiLine = true });

            if (false)
            {

            }
            else
            {
                patternBuilder.Append($@"(?<{label}FirstLine>([^\r\n]+)?)({NewLineWithPrefixSpace()}{precedingSpace}(?<{label}NextLines>([^\r\n]+)?))*?");
            }

            hasMl = true;
        }

        private int ProcessReplaceMultiLineToken(string pattern, ComplexToken multiLineToken, Match match, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var elements = multiLineToken.Children;
            var label = pattern.Substring(elements[3].Index, elements[3].Length);
            if (!patternMap.TryGetValue(label, out var patternData))
            {
                patternBuilder.Append(pattern.AsSpan(multiLineToken.Index, multiLineToken.Length)); //Use the pattern text as is
                return multiLineToken.Length;
            }

            var firstLine = match.Groups[$"{label}FirstLine"].Captures;
            var nextLines = match.Groups[$"{label}NextLines"].Captures;
            var lineCaptures = nextLines.Prepend(firstLine[0]).ToArray();

            var outputCaptures = new List<MatchData>();
            var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
            if (n.DidNotExist) outputIndieMatches.Insert(n.Index, new IndividualMatch(label, outputCaptures));
            else outputCaptures = outputIndieMatches[n.Index].Captures;

            var spaceStartIndexOffset = pattern[elements[0].Index] == '\r' && pattern[elements[0].Index + 1] == '\n' ? 2 : 0;
            var precedingSpace = pattern.AsSpan(elements[0].Index + spaceStartIndexOffset, elements[0].Length - spaceStartIndexOffset);

            var outputStart = multiLineToken.Index + outputOffset;
            for (var i = 0; i < lineCaptures.Length; i++)
            {
                var outputLength = 0;
                if (i > 0 || spaceStartIndexOffset > 0)
                {
                    patternBuilder.AppendLine();
                    outputLength += 2;
                }
                if (!exactWhiteSpace)
                {
                    patternBuilder.Append(match.Groups[PrefixSpaceLabel].Value);
                    outputLength += match.Groups[PrefixSpaceLabel].Length;
                }
                var line = lineCaptures[i];
                patternBuilder.Append(precedingSpace);
                outputLength += precedingSpace.Length;
                var outputLineStart = outputLength;
                outputLength += ProcessReplaceFreeText(pattern, (FreeTextToken)elements[1], match/*, outputOffset - elements[1].Index + multiLineToken.Index*/);
                patternBuilder.Append(line.Value);
                outputLength += line.Length + ProcessReplaceFreeText(pattern, (FreeTextToken)elements[7], match/*, outputOffset - elements[7].Index + multiLineToken.Index*/);
                var outputLineLength = outputLength - outputLineStart;
                patternBuilder.Append(pattern.AsSpan(elements[8].Index, elements[8].Length));
                outputLength += elements[8].Length;
                outputCaptures.Add(new MatchData(outputStart + outputLineStart, outputLineLength,
                    patternBuilder.ToString(outputStart + outputLineStart, outputLineLength)));
                outputStart += outputLength;
            }

            return outputStart - (multiLineToken.Index + outputOffset);
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

                    patternMap.Add(label, new PatternData(label) { IsOptional = true });
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

        private int ProcessReplaceUnorderedGroupToken(string pattern, ComplexToken unorderedGroupToken, Match match, int matchIndex, List<IndividualMatch> outputIndieMatches, int outputOffset)
        {
            var totalLength = 0;
            var outputStart = unorderedGroupToken.Index + outputOffset;
            Token? firstEntryWhitespace = null;
            for (var i = 0; i < unorderedGroupToken.Children.Count; i++)
            {
                var unorderedElements = ((ComplexToken)unorderedGroupToken.Children[i]).Children;
                var label = pattern.Substring(unorderedElements[2].Index, unorderedElements[2].Length);
                if (!patternMap.TryGetValue(label, out var patternData))
                {
                    patternBuilder.Append(pattern.AsSpan(unorderedGroupToken.Children[i].Index, unorderedGroupToken.Children[i].Length)); //Use the pattern text as is
                    totalLength += unorderedGroupToken.Children[i].Length;
                    continue;
                }

                var inputMatch = match.Groups[label];
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
                var whitespaceLength = ProcessReplaceFreeText(pattern, firstEntryWhitespace ?? unorderedElements[0], match);
                firstEntryWhitespace = null;
                outputMatchData.Index = outputStart + totalLength + whitespaceLength;
                if (unorderedElements.Count > 6)
                {
                    var offset = 0;
                    var singleLineStart = unorderedElements[6].Index;
                    for (var j = 6; j < unorderedElements.Count - 1; j++)
                    {
                        var singleLineToken = (ComplexToken)unorderedElements[j];
                        var length = ProcessReplaceFullStructure(pattern, singleLineToken.Children, match, matchIndex, outputIndieMatches, outputMatchData.Index - singleLineStart - offset);
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

        private List<IndividualMatch> GetInputIndividualMatches(Match match)
        {
            var individualMatches = new List<IndividualMatch>();
            foreach (var label in patternMap.Keys)
            {
                var n = GetAlphabeticalOrderIndex(individualMatches, l => l.Label, label);
                var patternData = patternMap[label];
                List<MatchData> captures;
                if (patternData.IsMultiLine)
                {
                    var firstLine = match.Groups[$"{label}FirstLine"].Captures;
                    var nextLines = match.Groups[$"{label}NextLines"].Captures;
                    captures = [new(firstLine[0].Index, firstLine[0].Length, firstLine[0].Value)];
                    for (var i = 0; i < nextLines.Count; i++)
                    {
                        captures.Add(new MatchData(nextLines[i].Index, nextLines[i].Length, nextLines[i].Value));
                    }
                    individualMatches.Insert(n.Index, new IndividualMatch(label, captures));
                    continue;
                }

                captures = [];
                var matchCaptures = match.Groups[label].Captures;
                for (var i = 0; i < matchCaptures.Count; i++)
                {
                    captures.Add(new MatchData(matchCaptures[i].Index, matchCaptures[i].Length, matchCaptures[i].Value));
                }
                individualMatches.Insert(n.Index, new IndividualMatch(label, captures));

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

        public class PatternData(string label)
        {
            public string Label { get; set; } = label;
            public bool IsOptional { get; set; }
            public bool IsDigit { get; set; }
            public bool IsMultiLine { get; set; }
            public bool IsMultiMatch { get; set; }
        }
    }
    public class RegexerResult
    {
        public string Output { get; set; }
        public RegexerMatchPair[]? Matches { get; set; }
    }

    public class RegexerMatchPair
    {
        public RegexerMatch InputMatch { get; set; }
        public RegexerMatch OutputMatch { get; set; }
    }

    public class MatchData
    {
        public MatchData(int index, int length, string text)
        {
            Index = index;
            Length = length;
            Text = text;
        }

        public int Index { get; set; }
        public int Length { get; set; }
        public string Text { get; set; }
        public override string ToString()
        {
            return $"{Index}, {Length}, {Text}";
        }
    }

    public class RegexerMatch : MatchData
    {
        public RegexerMatch(int index, int length, string text) : base(index, length, text) { }

        public List<IndividualMatch> IndividualMatches { get; set; }

        public override string ToString()
        {
            return $"{Index}, {Length}, {(IndividualMatches == null ? "<null>" : string.Join(", ", IndividualMatches.Select(i => $"<{i?.ToString() ?? "null"}>")))}";
        }
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
}