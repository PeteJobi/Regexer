using NCalc;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Regexer;

public class Regexer
{
    private TimeSpan _regexTimeout;
    private bool fasterMl;
    private ResultExtent resultExtent;

    public Regexer()
    {
        _regexTimeout = TimeSpan.FromSeconds(10);
        resultExtent = ResultExtent.Full;
    }

    public Regexer(TimeSpan regexTimeout, ResultExtent resultExtent = ResultExtent.Full, bool fasterMl = false)
    {
        this._regexTimeout = regexTimeout;
        this.resultExtent = resultExtent;
        this.fasterMl = fasterMl;
    }

    private string EscapeRegexKeywords(string pattern)
    {
        const string regexKeywords = @"[\\$.+*()\[\]|^?]";
        pattern = Regex.Replace(pattern, $"({regexKeywords})", "\\$1");
        var matches = Regex.Matches(pattern, @"(?<=\\\[\\\[\w*?(?:(?:\\\|)?m\\\|)?\{)(((?!\}(?:\\\|.+?)?\\\]\\\]).)+)(?=\}(?:\\\|.+?)?\\\]\\\])");
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            var rep = Regex.Replace(match.Groups[1].Value, $@"(\\({regexKeywords}))", "$2");
            pattern = pattern[..match.Index] + rep + pattern[(match.Index + match.Length)..];
        }
        return pattern;
    }

    public async Task<RegexerResult> AutoRegex(string input, string pattern, string replace, CancellationToken cancellationToken = default)
    {
        return await await Task.WhenAny(
            Cancel(cancellationToken),
            Task.Run(() => AutoRegexInternal(input, pattern, replace), cancellationToken));
    }

    public void SetResultExtent(ResultExtent resultExtent) => this.resultExtent = resultExtent;
    public void SetRegexTimeout(TimeSpan regexTimeout) => this._regexTimeout = regexTimeout;
    public void EnableFasterML(bool faster) => fasterMl = faster;

    private async Task<RegexerResult> Cancel(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(-1, cancellationToken);
        }
        catch (TaskCanceledException) { }
        return new RegexerResult { Output = "Cancelled" };
    }

    private RegexerResult AutoRegexInternal(string input, string pattern, string replace)
    {
        pattern = EscapeRegexKeywords(pattern);
        pattern = Regex.Replace(pattern, "\r\n", "\r\n(?:[^\\S\\r\\n]+)?");
        pattern = Regex.Replace(pattern, @"^(\\\[\\\[\w+\\\|ml\\\]\\\])$", "^$1$");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+(\\\|\w+)?)\\\]\\\])", "[$2]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+)?\{([^\r\n]+?)\}\\\]\\\])", "[$2{$3}]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+\\\|)?u\\\|([^\r\n]+?)\\\]\\\])", "[$2u\\|$3]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+\\\|)?m\\\|\{([^\r\n]+?)\}\\\|([^\r\n]+?)\\\]\\\])", "[$2m\\|{$3}\\|$4]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+\\\|)?m\\\|\{([^\r\n]+?)\}\\\]\\\])", "[$2m\\|{$3}]");
        pattern = Regex.Replace(pattern, @"(?>[^\S\r\n]+)(?!\[\w+\\\|ml\])", @"[^\S\r\n]+");
        pattern = @"(?<space>[^\S\r\n]+)?" + pattern;
        var multiLineGroups = Regex.Matches(pattern, @"\[(\w+)\\\|ml\]").Select(g => g.Groups[1].Value).ToArray();
        if (multiLineGroups.Any())
        {
            var rep = fasterMl
                ? @"(?<${mlName}FirstLine>[^\r\n]*?)(\r\n(\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]*)[^\r\n]*?))?)*?"
                : @"(?<${mlName}FirstLine>([^\r\n]+)?)(\r\n\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]+)?([^\r\n]+)?))*?";
            pattern = Regex.Replace(pattern, @"(?<mlSpace>[^\S\r\n]+)?\[(?<mlName>\w+)\\\|ml\]", rep);
        }
        var namedCaptures = new HashSet<string>();
        var simpleMatches = Regex.Matches(pattern, @"\[(\w+)\]");
        foreach (Match simpleMatch in simpleMatches)
        {
            namedCaptures.Add(simpleMatch.Groups[1].Value);
        }
        pattern = Regex.Replace(pattern, @"\[(\w+)\]", "(?<$1>[^\\r\\n]+?)");
        var optionalGroups = new List<string>();
        var digitGroups = new List<string>();
        var configMatches = Regex.Matches(pattern, @"\[(\w+)\\\|([wdsgol]+)\]");
        for (var i = configMatches.Count - 1; i >= 0; i--)
        {
            var name = configMatches[i].Groups[1].Value;
            var options = configMatches[i].Groups[2].Value;
            var restriction = options.Contains('w') ? (options.Contains('l') ? "[\\w\\r\\n]" : "\\w")
                : options.Contains('d') ? (options.Contains('l') ? "[\\d\\r\\n]" : "\\d")
                : options.Contains('s') ? (options.Contains('l') ? "\\s" : "[^\\S\\r\\n]")
                : (options.Contains('l') ? "[\\S\\s]" : "[^\\r\\n]");
            var quantifier = options.Contains('g') ? "+" : "+?";
            var optional = options.Contains('o') ? "?" : "";
            if (optional != string.Empty) optionalGroups.Add(name);
            if (restriction == "\\d") digitGroups.Add(name);

            var newPattern = $"(?<{name}>{restriction}{quantifier}){optional}";

            pattern = pattern[..configMatches[i].Index] + newPattern + pattern[(configMatches[i].Index + configMatches[i].Length)..];
            namedCaptures.Add(name);
        }

        var patternMatches = Regex.Matches(pattern, @"\[(\w+)\{([^\r\n]+?)\}\]");
        for (var i = patternMatches.Count - 1; i >= 0; i--)
        {
            var patternMatch = patternMatches[i];
            namedCaptures.Add(patternMatch.Groups[1].Value);
            pattern = pattern[..patternMatch.Index] + $"(?<{patternMatch.Groups[1].Value}>{patternMatch.Groups[2].Value})" + pattern[(patternMatch.Index + patternMatch.Length)..];
        }

        pattern = Regex.Replace(pattern, @"\[\{([^\r\n]+?)\}\]", "$1");
        var uMatches = Regex.Matches(pattern, @"(?<uLines>\r\n\(\?:\[\^\\S\\r\\n\]\+\)\?(\[\^\\S\\r\\n\]\+)?\[(?:\w+\\\|)?u\\\|[^\r\n]+\])+\r\n");
        for (var i = uMatches.Count - 1; i >= 0; i--)
        {
            var uMatch = uMatches[i];
            var lMatches = uMatch.Groups["uLines"].Captures.Select(c => Regex.Match(c.Value, @"\r\n.+?\[((?<uName>\w+)\\\|)?u\\\|(?<uLine>.+)\]")).ToArray();
            IEnumerable<(string line, string name)> linesAndNames = lMatches.Select(m => (m.Groups["uLine"].Value, m.Groups["uName"].Value));
            var inAnyOrder = string.Join(string.Empty, linesAndNames.Select(l => $"(?=.*({l.line})?)"));
            var noDuplicates = $"(?!.*({string.Join('|', linesAndNames.Select(l => $"\\s+{l.line}"))})+.*\\1)";
            var nothingElseBesidesThem = $"({string.Join('|', linesAndNames.Select(l => "\\s+" + (l.name == string.Empty ? l.line : $"(?<{l.name}>{l.line})")))})*";
            var fullPattern = inAnyOrder + noDuplicates + nothingElseBesidesThem + "(?:\\r\\n)?";
            pattern = pattern[..uMatch.Index] + fullPattern + pattern[(uMatch.Index + uMatch.Length)..];
        }

        var mMatches = Regex.Matches(pattern, @"\[((?<mName>\w+)\\\|)?m\\\|\{(?<separator>.*?)\}(?:\\\|(?<mLine>(?>[^\[\]]+|(?<Open>\[)|(?<-Open>\]))+(?(Open)(?!))))?\]");
        var multiGroups = new List<KeyValuePair<string, HashSet<string>>>();
        for (var i = mMatches.Count - 1; i >= 0; i--)
        {
            var mMatch = mMatches[i];
            var lineCaptured = mMatch.Groups["mLine"].Success ? mMatch.Groups["mLine"].Value : @"[^\r\n]+?";
            var phraseOrLineToMatch = mMatch.Groups["mName"].Value == string.Empty ? lineCaptured : $"(?<{mMatch.Groups["mName"]}>{lineCaptured})";
            if (mMatch.Groups["mName"].Value != string.Empty && mMatch.Groups["mLine"].Success)
            {
                var multiData = new KeyValuePair<string, HashSet<string>>(mMatch.Groups["mName"].Value,
                    Regex.Matches(phraseOrLineToMatch, @"\(\?<(.+?)>").Select(m => m.Groups[1].Value).ToHashSet());
                multiGroups.Add(multiData);
            }
            var separator = mMatch.Groups["separator"].Value.Replace("<ml>", "\r\n");
            var fullPattern = $"{phraseOrLineToMatch}(?:{separator}{phraseOrLineToMatch})*?";
            pattern = pattern[..mMatch.Index] + fullPattern + pattern[(mMatch.Index + mMatch.Length)..];
        }

        var matches = Regex.Matches(input, pattern, RegexOptions.None, _regexTimeout);
        if (!matches.Any()) return new RegexerResult { Output = input };

        var replaceWasEmpty = replace == string.Empty;
        replace = "${space}" + replace;
        replace = Regex.Replace(replace, "\r\n", "\r\n${space}");
        if(resultExtent == ResultExtent.Minimal) replace = Regex.Replace(replace, @"\[\[(\w+?)\]\]", "${$1}");


        if (replaceWasEmpty && resultExtent == ResultExtent.Minimal)
        {
            var result = Regex.Replace(input, pattern, replace, RegexOptions.None, _regexTimeout);
            return new RegexerResult { Output = result };
        }







        var matchesOffset = 0;
        var fullReplacement = string.Empty;
        var resultPairs = new RegexerMatchPair[matches.Count];
        var hasULines = Regex.IsMatch(replace, @"\[\[\w+\|u");
        var multiGroupsGeneral = Regex.Matches(replace, @"\[\[(\w+)\|m:[^\r\n:]*?\]\]").Select(g => g.Groups[1].Value).Distinct().ToArray();
        var duplicateGroups = Regex.Matches(replace, @"\[\[(\w+)\|d:(?:\d+|[\di+*/%-]+)(?::.+?)?\]\]").Select(g => g.Groups[1].Value).Distinct().ToArray();
        var capitalizeGroups = Regex.Matches(replace, @"\[\[(\w+)\|c:(?:u|l|s|fu|fl)\]\]").Select(g => g.Groups[1].Value).Distinct().ToArray();
        for (var i = 0; i < matches.Count; i++)
        {
            var modifiedReplace = replace;
            var multiGroupRepLengths = new Dictionary<string, List<int>>();
            foreach (var group in multiGroups) //Expand the multi groups in the replacement and keep track of the lengths of each repetition for each match, so that they can be correctly associated with their respective captures in the input when they are later transformed into MatchData and added to the outputIndieMatches
            {
                var mResultMatches = Regex.Matches(replace, string.Format(@"\[\[{0}\|m:(?<separator>[^\r\n]*?):(?<replacement>(?>\[\[(?<Open>)|(?<-Open>\]\])|\[(?!\[)|\](?!\])|[^\[\]])*(?(Open)(?!)))\]\]", group.Key));
                if (!mResultMatches.Any()) continue;
                var lengths = new List<int>();
                multiGroupRepLengths.TryAdd(group.Key, lengths);
                for (var j = mResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = mResultMatches.ElementAt(j);
                    var rep = match.Groups["replacement"].Value;
                    lengths.Add(rep.Length);
                    rep = string.Join("", Enumerable.Repeat(rep, matches[i].Groups[group.Key].Captures.Count));
                    modifiedReplace = modifiedReplace[..match.Groups["replacement"].Index] + rep + modifiedReplace[(match.Groups["replacement"].Index + match.Groups["replacement"].Length)..];
                }
            }

            var beforeMatchStartIndex = i == 0 ? 0 : matches[i - 1].Index + matches[i - 1].Length;
            matchesOffset += matches[i].Index - beforeMatchStartIndex;
            var replacement = matches[i].Result(modifiedReplace);
            var inputIndieMatches = new List<IndividualMatch>();
            var outputIndieMatches = new List<IndividualMatch>();
            var orderedMatchData = new List<MatchData>();

            foreach (var group in namedCaptures)
            {
                var inputCaptures = new List<MatchData>();
                var outputCaptures = new List<MatchData>();
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                inputIndieMatches.Insert(n.Index, new IndividualMatch(group, inputCaptures));
                outputIndieMatches.Insert(n.Index, new IndividualMatch(group, outputCaptures));

                var inputMatch = matches[i].Groups[group];
                foreach (Capture capture in inputMatch.Captures)
                {
                    inputCaptures.Add(new MatchData(capture.Index, capture.Length, capture.Value));
                }

                var ppResultMatches = Regex.Matches(replacement, $@"\[\[{group}\]\]");
                for (var j = ppResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = ppResultMatches.ElementAt(j);
                    var rep = inputMatch.Value;
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            if (hasULines)
            {
                var uResultMatches = Regex.Matches(replacement, @"(?<uLines>(?:[^\S\r\n]+)?\[\[\w+\|u(?::(?>\[\[(?<Open>)|(?<-Open>\]\])|\[(?!\[)|\](?!\])|[^\[\]])*(?(Open)(?!)))?\]\][^\S\r\n]*?(?:\n|\r\n)?)+");
                for (var j = uResultMatches.Count - 1; j >= 0; j--)
                {
                    for (var k = uResultMatches[j].Groups["uLines"].Captures.Count - 1; k >= 0; k--)
                    {
                        var index = uResultMatches[j].Groups["uLines"].Captures[k].Index;
                        var length = uResultMatches[j].Groups["uLines"].Captures[k].Length;
                        var match = Regex.Match(uResultMatches[j].Groups["uLines"].Captures[k].Value, @"(?<uSpace>[^\S\r\n]+)?\[\[(?<uName>\w+)\|?u(?::(?<replacement>.+))?\]\](?<uEndSpace>\s+)?");
                        var startingSpace = match.Groups["uSpace"].Value;
                        var isLastInGroup = k == uResultMatches[j].Groups["uLines"].Captures.Count - 1;
                        var name = match.Groups["uName"].Value;
                        var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, name);
                        var inputCaptures = new List<MatchData>();
                        var outputCaptures = new List<MatchData>();
                        inputIndieMatches.Insert(n.Index, new IndividualMatch(name, inputCaptures));
                        outputIndieMatches.Insert(n.Index, new IndividualMatch(name, outputCaptures));
                        var inputMatch = matches[i].Groups[name];
                        inputCaptures.Add(new MatchData(inputMatch.Index, inputMatch.Length, inputMatch.Value));
                        var endingSpace = isLastInGroup && !inputMatch.Success ? string.Empty : match.Groups["uEndSpace"].Value;
                        var rep = !inputMatch.Success ? string.Empty : startingSpace + (match.Groups["replacement"].Value != string.Empty ? match.Groups["replacement"].Value : inputMatch.Value) + endingSpace;
                        var matchData = new MatchData(index, length, rep);
                        outputCaptures.Add(matchData);
                        n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                        orderedMatchData.Insert(n.Index, matchData);
                    }
                }
            }

            foreach (var group in multiLineGroups)
            {
                var mlMatches = Regex.Matches(replacement, string.Format(@"(?<space>[^\S\r\n]+)?(?<before>[^\r\n]+?)?\[\[{0}\|ml\]\](?<after>[^\r\n]+?)?(?<end>\r\n|$)", group));
                if (!mlMatches.Any()) continue;
                var inputCaptures = new List<MatchData>();
                var outputCaptures = new List<MatchData>();
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                if (n.DidNotExist)
                {
                    inputIndieMatches.Insert(n.Index, new IndividualMatch(group, inputCaptures));
                    outputIndieMatches.Insert(n.Index, new IndividualMatch(group, outputCaptures));
                }
                else
                {
                    inputCaptures = inputIndieMatches[n.Index].Captures;
                    outputCaptures = outputIndieMatches[n.Index].Captures;
                }
                var firstLine = matches[i].Groups[$"{group}FirstLine"].Captures;
                var nextLines = matches[i].Groups[$"{group}NextLines"].Captures;
                var lineCaptures = (firstLine[0].Value != string.Empty ? nextLines.Prepend(firstLine[0]) : nextLines).ToArray();
                inputCaptures.AddRange(lineCaptures.Select(capture => new MatchData(capture.Index, capture.Length, capture.Value)));
                for (var j = mlMatches.Count - 1; j >= 0; j--)
                {
                    var match = mlMatches.ElementAt(j);
                    var rep = string.Join("\r\n", lineCaptures.Select(line => match.Groups["space"].Value + match.Groups["before"] + line.Value + match.Groups["after"])) + match.Groups["end"];
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in multiGroups)
            {
                var mResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|m:(?<separator>[^\r\n]*?):(?<replacement>(?>\[\[(?<Open>)|(?<-Open>\]\])|\[(?!\[)|\](?!\])|[^\[\]])*(?(Open)(?!)))\]\]", group.Key));
                if (!mResultMatches.Any()) continue;
                var inputCaptures = new List<MatchData>();
                var outputCaptures = new List<MatchData>();
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group.Key);
                if (n.DidNotExist)
                {
                    inputIndieMatches.Insert(n.Index, new IndividualMatch(group.Key, inputCaptures));
                    outputIndieMatches.Insert(n.Index, new IndividualMatch(group.Key, outputCaptures));
                }
                else
                {
                    inputCaptures = inputIndieMatches[n.Index].Captures;
                    outputCaptures = outputIndieMatches[n.Index].Captures;
                }
                var inputMatch = matches[i].Groups[group.Key];
                foreach (Capture capture in inputMatch.Captures)
                {
                    inputCaptures.Add(new MatchData(capture.Index, capture.Length, capture.Value));
                }

                for (var j = mResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = mResultMatches.ElementAt(j);
                    var subMatches = Regex.Matches(match.Groups["replacement"].Value, @"\[\[(\w+?)\|m\]\]");
                    var labelCount = new Dictionary<string, int>();
                    for (var k = 0; k < subMatches.Count; k++)
                    {
                        var subMatch = subMatches[k];
                        var label = subMatch.Groups[1].Value;
                        n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, label);
                        var subOutputCaptures = outputIndieMatches[n.Index].Captures;

                        labelCount.TryAdd(label, 0);

                        var subRep = matches[i].Groups[label].Captures[labelCount[label]].Value;
                        var subMatchData = new MatchData(subMatch.Index + match.Groups["replacement"].Index, subMatch.Length, subRep);
                        subOutputCaptures.Add(subMatchData);
                        n = GetNumericOrderIndex(orderedMatchData, m => m.Index, subMatchData.Index);
                        orderedMatchData.Insert(n.Index, subMatchData);
                        labelCount[label]++;
                    }

                    var separator = match.Groups["separator"].Value.Replace("<ml>", "\r\n");
                    var list = new List<string>();
                    var shift = 0;
                    for (var k = 0; k < inputMatch.Captures.Count; k++)
                    {
                        list.Add(match.Groups["replacement"].Value[shift..(shift + multiGroupRepLengths[group.Key][j])]);
                        shift += multiGroupRepLengths[group.Key][j];
                    }
                    var rep = string.Join(separator, list);
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in multiGroupsGeneral)
            {
                var mResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|m:(?<separator>[^\r\n]*?)\]\]", group));
                var inputMatch = matches[i].Groups[group];
                var outputCaptures = new List<MatchData>();
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                if (n.DidNotExist)
                {
                    outputIndieMatches.Insert(n.Index, new IndividualMatch(group, outputCaptures));

                    var inputCaptures = new List<MatchData>();
                    inputIndieMatches.Insert(n.Index, new IndividualMatch(group, inputCaptures));
                    foreach (Capture capture in inputMatch.Captures)
                    {
                        inputCaptures.Add(new MatchData(capture.Index, capture.Length, capture.Value));
                    }
                }
                else
                {
                    outputCaptures = outputIndieMatches[n.Index].Captures;
                }
                for (var j = mResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = mResultMatches.ElementAt(j);
                    var separator = match.Groups["separator"].Value.Replace("<ml>", "\r\n");
                    var rep = string.Join(separator, inputMatch.Captures.Select(c => c.Value));
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in optionalGroups)
            {
                var oResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|o:(?<replacement>(?>\[\[(?<Open>)|(?<-Open>\]\])|\[(?!\[)|\](?!\])|[^\[\]])*(?(Open)(?!)))\]\]", group));
                var inputMatch = matches[i].Groups[group];
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                var outputCaptures = outputIndieMatches[n.Index].Captures;
                for (var j = oResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = oResultMatches.ElementAt(j);
                    var rep = !inputMatch.Success ? string.Empty : match.Groups["replacement"].Value != string.Empty ? match.Groups["replacement"].Value : inputMatch.Value;
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in duplicateGroups)
            {
                var dResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|d:(?:(?<amount>\d+)|(?<eval>[\di()+*/%-]+))(?::(?<separator>.+?))?\]\]", group));
                var inputMatch = matches[i].Groups[group];
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                var outputCaptures = outputIndieMatches[n.Index].Captures;
                for (var j = dResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = dResultMatches.ElementAt(j);
                    byte amount;
                    if (match.Groups["amount"].Value != string.Empty)
                    {
                        if (!byte.TryParse(match.Groups["amount"].Value, out amount))
                        {
                            throw new ArgumentOutOfRangeException("The maximum amount of duplications allowed is 255.", default(Exception));
                        }
                    }
                    else if (match.Groups["eval"].Value != string.Empty)
                    {
                        var expression = match.Groups["eval"].Value.Replace("i", (i + 1).ToString()); //i: one-based match index
                        var evaluation = Evaluate(expression);
                        if (evaluation > byte.MaxValue)
                        {
                            throw new ArgumentOutOfRangeException("The maximum amount of duplications allowed is 255.", default(Exception));
                        }
                        amount = (byte)evaluation;
                    }
                    else continue;
                    var separator = match.Groups["separator"].Value.Replace("<ml>", "\r\n");
                    var rep = string.Join(separator, Enumerable.Repeat(inputMatch.Value, amount));
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in capitalizeGroups)
            {
                if(digitGroups.Contains(group) && resultExtent != ResultExtent.Full) continue;
                var cResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|c:(?<type>u|l|s|fu|fl)\]\]", group));
                var inputMatch = matches[i].Groups[group];
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                var outputCaptures = outputIndieMatches[n.Index].Captures;
                for (var j = cResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = cResultMatches.ElementAt(j);
                    var caseType = match.Groups["type"].Value;
                    var rep = caseType switch
                    {
                        "u" => inputMatch.Value.ToUpper(), //u: Upper case
                        "l" => inputMatch.Value.ToLower(), //l: Lower case
                        "fu" => inputMatch.Value[..1].ToUpper() + inputMatch.Value[1..], //fu: First letter upper case
                        "fl" => inputMatch.Value[..1].ToLower() + inputMatch.Value[1..], //fl: First letter lower case
                        _ => inputMatch.Value[..1].ToUpper() + inputMatch.Value[1..].ToLower() //s: Sentence case
                    };
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            foreach (var group in digitGroups)
            {
                var eResultMatches = Regex.Matches(replacement, string.Format(@"\[\[{0}\|e:(?<eval>[\dim()+*/%-]+)\]\]", group));
                var inputMatch = matches[i].Groups[group];
                var n = GetAlphabeticalOrderIndex(outputIndieMatches, l => l.Label, group);
                var outputCaptures = outputIndieMatches[n.Index].Captures;
                for (var j = eResultMatches.Count - 1; j >= 0; j--)
                {
                    var match = eResultMatches.ElementAt(j);
                    var expression = match.Groups["eval"].Value.Replace("i", (i + 1).ToString()).Replace("m", inputMatch.Value); //i: one-based match index, m: match value
                    var rep = Evaluate(expression).ToString();
                    var matchData = new MatchData(match.Index, match.Length, rep);
                    outputCaptures.Add(matchData);
                    n = GetNumericOrderIndex(orderedMatchData, m => m.Index, matchData.Index);
                    orderedMatchData.Insert(n.Index, matchData);
                }
            }

            var offset = 0;
            for (var j = 0; j < orderedMatchData.Count; j++)
            {
                var mData = orderedMatchData[j];
                var mDataOldLength = mData.Length;
                if (j < orderedMatchData.Count - 1 && orderedMatchData[j + 1].Index < mData.Index + mData.Length)
                {
                    var (_, amountProcessed) = Search(j + 1, mData, offset);
                    j += amountProcessed;
                }
                mData.Index += offset;
                replacement = replacement[..mData.Index] + mData.Text + replacement[(mData.Index + mDataOldLength)..];
                mData.Index += matchesOffset;
                mData.Length = mData.Text.Length;
                offset += mData.Text.Length - mDataOldLength;
            }

            resultPairs[i] = new RegexerMatchPair
            {
                InputMatch = new RegexerMatch(matches[i].Index, matches[i].Length, matches[i].Value)
                {
                    IndividualMatches = inputIndieMatches
                },
                OutputMatch = new RegexerMatch(matchesOffset, replacement.Length, replacement)
                {
                    IndividualMatches = outputIndieMatches
                }
            };

            matchesOffset += replacement.Length;
            fullReplacement += input[beforeMatchStartIndex..matches[i].Index] + replacement;
            if(i == matches.Count - 1) fullReplacement += input[(matches[i].Index + matches[i].Length)..];

            foreach (var outputIndieMatch in outputIndieMatches)
            {
                foreach (var matchData in outputIndieMatch.Captures)
                {
                    Debug.WriteLine($"{outputIndieMatch.Label} -> {fullReplacement[matchData.Index..(matchData.Index + matchData.Length)]} ..... {matchData.Length},{matchData.Text.Length}");
                    
                }
            }

            (int LengthOffset, int AmountProcessed) Search(int matchDataIndex, MatchData matchDataSearched, int offset)
            {
                var startingOffset = offset;
                var endOffset = 0;
                int j;
                for (j = matchDataIndex; j < orderedMatchData.Count; j++)
                {
                    var mData = orderedMatchData[j];
                    if (mData.Index >= matchDataSearched.Index + matchDataSearched.Length) break;
                    if (j < orderedMatchData.Count - 1 && orderedMatchData[j + 1].Index + orderedMatchData[j + 1].Length < mData.Index + mData.Length)
                    {
                        var (lengthOffset, amountProcessed) = Search(j + 1, mData, offset);
                        j += amountProcessed;
                        endOffset += lengthOffset;
                    }

                    mData.Index += startingOffset;
                    var oldText = replacement[mData.Index..(mData.Index + mData.Length + endOffset)];
                    mData.Index = matchDataSearched.Index + startingOffset + matchesOffset;
                    var indexInParent = matchDataSearched.Text.IndexOf(oldText, StringComparison.Ordinal);
                    if (indexInParent != -1)
                    {
                        mData.Index += indexInParent;
                        matchDataSearched.Text = matchDataSearched.Text[..indexInParent] + mData.Text + matchDataSearched.Text[(indexInParent + oldText.Length)..];
                        offset += mData.Text.Length - mData.Length;
                    }
                    mData.Length = mData.Text.Length;
                }

                matchDataSearched.Length = matchDataSearched.Text.Length;
                return (offset - startingOffset, j - matchDataIndex);
            }
        }

        return new RegexerResult { Output = fullReplacement, Matches = resultPairs };
    }

    int Evaluate(string expression)
    {
        try
        {
            var expr = new Expression(expression);
            return expr.ToLambda<int>()();
        }
        catch (EvaluationException e)
        {
            throw new ArithmeticException($"Expression \"{expression}\" could not be evaluated.", e);
        }
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

    private static (int Index, bool DidNotExist) GetNumericOrderIndex<T>(List<T> list, Func<T, int> path, int value)
    {
        if (list.Count == 0)
            return (0, true);

        int left = 0, right = list.Count - 1;
        var lastMatchIndex = -1;

        // Binary search to find the value or its insertion point
        while (left <= right)
        {
            var mid = left + (right - left) / 2;

            if (path(list[mid]) == value)
            {
                lastMatchIndex = mid;
                left = mid + 1; // Continue searching to the right for the last occurrence
            }
            else if (path(list[mid]) < value)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        // If found, return the index of the last match
        if (lastMatchIndex != -1) return (lastMatchIndex, false);

        // If not found, return the insertion point (left position)
        return (left, true);
    }
}

public enum ResultExtent{ Minimal, Advanced, Full }
public class RegexerResult
{
    public string Output { get; set; }
    public RegexerMatchPair[]? Matches { get; set; }
}

public class RegexerMatchPair
{
    public RegexerMatch InputMatch { get; set; }
    public RegexerMatch? OutputMatch { get; set; }

    public override bool Equals(object? obj)
    {
        var pair = obj as RegexerMatchPair;
        if(pair == null) return false;
        if (InputMatch.Index != pair.InputMatch.Index) return false;
        if (InputMatch.Length != pair.InputMatch.Length) return false;
        if (InputMatch.Text != pair.InputMatch.Text) return false;
        if (OutputMatch?.Index != pair.OutputMatch?.Index) return false;
        if (OutputMatch?.Length != pair.OutputMatch?.Length) return false;
        if (OutputMatch?.Text != pair.OutputMatch?.Text) return false;
        return true;
    }
}

public class MatchData
{
    public MatchData()
    {
    }

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

public class RegexerMatch: MatchData
{
    public RegexerMatch()
    {
    }

    public RegexerMatch(int index, int length, string text) : base(index, length, text){}

    public List<IndividualMatch> IndividualMatches { get; set; }

    public override string ToString()
    {
        return $"{Index}, {Length}, {(IndividualMatches == null ? "<null>" : string.Join(", ", IndividualMatches.Select(i => $"<{i?.ToString() ?? "null"}>")))}";
    }
}

public class IndividualMatch
{
    public IndividualMatch(string label, List<MatchData> captures)
    {
        Label = label;
        Captures = captures;
    }

    public string Label { get; set; }
    public List<MatchData> Captures { get; set; }

    public override string ToString()
    {
        return $"{Label} -> {string.Join(", ", Captures.Select(c => $"[{c}]"))}";
    }
}