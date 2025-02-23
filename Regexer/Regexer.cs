﻿using System.IO;
using System.Text.RegularExpressions;
using NCalc;

namespace Regexer;

public class Regexer
{
    private TimeSpan regexTimeoutSpan;

    public Regexer()
    {
        regexTimeoutSpan = TimeSpan.FromSeconds(10);
    }

    public Regexer(TimeSpan regexTimeoutSpan)
    {
        this.regexTimeoutSpan = regexTimeoutSpan;
    }

    private string EscapeRegexKeywords(string pattern)
    {
        const string regexKeywords = @"[\\$.+*()\[\]|^?]";
        pattern = Regex.Replace(pattern, $"({regexKeywords})", "\\$1");
        var matches = Regex.Matches(pattern, @"(?<=\\\[\\\[\w*?\{)(((?!\}\\\]\\\]).)+)(?=\}\\\]\\\])");
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
        pattern = Regex.Replace(pattern, "\r\n", "\r\n([^\\S\\r\\n]+)?");
        pattern = Regex.Replace(pattern, @"^(\\\[\\\[\w+\\\|ml\\\]\\\])$", "^$1$");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+(\\\|\w+)?)\\\]\\\])", "[$2]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+)?\{([^\r\n]+?)\}\\\]\\\])", "[$2{$3}]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+\\\|)?u\\\|([^\r\n]+?)\\\]\\\])", "[$2u\\|$3]");
        pattern = Regex.Replace(pattern, @"(?>[^\S\r\n]+)(?!\[\w+\\\|ml\])", @"[^\S\r\n]+");
        pattern = @"(?<space>[^\S\r\n]+)?" + pattern;
        var multiLineGroups = Regex.Matches(pattern, @"\[(\w+)\\\|ml\]").Select(g => g.Groups[1].Value);
        if (multiLineGroups.Any())
        {
            pattern = Regex.Replace(pattern, @"(?<mlSpace>[^\S\r\n]+)?\[(?<mlName>\w+)\\\|ml\]",
                @"(?<${mlName}FirstLine>([^\r\n]+)?)((\r\n\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]+)?([^\r\n]+)?))+?)?");
            //@"(?<${mlName}FirstLine>[^\r\n]*?)(\r\n(\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]*)[^\r\n]*?))?)*?");
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
            if(restriction == "\\d") digitGroups.Add(name);

            var newPattern = $"(?<{name}>{restriction}{quantifier}){optional}";

            pattern = pattern[..configMatches[i].Index] + newPattern + pattern[(configMatches[i].Index + configMatches[i].Length)..];
        }

        pattern = Regex.Replace(pattern, @"\[(\w+)\{([^\r\n]+?)\}\]", "(?<$1>$2)");
        pattern = Regex.Replace(pattern, @"\[\{([^\r\n]+?)\}\]", "$1");
        var uMatches = Regex.Matches(pattern, @"(?<uLines>\r\n\(\[\^\\S\\r\\n\]\+\)\?(\[\^\\S\\r\\n\]\+)?\[(\w+\\\|)?u\\\|[^\r\n]+\])+\r\n");
        for (var i = uMatches.Count - 1; i >= 0; i--)
        {
            var uMatch = uMatches[i];
            var lMatches = uMatch.Groups["uLines"].Captures.Select(c => Regex.Match(c.Value, @"\r\n.+?\[((?<uName>\w+)\\\|)?u\\\|(?<uLine>.+)\]"));
            optionalGroups.AddRange(lMatches.Select(m => m.Groups["uName"].Value));
            replace = lMatches.Select(m => m.Groups["uName"].Value).Aggregate(replace, (current, name) => current.Replace($"[[{name}]]", $"[[{name}|o:]]"));
            IEnumerable<(string line, string name)> linesAndNames = lMatches.Select(m => (m.Groups["uLine"].Value, m.Groups["uName"].Value));
            var inAnyOrder = string.Join(string.Empty, linesAndNames.Select(l => $"(?=.*({l.line})?)"));
            var noDuplicates = $"(?!.*({string.Join('|', linesAndNames.Select(l => $"\\s+{l.line}"))})+.*\\1)";
            var nothingElseBesidesThem = $"({string.Join('|', linesAndNames.Select(l => "\\s+" + (l.name == string.Empty ? l.line : $"(?<{l.name}>{l.line})")))})*";
            var fullPattern = inAnyOrder + noDuplicates + nothingElseBesidesThem + "(?:\\r\\n)?";
            pattern = pattern[..uMatch.Index] + fullPattern + pattern[(uMatch.Index + uMatch.Length)..];
        }

        var replaceWasEmpty = replace == string.Empty;
        replace = "${space}" + replace;
        replace = Regex.Replace(replace, "\r\n", "\r\n${space}");
        replace = Regex.Replace(replace, @"\[\[(\w+?)\]\]", "${$1}");

        var matches = Regex.Matches(input, pattern, RegexOptions.None, regexTimeoutSpan);
        if (!matches.Any()) return new RegexerResult { Output = input };
        //try
        //{
        //    if (!matches.Any()) return input;
        //}
        //catch (RegexMatchTimeoutException e)
        //{
        //    return "Fail";
        //}
        var multiLineGroupsKvps = multiLineGroups.Select(group =>
        {
            replace = replace.Replace($"${{{group}}}", $"[[{group}]]"); //replaces ${id} with [id]
            return new KeyValuePair<string, IEnumerable<IEnumerable<string>>>(group, matches.Select(match =>
            {
                var firstLine = match.Groups[$"{group}FirstLine"].Captures;
                var nextLines = match.Groups[$"{group}NextLines"].Captures;
                return firstLine[0].Value != string.Empty ? nextLines.Select(capture => capture.Value).Prepend(firstLine[0].Value) : nextLines.Select(capture => capture.Value);
            }));
        }).ToArray();

        var result = Regex.Replace(input, pattern, replace, RegexOptions.None, regexTimeoutSpan);

        RegexerMatchPair[] resultMatches;
        if (replaceWasEmpty)
        {
            resultMatches = GetRegexMatches(matches, replace, true);
            return new RegexerResult { Output = result, Matches = resultMatches };
        }

        var uMultiLineReplacements = new List<KeyValuePair<string, string>>?[matches.Count];

        foreach (var kvp in multiLineGroupsKvps)
        {
            var mlMatches = Regex.Matches(result, string.Format(@"(?<space>[^\S\r\n]+)?(?<before>[^\r\n]+?)?\[\[{0}\]\](?<after>[^\r\n]+?)?(?<end>\r\n|$)", kvp.Key));
            if (!mlMatches.Any()) continue;
            for (var i = kvp.Value.Count() - 1; i >= 0; i--)
            {
                var lines = kvp.Value.ElementAt(i);
                var segmentCount = mlMatches.Count / kvp.Value.Count();
                var jInit = segmentCount * (i + 1);
                uMultiLineReplacements[i] ??= new();
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = mlMatches.ElementAt(j);
                    var rep = string.Join("\r\n", lines.Select(line => match.Groups["space"].Value + match.Groups["before"] + line + match.Groups["after"])) + match.Groups["end"];
                    uMultiLineReplacements[i].Insert(0, new(match.Value, rep));
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        foreach (var group in optionalGroups)
        {
            var oResultMatches = Regex.Matches(result, string.Format(@"(?<oSpace>\s+)?\[\[{0}\|o:(?<oLine>[^\r\n]*?)\]\]", group));
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var inputMatch = matches[i].Groups[group];
                var segmentCount = oResultMatches.Count / matches.Count;
                var jInit = segmentCount * (i + 1);
                uMultiLineReplacements[i] ??= new();
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = oResultMatches.ElementAt(j);
                    var space = match.Groups["oSpace"].Value;
                    var rep = space + (!inputMatch.Success ? string.Empty : (match.Groups["oLine"].Value != string.Empty ? match.Groups["oLine"].Value : inputMatch.Value));
                    uMultiLineReplacements[i].Insert(0, new(match.Value[space.Length..], rep[space.Length..]));
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        var duplicateGroups = Regex.Matches(replace, @"\[\[(\w+)\|d:(?:\d+|[\di+*/-]+)(?::.+?)?\]\]").Select(g => g.Groups[1].Value).Distinct();
        foreach (var group in duplicateGroups)
        {
            var dResultMatches = Regex.Matches(result, string.Format(@"(?<dSpace>\s+)?\[\[{0}\|d:((?<amount>\d+)|(?<eval>[\di+*/-]+))(:(?<separator>.+?))?\]\]", group));
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var inputMatch = matches[i].Groups[group];
                var segmentCount = dResultMatches.Count / matches.Count;
                var jInit = segmentCount * (i + 1);
                uMultiLineReplacements[i] ??= new();
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
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
                        var expression = match.Groups["eval"].Value.Replace("i", (j + 1).ToString()); //i: one-based match index
                        var evaluation = Evaluate(expression);
                        if (evaluation > byte.MaxValue)
                        {
                            throw new ArgumentOutOfRangeException("The maximum amount of duplications allowed is 255.", default(Exception));
                        }
                        amount = (byte)evaluation;
                    }
                    else continue;
                    var separator = match.Groups["separator"].Value;
                    if (separator == "ml") separator = "\r\n";
                    var space = match.Groups["dSpace"].Value;
                    var rep = space + string.Join(separator, Enumerable.Repeat(inputMatch.Value, amount));
                    uMultiLineReplacements[i].Insert(0, new(match.Value[space.Length..], rep[space.Length..]));
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        var capitalizeGroups = Regex.Matches(replace, @"\[\[(\w+)\|c:(?:u|l|s)\]\]").Select(g => g.Groups[1].Value).Distinct();
        foreach (var group in capitalizeGroups)
        {
            if(digitGroups.Contains(group)) continue;
            var cResultMatches = Regex.Matches(result, string.Format(@"(?<cSpace>\s+)?\[\[{0}\|c:(?<type>u|l|s)\]\]", group));
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var inputMatch = matches[i].Groups[group];
                var segmentCount = cResultMatches.Count / matches.Count;
                var jInit = segmentCount * (i + 1);
                uMultiLineReplacements[i] ??= new();
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = cResultMatches.ElementAt(j);
                    var caseType = match.Groups["type"].Value;
                    var rep = caseType switch
                    {
                        "u" => inputMatch.Value.ToUpper(), //u: Upper case
                        "l" => inputMatch.Value.ToLower(), //l: Lower case
                        _ => inputMatch.Value[..1].ToUpper() + inputMatch.Value[1..].ToLower() //s: Sentence case
                    };
                    var space = match.Groups["cSpace"].Value;
                    rep = space + rep;
                    uMultiLineReplacements[i].Insert(0, new(match.Value[space.Length..], rep[space.Length..]));
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        foreach (var group in digitGroups)
        {
            var eResultMatches = Regex.Matches(result, string.Format(@"(?<eSpace>\s+)?\[\[{0}\|e:(?<eval>[\dim+*/-]+)\]\]", group));
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var inputMatch = matches[i].Groups[group];
                var segmentCount = eResultMatches.Count / matches.Count;
                var jInit = segmentCount * (i + 1);
                uMultiLineReplacements[i] ??= new();
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = eResultMatches.ElementAt(j);
                    var expression = match.Groups["eval"].Value.Replace("i", (j + 1).ToString()).Replace("m", inputMatch.Value); //i: one-based match index, m: match value
                    var space = match.Groups["eSpace"].Value;
                    var rep = space + Evaluate(expression);
                    uMultiLineReplacements[i].Insert(0, new(match.Value[space.Length..], rep[space.Length..]));
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        resultMatches = GetRegexMatches(matches, replace, false, uMultiLineReplacements);
        return new RegexerResult { Output = result, Matches = resultMatches };
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

    RegexerMatchPair[] GetRegexMatches(MatchCollection matches, string replace, bool noReplace, List<KeyValuePair<string, string>>?[]? uMultiLineReplacements = null)
    {
        var offset = 0;
        var matchPairs = new RegexerMatchPair[matches.Count];
        for (var i = 0; i < matchPairs.Length; i++)
        {
            var match = matches[i];
            var inpMatch = new RegexerMatch
            {
                Index = match.Index,
                Length = match.Length,
                Text = match.Value
            };
            if (noReplace || uMultiLineReplacements == null)
            {
                matchPairs[i] = new RegexerMatchPair { InputMatch = inpMatch };
                continue;
            }

            var rep = match.Result(replace);
            if (uMultiLineReplacements.All(r => r != null))
            {
                rep = uMultiLineReplacements[i]!.Aggregate(rep, (current, keyValuePair) => current.Replace(keyValuePair.Key, keyValuePair.Value));
            }

            var outMatch = new RegexerMatch
            {
                Index = match.Index + offset,
                Length = rep.Length,
                Text = rep
            };
            offset += rep.Length - match.Length;

            matchPairs[i] = new RegexerMatchPair { InputMatch = inpMatch, OutputMatch = outMatch };
        }

        return matchPairs;
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

public class RegexerMatch
{
    public int Index { get; set; }
    public int Length { get; set; }
    public string Text { get; set; }
}