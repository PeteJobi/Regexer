using System.IO;
using System.Text.RegularExpressions;

namespace Regexer;

public class Regexer
{
    public string AutoRegex(string input, string pattern, string replace)
    {
        pattern = pattern.Replace("$", "\\$");
        pattern = pattern.Replace(".", "\\.");
        pattern = pattern.Replace("+", "\\+");
        pattern = pattern.Replace("(", "\\(");
        pattern = pattern.Replace(")", "\\)");
        pattern = @"(?<space>[^\S\r\n]+)?" + pattern;
        pattern = Regex.Replace(pattern, "\r\n", "\r\n([^\\S\\r\\n]+)?");
        pattern = Regex.Replace(pattern, @"(?>[^\S\r\n]+)(?!\[\w+\|ml\])", @"[^\S\r\n]+");
        const string multiLinePattern = @"(?<mlSpace>[^\S\r\n]+)\[(?<mlName>\w+)\|ml\]";
        var mm = Regex.Matches(pattern, @"(?<mlSpace>[^\S\r\n]+)\[(?<ml>\w+)\|ml\]");
        var multiLineGroups = Regex.Matches(pattern, multiLinePattern).Select(g => g.Groups["mlName"].Value);
        pattern = Regex.Replace(pattern, multiLinePattern, @"(?<${mlName}>(?<${mlName}FirstLine>.+?)((\r\n\k<space>?${mlSpace}(?<${mlName}NextLines>([^\S\r\n]+)?.+?))+?)?)");
        pattern = Regex.Replace(pattern, @"\[(\w+)\]", "(?<$1>.+?)");
        replace = "${space}" + replace;
        replace = Regex.Replace(replace, "\r\n", "\r\n${space}");
        replace = Regex.Replace(replace, @"\[(\w+)\]", "${$1}");

        var matches = Regex.Matches(input, pattern);
        if (!matches.Any()) return input;
        var multiLineGroupsKvps = multiLineGroups.Select(group =>
        {
            replace = replace.Replace($"${{{group}}}", $"[{group}]"); //replaces ${id} with [id]
            return new KeyValuePair<string, IEnumerable<IEnumerable<string>>>(group, matches.Select(match =>
            {
                var firstLine = match.Groups[$"{group}FirstLine"].Captures;
                var nextLines = match.Groups[$"{group}NextLines"].Captures;
                return nextLines.Select(capture => capture.Value).Prepend(firstLine[0].Value);
            }));
        }).ToArray();

        var result = Regex.Replace(input, pattern, replace);
        foreach (var kvp in multiLineGroupsKvps)
        {
            var postMatches = Regex.Matches(result, string.Format(@"(?<space>[^\S\r\n]+)(?<before>.+?)?\[{0}\](?<after>.+?)?\r\n", kvp.Key));
            var ccc = postMatches.ElementAt(0).Groups;
            for (int i = kvp.Value.Count() - 1; i >= 0; i--)
            {
                var lines = kvp.Value.ElementAt(i);
                var segmentCount = postMatches.Count() / kvp.Value.Count();
                var jInit = segmentCount * (i + 1);
                for (int j = jInit - 1; j >= jInit - segmentCount; j--)
                {
                    var match = postMatches.ElementAt(j);
                    var rep = string.Join("\r\n", lines.Select(line => match.Groups["space"].Value + match.Groups["before"] + line + match.Groups["after"])) + "\r\n";
                    result = result[..match.Index] + rep + result[(match.Index + match.Length)..];
                }
            }
        }

        return result;
    }
}