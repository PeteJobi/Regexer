using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Range = FastColoredTextBoxNS.Range;

namespace RegexerUI
{
    public class FctbManager
    {
        private static readonly Brush BaseBrush = Brushes.Gainsboro;
        public static readonly TextStyle BaseStyle = new(null, BaseBrush, FontStyle.Bold);
        private readonly TextStyle _separatorStyle = new(Brushes.BlueViolet, BaseBrush, FontStyle.Bold);
        private readonly TextStyle _keyLetterStyle = new(Brushes.CornflowerBlue, BaseBrush, FontStyle.Bold);
        private readonly TextStyle _keyLetterForRegexStyle = new(Brushes.CornflowerBlue, BaseBrush, FontStyle.Bold);
        private readonly TextStyle _regexStyle = new(Brushes.Brown, BaseBrush, FontStyle.Bold);
        private readonly TextStyle _baseLightStyle = new(null, new SolidBrush(Color.FromArgb(235, 235, 235)), FontStyle.Regular);
        private readonly TextStyle _baseNestedStyle = new(null, BaseBrush, FontStyle.Bold);
        public static readonly TextStyle MatchStyle = new(null, BaseBrush, FontStyle.Regular);
        public static readonly TextStyle MatchLightStyle = new(null, new SolidBrush(Color.FromArgb(235, 235, 235)), FontStyle.Regular);
        private static readonly string SubMatchPattern = @$"(?<{Style.BaseLight}>(?>\[\[(?<Open>)|(?<-Open>\]\])|\[(?!\[)|\](?!\])|[^\[\]])*(?(Open)(?!)))";
        private readonly string[] _patterns =
        {
            @$"\[\[\w+?(?:(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>(ml|[wdsgol]+)))?\]\]",
            @$"\[\[(?:\w+)?(?<{Style.Regex}>\{{[^\r\n]+?\}})\]\]",
            @$"\[\[(?:\w+(?<{Style.Separator}>\|))?(?<{Style.KeyLetter}>u)(?<{Style.Separator}>\|)(?<{Style.BaseLight}>[^\r\n]+)\]\]",
            @$"\[\[(?:\w+(?<{Style.Separator}>\|))?(?<{Style.KeyLetter}>m)(?<{Style.Separator}>\|)(?<{Style.Regex}>\{{[^\r\n]+?\}})(?:(?<{Style.Separator}>\|){SubMatchPattern})?\]\]"
        };
        private readonly string[] _replacePatterns =
        {
            @$"\[\[\w+?(?:(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>c)(?<{Style.Separator}>:)(?<{Style.KeyLetter}>u|l|s|fu|fl))?\]\]",
            @$"\[\[\w+(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>o)(?<{Style.Separator}>:){SubMatchPattern}\]\]",
            @$"\[\[\w+(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>u)(?:(?<{Style.Separator}>:){SubMatchPattern})?\]\]",
            @$"\[\[\w+(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>m)(?<{Style.Separator}>:)(?<{Style.Regex}>[^\r\n]+?)(?:(?<{Style.Separator}>:){SubMatchPattern})\]\]",
            @$"\[\[\w+(?<{Style.Separator}>\|)(?<{Style.KeyLetter}>d)(?<{Style.Separator}>:)(?<{Style.KeyLetter}>\d+|[\di+*/%-]+)(?:(?<{Style.Separator}>:)(?<{Style.Regex}>[^\r\n]+?))\]\]",
        };
        private readonly Regex _newLineRegex = new("<ml>");
        private readonly Style[] _allStyles = Enum.GetValues(typeof(Style)).Cast<Style>().ToArray();


        public void Highlight(FastColoredTextBox textBox, Range range, bool isReplaceText, bool isNested = false)
        {
            range.ClearStyle(BaseStyle, _separatorStyle, _keyLetterStyle, _keyLetterForRegexStyle, _regexStyle);
            if (!isNested) range.ClearStyle(_baseLightStyle);
            var start = range.Start.iChar;
            for (var i = 0; i < range.Start.iLine; i++)
            {
                start += textBox.GetLine(i).Length + "\r\n".Length;
            }
            var patterns = isReplaceText ? _replacePatterns : _patterns;
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(range.Text, pattern);
                if (!matches.Any()) continue;
                foreach (Match match in matches)
                {
                    textBox.GetRange(start + match.Index, start + match.Index + match.Length).SetStyle(isNested ? _baseNestedStyle : BaseStyle);
                    foreach (var style in _allStyles)
                    {
                        var group = match.Groups[style.ToString()];
                        if (group.Success)
                        {
                            switch (style)
                            {
                                case Style.Separator:
                                    foreach (Capture capture in group.Captures)
                                    {
                                        textBox.GetRange(start + capture.Index, start + capture.Index + capture.Length).SetStyle(_separatorStyle);
                                    }
                                    break;
                                case Style.KeyLetter:
                                    textBox.GetRange(start + group.Index, start + group.Index + group.Length).SetStyle(_keyLetterStyle);
                                    break;
                                case Style.Regex:
                                    textBox.GetRange(start + group.Index, start + group.Index + group.Length).SetStyle(_regexStyle);
                                    textBox.GetRange(start + group.Index, start + group.Index + group.Length).SetStyle(_keyLetterForRegexStyle, _newLineRegex);
                                    break;
                                case Style.BaseLight:
                                    var r = textBox.GetRange(start + group.Index, start + group.Index + group.Length);
                                    r.SetStyle(_baseLightStyle);
                                    Highlight(textBox, r, isReplaceText, true);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            }
        }

        enum Style{ Separator, KeyLetter, Regex, BaseLight }
    }
}
