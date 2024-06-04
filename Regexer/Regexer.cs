using System.IO;
using System.Text.RegularExpressions;

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
        const string regexKeywords = @"[\\$.+*()\[\]|]";
        pattern = Regex.Replace(pattern, $"({regexKeywords})", "\\$1");
        return Regex.Replace(pattern, string.Format(@"(?<=\\\[\\\[\w*?\{{.*?)\\({0})(?=.*?\}}\\\]\\\])", regexKeywords), "$1");
    }

    public async Task<string> AutoRegex(string input, string pattern, string replace, CancellationToken cancellationToken = default)
    {
        return await await Task.WhenAny(
            Cancel(cancellationToken),
            Task.Run(() => AutoRegexInternal(input, pattern, replace), cancellationToken));
    }

    private async Task<string> Cancel(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(-1, cancellationToken);
        }
        catch (TaskCanceledException){}
        return "Cancelled";
    }

    private string AutoRegexInternal(string input, string pattern, string replace)
    {
        pattern = EscapeRegexKeywords(pattern);
        pattern = @"(?<space>[^\S\r\n]+)?" + pattern;
        pattern = Regex.Replace(pattern, "\r\n", "\r\n([^\\S\\r\\n]+)?");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+(\\\|\w+)?)\\\]\\\])", "[$2]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+)?\{([^\r\n]+)\}\\\]\\\])", "[$2{$3}]");
        pattern = Regex.Replace(pattern, @"(\\\[\\\[(\w+\\\|)?u\\\|([^\r\n]+?)\\\]\\\])", "[$2u|$3]");
        pattern = Regex.Replace(pattern, @"(?>[^\S\r\n]+)(?!\[\w+\\\|ml\])", @"[^\S\r\n]+");
        var multiLineGroups = Regex.Matches(pattern, @"\[(\w+)\\\|ml\]").Select(g => g.Groups[1].Value);
        if (multiLineGroups.Any())
        {
            pattern = Regex.Replace(pattern, @"(?<mlSpace>[^\S\r\n]+)\[(?<mlName>\w+)\\\|ml\]",
                @"(?<${mlName}FirstLine>[^\r\n]*?)(\r\n(\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]*)[^\r\n]*?))?)*?");
        }
        pattern = Regex.Replace(pattern, @"\[(\w+)\]", "(?<$1>[^\\r\\n]+?)");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|o\]", "(?<$1>[^\\r\\n]+?)?");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|w\]", "(?<$1>\\w+?)");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|d\]", "(?<$1>\\d+?)");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|s\]", "(?<$1>[^\\S\\r\\n]+?)");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|wo\]", "(?<$1>\\w+?)?");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|do\]", "(?<$1>\\d+?)?");
        pattern = Regex.Replace(pattern, @"\[(\w+)\\\|so\]", "(?<$1>[^\\S\\r\\n]+?)?");
        pattern = Regex.Replace(pattern, @"\[(\w+)\{([^\r\n]+)\}\]", "(?<$1>$2)");
        pattern = Regex.Replace(pattern, @"\[\{([^\r\n]+)\}\]", "$1");
        var uMatches = Regex.Matches(pattern, @"(?<uLines>\r\n\(\[\^\\S\\r\\n\]\+\)\?(\[\^\\S\\r\\n\]\+)?\[(\w+\\\|)?u\|[^\r\n]+\])+");
        //var bbb = uMatches[0].Groups["uLines"];
        for (var i = uMatches.Count - 1; i >= 0; i--)
        {
            var uMatch = uMatches[i];
            var lMatches = uMatch.Groups["uLines"].Captures.Select(c => Regex.Match(c.Value, @"\r\n.+?\[((?<uName>\w+)\\\|)?u\|(?<uLine>.+)\]"));
            replace = lMatches.Select(m => m.Groups["uName"].Value).Aggregate(replace, (current, name) => current.Replace($"[[{name}]]", $"[[{name}|]]"));
            IEnumerable<(string line, string name)> linesAndNames = lMatches.Select(m => (m.Groups["uLine"].Value, m.Groups["uName"].Value));
            var inAnyOrder = string.Join(string.Empty, linesAndNames.Select(l => $"(?=.*({l.line})?)"));
            var noDuplicates = $"(?!.*({string.Join('|', linesAndNames.Select(l => $"\\s+{l.line}"))})+.*\\1)";
            var nothingElseBesidesThem = $"({string.Join('|', linesAndNames.Select(l => "\\s+" + (l.name == string.Empty ? l.line : $"(?<{l.name}>{l.line})")))})*";
            var fullPattern = inAnyOrder + noDuplicates + nothingElseBesidesThem;
            pattern = pattern[..uMatch.Index] + fullPattern + pattern[(uMatch.Index + uMatch.Length)..];
        }

        var replaceWasEmpty = replace == string.Empty;
        replace = "${space}" + replace;
        replace = Regex.Replace(replace, "\r\n", "\r\n${space}");
        replace = Regex.Replace(replace, @"\[\[(\w+)\]\]", "${$1}");

        var matches = Regex.Matches(input, pattern, RegexOptions.None, regexTimeoutSpan);
        if (!matches.Any()) return input;
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

        if (replaceWasEmpty || !multiLineGroups.Any()) return result;
        foreach (var kvp in multiLineGroupsKvps)
        {
            var mlMatches = Regex.Matches(result, string.Format(@"(?<space>[^\S\r\n]+)(?<before>[^\r\n]+?)?\[\[{0}\]\](?<after>[^\r\n]+?)?\r\n", kvp.Key));
            if (!mlMatches.Any()) continue;
            for (var i = kvp.Value.Count() - 1; i >= 0; i--)
            {
                var lines = kvp.Value.ElementAt(i);
                var segmentCount = mlMatches.Count / kvp.Value.Count();
                var jInit = segmentCount * (i + 1);
                for (int j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = mlMatches.ElementAt(j);
                    var rep = string.Join("\r\n", lines.Select(line => match.Groups["space"].Value + match.Groups["before"] + line + match.Groups["after"])) + "\r\n";
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        var uLineGroups = Regex.Matches(replace, @"\[\[(\w+)\|([^\r\n]+)?\]\]").Select(g => g.Groups[1].Value).Distinct();
        foreach (var group in uLineGroups)
        {
            var uResultMatches = Regex.Matches(result, string.Format(@"(?<uSpace>\s+)\[\[{0}\|(?<uLine>[^\r\n]+)?\]\]", group));
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var inputMatch = matches[i].Groups[group];
                var segmentCount = uResultMatches.Count / matches.Count;
                var jInit = segmentCount * (i + 1);
                for (var j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = uResultMatches.ElementAt(j);
                    var rep = !inputMatch.Success ? string.Empty : match.Groups["uSpace"].Value + (match.Groups["uLine"].Success ? match.Groups["uLine"].Value : inputMatch.Value);
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        return result;
    }
}