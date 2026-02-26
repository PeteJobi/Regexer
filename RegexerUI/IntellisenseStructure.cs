using RegexerUI;
using RegexerUI.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal static class IntellisenseStructure
{
    public const string LabelSuggestions = nameof(LabelSuggestions);

    private static SuggestionItem CloseTag(Func<GroupCollection, bool>? shouldHide) => new("]]", "]]", GetResource("CloseTagTitle"), GetResource("CloseTagText"))
    {
        ShouldHide = shouldHide,
        IntellisenseData = new IntellisenseData(@"\]")
        {
            SuggestionItems = new[]
            {
                new SuggestionItem("]", "]", "Close tag", "Close the tag with ]]"){ ShouldHide = shouldHide }
            }
        }
    };
    private static SuggestionItem AddTextHere(string text, Func<GroupCollection, bool>? shouldHide = null) =>
        new($"(Type {text} here)", string.Empty, string.Empty, string.Empty) { ShouldHide = shouldHide };

    private static SuggestionItem MlKeyword(Func<GroupCollection, bool>? shouldHide = null) => 
        new("<ml>", "<ml>", GetResource("AddLineBreakTitle"), GetResource("AddLineBreakText")){ ShouldHide = shouldHide };

    public static readonly IntellisenseData PatternStructure = new IntellisenseData(@"\[\[(?<name>\w+(?<separator>\|)?)?", true)
    {
        SuggestionItems = new[]
        {
            AddTextHere("match label", group => group["separator"].Success),
            new SuggestionItem("u <Unordered text to match>", "u|", GetResource("UnorderedMatchTitle"), GetResource("UnorderedMatchText"))
            {
                ShouldHide = group => group["name"].Success && !group["separator"].Success,
                IntellisenseData = new IntellisenseData("u")
                {
                    SuggestionItems = new []
                    {
                        new SuggestionItem("| <Match>", "|", GetResource("UnorderedMatchTitle"), GetResource("UnorderedMatchText"))
                        {
                            ShouldHide = group => group["name"].Success && !group["separator"].Success,
                            IntellisenseData = new IntellisenseData(@"\|(?<line>.+)?", true)
                            {
                                SuggestionItems = new[]
                                {
                                    AddTextHere("line/phrase to match", group => group["name"].Success && !group["separator"].Success),
                                    CloseTag(group => (group["name"].Success && !group["separator"].Success) || !group["line"].Success)
                                }
                            }
                        }
                    }
                }
            },
            new SuggestionItem("m <Separator | Multiple text to match>", "m|{", GetResource("MultipleMatchTitle"), GetResource("MultipleMatchText"))
            {
                ShouldHide = group => group["name"].Success && !group["separator"].Success,
                IntellisenseData = new IntellisenseData(@"m(?<hasBarAfterM>\|(?<hasBracAfterM>\{)?)?")
                {
                    SuggestionItems = new []
                    {
                        new SuggestionItem("| <Separator>", "|{", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText"))
                        {
                            ShouldHide = group => (group["name"].Success && !group["separator"].Success) || group["hasBarAfterM"].Success
                        },
                        new SuggestionItem("{ <Separator>", "{", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText"))
                        {
                            ShouldHide = group => (group["name"].Success && !group["separator"].Success) || !group["hasBarAfterM"].Success || group["hasBracAfterM"].Success,
                            IntellisenseData = new IntellisenseData(@"(?<secondSeparator>[^}]+(?<closed>\})?)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("separator", group => (group["name"].Success && !group["separator"].Success) || !group["hasBarAfterM"].Success || !group["hasBracAfterM"].Success || group["closed"].Success),
                                    MlKeyword(group => (group["name"].Success && !group["separator"].Success) || !group["hasBarAfterM"].Success || !group["hasBracAfterM"].Success || group["closed"].Success),
                                    new SuggestionItem("} <Close separator>", "}", GetResource("MultipleMatchSeparatorTitle"), GetResource("MultipleMatchSeparatorText"))
                                    {
                                        ShouldHide = group => (group["name"].Success && !group["separator"].Success) || !group["hasBarAfterM"].Success || group["closed"].Success || !group["secondSeparator"].Success
                                    },
                                    new SuggestionItem("| <Match>", "|", GetResource("MultipleMatchMatchTitle"), GetResource("MultipleMatchMatchText"))
                                    {
                                        ShouldHide = group => !group["hasBarAfterM"].Success || !group["closed"].Success,
                                        IntellisenseData = new IntellisenseData(@"\|(?<line>.+)?", true)
                                        {
                                            SuggestionItems = new[]
                                            {
                                                AddTextHere("line/phrase to match", group => !group["hasBarAfterM"].Success),
                                                CloseTag(group => !group["hasBarAfterM"].Success || !group["line"].Success)
                                            }
                                        }
                                    },
                                    CloseTag(group => !group["hasBarAfterM"].Success || !group["secondSeparator"].Success || !group["closed"].Success)
                                }
                            }
                        },
                    }
                }
            },
            new SuggestionItem("{ <Regex>", "{", GetResource("CustomRegexTitle"), GetResource("CustomRegexText"))
            {
                ShouldHide = group => group["separator"].Success,
                IntellisenseData = new IntellisenseData(@"\{(?<regex>[^}]+(?<closed>\})?)?", true)
                {
                    SuggestionItems = new []
                    {
                        AddTextHere("regex", group => group["separator"].Success),
                        MlKeyword(group => group["separator"].Success),
                        new SuggestionItem("} <Close regex>", "}]]", GetResource("CustomRegexTitle"), GetResource("CustomRegexText"))
                        {
                            ShouldHide = group => group["separator"].Success || !group["regex"].Success || group["closed"].Success
                        },
                        CloseTag(group => group["separator"].Success || !group["closed"].Success)
                    }
                }
            },
            new SuggestionItem("| <Modifiers>", "|", GetResource("ModifierTitle"), GetResource("ModifierText"))
            {
                ShouldHide = group => !group["name"].Success || group["separator"].Success,
                IntellisenseData = new IntellisenseData(@"\|(?<modifiers>[wdsgol]+)?")
                {
                    SuggestionItems = new []
                    {
                        new SuggestionItem("w <Word>", "w", GetResource("CustomRegexWordTitle"), GetResource("CustomRegexWordText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('w')
                        },
                        new SuggestionItem("d <Digit>", "d", GetResource("CustomRegexDigitTitle"), GetResource("CustomRegexDigitText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('d')
                        },
                        new SuggestionItem("s <Whitespace>", "s", GetResource("CustomRegexSpaceTitle"), GetResource("CustomRegexSpaceText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('s')
                        },
                        new SuggestionItem("g <Greedy match>", "g", GetResource("CustomRegexGreedyTitle"), GetResource("CustomRegexGreedyText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('g')
                        },
                        new SuggestionItem("o <Optional match>", "o", GetResource("CustomRegexOptionalTitle"), GetResource("CustomRegexOptionalText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('o')
                        },
                        new SuggestionItem("l <Include line breaks>", "l", GetResource("CustomRegexLBTitle"), GetResource("CustomRegexLBText"))
                        {
                            ShouldHide = group => group["modifiers"].Value.Contains('l')
                        },
                        CloseTag(group => !group["name"].Success || group["separator"].Success || !group["modifiers"].Success)
                    }
                }
            },
            new SuggestionItem("ml <Multi line match>", "ml]]", GetResource("MultiLineTitle"), GetResource("MultiLineText"))
            {
                ShouldHide = group => !group["name"].Success || !group["separator"].Success,
                IntellisenseData = new IntellisenseData(@"m(?<hasL>l)?")
                {
                    SuggestionItems = new []
                    {
                        new SuggestionItem("l <Multi line match>", "l]]", GetResource("MultiLineTitle"), GetResource("MultiLineText"))
                        {
                            ShouldHide = group => !group["name"].Success || !group["separator"].Success || group["hasL"].Success
                        },
                        CloseTag(group => !group["name"].Success || !group["separator"].Success || !group["hasL"].Success)
                    }
                }
            },
            CloseTag(group => !group["name"].Success || group["separator"].Success)
        }
    };

    public static readonly IntellisenseData ReplaceStructure = new(@"\[\[(?<name>\w+)?", true)
    {
        SuggestionItems = new[]
        {
            new SuggestionItem(LabelSuggestions, string.Empty, string.Empty, string.Empty){ ShouldHide = _ => true }, //This suggestionItem is a placeholder for populating labels entered in the find textBox
            AddTextHere("match label"),
            new SuggestionItem("| <Replace modifier>", "|", GetResource("ReplaceModifierTitle"), GetResource("ReplaceModifierText"))
            {
                ShouldHide = group => !group["name"].Success,
                IntellisenseData = new IntellisenseData(@"\|(?<type>[oumdec]|ml)?")
                {
                    SuggestionItems = new[]
                    {
                        new SuggestionItem("o <Replace text>", "o:", GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"o:(?<replace>.+)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("replacement text"),
                                    CloseTag(group => !group["replace"].Success)
                                }
                            }
                        },
                        new SuggestionItem("u <Replace text>", "u:", GetResource("ReplaceTitle"), GetResource("ReplaceText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"u:(?<replace>.+)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("replacement text"),
                                    CloseTag(group => !group["replace"].Success)
                                }
                            }
                        },
                        new SuggestionItem("m <Separator : Replace text>", "m:", GetResource("ReplaceMultiTitle"), GetResource("ReplaceMultiText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"m:(?<separator>[^:]+)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("separator"),
                                    MlKeyword(),
                                    new SuggestionItem(": <Replace text>", ":",GetResource("ReplaceMultiReplaceTitle"), GetResource("ReplaceMultiReplaceText"))
                                    {
                                        ShouldHide = group => !group["separator"].Success,
                                        IntellisenseData = new IntellisenseData(@":(?<replace>.+)?", true)
                                        {
                                            SuggestionItems = new []
                                            {
                                                AddTextHere("replacement text"),
                                                 CloseTag(group => !group["replace"].Success)
                                            }
                                        }
                                    },
                                    CloseTag(group => !group["separator"].Success)
                                }
                            }
                        },
                        new SuggestionItem("u (No replacement)", "u]]", GetResource("NoUReplaceTitle"), GetResource("NoUReplaceText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"u", true)
                            {
                                SuggestionItems = new []
                                {
                                    CloseTag(null)
                                }
                            }
                        },
                        new SuggestionItem("m (Capture from multi match)", "m]]", GetResource("MultiMatchCaptureTitle"), GetResource("MultiMatchCaptureText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"m", true)
                            {
                                SuggestionItems = new []
                                {
                                    CloseTag(group => group["type"].Success)
                                }
                            }
                        },
                        new SuggestionItem("d <Duplication amount : Duplication text>", "d:", GetResource("DuplicationTitle"), GetResource("DuplicationText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"d:(?<eval>\d+|[\di+*/%-]+)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("duplication amount/expression"),
                                    new SuggestionItem("i <Index of match>", "i", GetResource("MatchIndexTitle"), GetResource("MatchIndexText")),
                                    new SuggestionItem("+", "+", GetResource("AdditionText"), string.Empty),
                                    new SuggestionItem("-", "-", GetResource("SubtractionText"), string.Empty),
                                    new SuggestionItem("*", "*", GetResource("MultiplicationText"), string.Empty),
                                    new SuggestionItem("/", "/", GetResource("DivisionText"), string.Empty),
                                    new SuggestionItem("%", "%", GetResource("ModuloText"), string.Empty),
                                    new SuggestionItem(": <Separator>", ":", GetResource("DuplicationSeparatorTitle"), GetResource("DuplicationSeparatorText"))
                                    {
                                        ShouldHide = group => !group["eval"].Success,
                                        IntellisenseData = new IntellisenseData(@":(?<separator>.+)?", true)
                                        {
                                            SuggestionItems = new []
                                            {
                                                AddTextHere("separator"),
                                                CloseTag(group => !group["separator"].Success)
                                            }
                                        }
                                    },
                                    CloseTag(group => !group["eval"].Success)
                                }
                            }
                        },
                        new SuggestionItem("e <New number>", "e:", GetResource("ReplaceNumberTitle"), GetResource("ReplaceNumberText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"e:(?<eval>\d+|[\dim()+*/%-]+)?", true)
                            {
                                SuggestionItems = new []
                                {
                                    AddTextHere("new number/expression"),
                                    new SuggestionItem("i <Index of match>", "i", GetResource("MatchIndexTitle"),GetResource("MatchIndexText")),
                                    new SuggestionItem("m <Original match value>", "m", GetResource("MatchValueTitle"), GetResource("MatchValueText")),
                                    new SuggestionItem("+", "+", GetResource("AdditionText"), string.Empty),
                                    new SuggestionItem("-", "-", GetResource("SubtractionText"), string.Empty),
                                    new SuggestionItem("*", "*", GetResource("MultiplicationText"), string.Empty),
                                    new SuggestionItem("/", "/", GetResource("DivisionText"), string.Empty),
                                    new SuggestionItem("%", "%", GetResource("ModuloText"), string.Empty),
                                    CloseTag(group => !group["eval"].Success)
                                }
                            }
                        },
                        new SuggestionItem("c <Capitalization>", "c:", GetResource("CapitalizationTitle"), GetResource("CapitalizationText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"c:(?<capType>u|l|s|fu|fl)?")
                            {
                                SuggestionItems = new []
                                {
                                    new SuggestionItem("u <Uppercase>", "u]]", GetResource("UppercaseText"), "")
                                    {
                                        ShouldHide = group => group["capType"].Value == "u"
                                    },
                                    new SuggestionItem("l <Lowercase>", "l]]", GetResource("LowercaseText"), "")
                                    {
                                        ShouldHide = group => group["capType"].Value == "l"
                                    },
                                    new SuggestionItem("s <Sentence case>", "s]]", GetResource("SentenceText"), "")
                                    {
                                        ShouldHide = group => group["capType"].Value == "s"
                                    },
                                    new SuggestionItem("fu <First letter uppercase>", "fu]]", GetResource("FirstUppercaseText"), "")
                                    {
                                        ShouldHide = group => group["capType"].Value == "fu"
                                    },
                                    new SuggestionItem("fl <First letter lowercase>", "fl]]", GetResource("FirstLowercaseText"), "")
                                    {
                                        ShouldHide = group => group["capType"].Value == "fl"
                                    },
                                    CloseTag(group => !group["capType"].Success || group["capType"].Value is not ("u" or "l" or "s" or "fu" or "fl"))
                                }
                            }
                        },
                        new SuggestionItem("ml (Multi line replace)", "ml]]", GetResource("MultiLineRepTitle"), GetResource("MultiLineRepText"))
                        {
                            ShouldHide = group => group["type"].Success,
                            IntellisenseData = new IntellisenseData(@"m(?<hasL>l)?")
                            {
                                SuggestionItems = new []
                                {
                                    new SuggestionItem("l (Multi line replace)", "l]]", GetResource("MultiLineRepTitle"), GetResource("MultiLineRepText"))
                                    {
                                        ShouldHide = group => group["hasL"].Success || group["type"].Success
                                    },
                                    CloseTag(group => !group["hasL"].Success)
                                }
                            }
                        },
                        new SuggestionItem(":", ":", "", "")
                        {
                            ShouldHide = group => !group["type"].Success || group["type"].Value == "ml"
                        },
                    }
                }
            },
            CloseTag(group => !group["name"].Success)
        }
    };

    private static string GetResource(string name) => Resources.ResourceManager.GetString(name);
}
