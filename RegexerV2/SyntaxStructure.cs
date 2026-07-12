using System;
using System.Collections.Generic;
using System.Text;

namespace RegexerV2
{
    internal static class SyntaxStructure
    {
        private static readonly Symbol OpenerSymbol = new("[["){ Name = TokenName.Opener };
        private static readonly Symbol CloserSymbol = new("]]"){ Name = TokenName.Closer };
        private static readonly Symbol DemarcateSymbol = new("|"){ Name = TokenName.Demarcate };
        private static readonly Symbol CloseAmountQuantifierSymbol = new(">");
        private static readonly Symbol HyphenSymbol = new("-");
        private static readonly Symbol StartPlainTextSymbol = new("{");
        private static readonly Symbol EndPlainTextSymbol = new("}");
        private static readonly Symbol NewLineSymbol = new("\r\n");
        private static readonly Symbol MatchIndexSymbol = new("i");
        private static readonly Symbol MatchValueSymbol = new("m");
        private static readonly Symbol PlusSymbol = new("+");
        private static readonly Symbol MinusSymbol = new("-");
        private static readonly Symbol MultSymbol = new("*");
        private static readonly Symbol DivSymbol = new("/");
        private static readonly Symbol ModSymbol = new("%");
        private static readonly Symbol BracketOpenSymbol = new("(");
        private static readonly Symbol BracketCloseSymbol = new(")");
        private static readonly Vary ZeroOrOneAnyPatterns = new(StructureQuantifier.ZERO_OR_ONE, AnyPattern){ ReturnNullIfEmpty = true };
        private static readonly Vary ZeroOrOneAnyPatternsReplace = new(StructureQuantifier.ZERO_OR_ONE, AnyPatternReplace){ ReturnNullIfEmpty = true };

        private static readonly Vary SingleLineStructure = new(StructureQuantifier.ZERO_OR_MORE, new And
        {
            Name = TokenName.SingleLineStructure,
            Contents = [
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, CloserSymbol, OpenerSymbol){ Name = TokenName.FreeText, ProceedIfInvalidAfterEncountered = CloserSymbol },
                ZeroOrOneAnyPatterns
            ]
        }){ ReturnNullIfEmpty = true };

        private static readonly Vary SingleLineStructureReplace = new Vary(StructureQuantifier.ZERO_OR_MORE, new And
        {
            Name = TokenName.SingleLineStructure,
            Contents = [
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, CloserSymbol, OpenerSymbol){ Name = TokenName.FreeText, ProceedIfInvalidAfterEncountered = CloserSymbol },
                ZeroOrOneAnyPatternsReplace
            ]
        }){ ReturnNullIfEmpty = true };

        private static readonly And MultiLine = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                new Whitespace{ Type = WhitespaceType.START_OF_LINE },
                OpenerSymbol,
                new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                DemarcateSymbol,
                new Modifier("ml"),
                CloserSymbol,
                new Whitespace{ Type = WhitespaceType.END_OF_LINE }
            ]
        };

        private static readonly And MultiLineReplace = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                new Whitespace{ Type = WhitespaceType.START_OF_LINE },
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, OpenerSymbol){ Name = TokenName.FreeText },
                OpenerSymbol,
                new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                DemarcateSymbol,
                new Modifier("ml"),
                CloserSymbol,
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, NewLineSymbol){ Name = TokenName.FreeText/*, ProceedIfInvalidAfterEncountered = CloserSymbol*/ },
                new Whitespace{ Type = WhitespaceType.END_OF_LINE }
            ]
        };

        private static readonly Vary Unordered = new(StructureQuantifier.ONE_OR_MORE, new And
        {
            Name = TokenName.Unordered,
            Contents = [
                new Whitespace(),
                OpenerSymbol,
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents = [
                        new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                        DemarcateSymbol
                    ]
                }),
                new Modifier("u"),
                DemarcateSymbol,
                SingleLineStructure,
                CloserSymbol
            ]
        }){ Name = TokenName.UnorderedGroup };

        private static readonly Vary UnorderedReplace = new(StructureQuantifier.ONE_OR_MORE, new And
        {
            Name = TokenName.Unordered,
            Contents = [
                new Whitespace(),
                OpenerSymbol,
                new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                DemarcateSymbol,
                new Or
                {
                    Contents = [new Modifier("ui"), new Modifier("u")]
                },
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents = [
                        DemarcateSymbol,
                        SingleLineStructure
                    ]
                }),
                CloserSymbol
            ]
        }){ Name = TokenName.UnorderedGroup };

        private static readonly And Pattern = new()
        {
            Name = TokenName.Pattern,
            Contents =
            [
                OpenerSymbol,
                new Or
                {
                    Contents =
                    [
                        new FreeText(FreeTextType.LABEL, CloserSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                        new And
                        {
                            Contents = [
                                new Or
                                {
                                    Contents = [
                                        new And
                                        {
                                            Contents = [
                                                new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                {
                                                    Contents = [
                                                        DemarcateSymbol,
                                                        new OneOrMoreNoRepeat
                                                        {
                                                            Name = TokenName.RestrictionQuantifierNewLine,
                                                            Contents =
                                                            [
                                                                new Or
                                                                {
                                                                    Contents =
                                                                    [
                                                                        new Modifier("w"),
                                                                        new Modifier("d"),
                                                                        new Modifier("s")
                                                                    ]
                                                                },
                                                                new Or
                                                                {
                                                                    Contents =
                                                                    [
                                                                        new Modifier("g"),
                                                                        new And
                                                                        {
                                                                            Name = TokenName.ExactAmountQuantifier,
                                                                            Contents =
                                                                            [
                                                                                new Symbol("<"),
                                                                                new FreeText(FreeTextType.DIGITS,
                                                                                    CloseAmountQuantifierSymbol, HyphenSymbol),
                                                                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                                                {
                                                                                    Contents =
                                                                                    [
                                                                                        HyphenSymbol,
                                                                                        new FreeText(FreeTextType.DIGITS,
                                                                                            CloseAmountQuantifierSymbol)
                                                                                    ]
                                                                                }),
                                                                                CloseAmountQuantifierSymbol,
                                                                            ]
                                                                        }
                                                                    ]
                                                                },
                                                                new Modifier("l"),
                                                            ]
                                                        },
                                                    ]
                                                })
                                            ]
                                        },
                                        new And
                                        {
                                            Contents = [
                                                new Vary(StructureQuantifier.ZERO_OR_ONE, new FreeText(FreeTextType.LABEL, StartPlainTextSymbol){ MustNotBeEmpty = true, Name = TokenName.Label }),
                                                StartPlainTextSymbol,
                                                new FreeText(FreeTextType.PLAIN_TEXT, EndPlainTextSymbol){ Name = TokenName.PlainText },
                                                EndPlainTextSymbol
                                            ]
                                        }
                                    ]
                                },
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Name = TokenName.Optional,
                                    Contents =
                                    [
                                        DemarcateSymbol,
                                        new Modifier("o")
                                    ]
                                })
                            ]
                        },
                        new And
                        {
                            Contents =
                            [
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Contents = [
                                        new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                                        DemarcateSymbol
                                    ]
                                }),
                                new And
                                {
                                    Name = TokenName.MatchMultiple,
                                    Contents =
                                    [
                                        new Modifier("m"),
                                        DemarcateSymbol,
                                        StartPlainTextSymbol,
                                        new FreeText(FreeTextType.PLAIN_TEXT, EndPlainTextSymbol){ Name = TokenName.PlainText },
                                        EndPlainTextSymbol,
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            Contents = [
                                                DemarcateSymbol,
                                                SingleLineStructure
                                            ]
                                        })
                                    ]
                                }
                            ]
                        }
                    ]
                },
                CloserSymbol
            ]
        };

        private static readonly And PatternReplace = new()
        {
            Name = TokenName.Pattern,
            Contents =
            [
                OpenerSymbol,
                new FreeText(FreeTextType.LABEL, DemarcateSymbol, CloserSymbol) { MustNotBeEmpty = true, Name = TokenName.Label },
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents =
                    [
                        DemarcateSymbol,
                        new Or
                        {
                            Contents =
                            [
                                new And
                                {
                                    Name = TokenName.Optional,
                                    Contents =
                                    [
                                        new Or
                                        {
                                            Contents = [new Modifier("oi"), new Modifier("o")]
                                        },
                                        DemarcateSymbol,
                                        SingleLineStructureReplace
                                    ]
                                },
                                new And
                                {
                                    Name = TokenName.MatchMultiple,
                                    Contents = [
                                        new Modifier("m"),
                                        DemarcateSymbol,
                                        new FreeText(FreeTextType.PLAIN_TEXT, DemarcateSymbol, CloserSymbol){ Name = TokenName.PlainText },
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            Contents = [
                                                DemarcateSymbol,
                                                new Vary(StructureQuantifier.ZERO_OR_MORE, new And
                                                {
                                                    Name = TokenName.SingleLineStructure,
                                                    Contents = [
                                                        new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, CloserSymbol, OpenerSymbol){ Name = TokenName.FreeText, ProceedIfInvalidAfterEncountered = CloserSymbol },
                                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                        {
                                                            Name = TokenName.MatchMultipleSpread,
                                                            Contents = [
                                                                OpenerSymbol,
                                                                new FreeText(FreeTextType.LABEL, DemarcateSymbol){ MustNotBeEmpty = true, Name = TokenName.Label },
                                                                DemarcateSymbol,
                                                                new Modifier("m"),
                                                                CloserSymbol
                                                            ]
                                                        }){ ReturnNullIfEmpty = true }
                                                    ]
                                                }){ ReturnNullIfEmpty = true }
                                            ]
                                        })
                                    ]
                                },
                                new And
                                {
                                    Name = TokenName.Duplicate,
                                    Contents = [
                                        new Modifier("d"),
                                        DemarcateSymbol,
                                        new FreeText(FreeTextType.EXPRESSION_WITH_I, DemarcateSymbol, CloserSymbol){ MustNotBeEmpty = true },
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            Contents = [
                                                DemarcateSymbol,
                                                new FreeText(FreeTextType.PLAIN_TEXT, /*DemarcateSymbol, */CloserSymbol){ Name = TokenName.PlainText }
                                            ]
                                        })
                                    ]
                                },
                                new And
                                {
                                    Name = TokenName.Capitalize,
                                    Contents = [
                                        new Modifier("c"),
                                        DemarcateSymbol,
                                        new Or
                                        {
                                            Contents = [
                                                new Modifier("u"),
                                                new Modifier("l"),
                                                new Modifier("s"),
                                                new Modifier("fu"),
                                                new Modifier("fl")
                                            ]
                                        }
                                    ]
                                },
                                new And
                                {
                                    Name = TokenName.Evaluate,
                                    Contents = [
                                        new Modifier("e"),
                                        DemarcateSymbol,
                                        new FreeText(FreeTextType.EXPRESSION_WITH_IM, CloserSymbol){ MustNotBeEmpty = true },
                                    ]
                                }
                            ]
                        }
                    ]
                }),
                CloserSymbol
            ]
        };

        private static readonly Or AnyPattern = new() { Contents = [Pattern, Unordered, MultiLine] };

        private static readonly Or AnyPatternReplace = new() { Contents = [PatternReplace, UnorderedReplace, MultiLineReplace] };

        public static readonly Vary FindStructure = new (StructureQuantifier.ZERO_OR_MORE, new Or
        {
            Contents = [
                MultiLine,
                new And //Other patterns
                {
                    Contents =
                    [
                        new FreeText(FreeTextType.PRE_PATTERN_TEXT, OpenerSymbol, NewLineSymbol){ Name = TokenName.FreeText },
                        ZeroOrOneAnyPatterns
                    ]
                }
            ]
        });

        public static readonly Vary ReplaceStructure = new(StructureQuantifier.ZERO_OR_MORE, new Or
        {
            Contents = [
                MultiLineReplace,
                new And //Other patterns
                {
                    Contents =
                    [
                        new FreeText(FreeTextType.PRE_PATTERN_TEXT, OpenerSymbol, NewLineSymbol){ Name = TokenName.FreeText },
                        ZeroOrOneAnyPatternsReplace
                    ]
                }
            ]
        });

        public enum FreeTextType
        {
            PRE_PATTERN_TEXT, SINGLE_LINE_PRE_PATTERN_TEXT, LABEL, PLAIN_TEXT, DIGITS, EXPRESSION_WITH_I, EXPRESSION_WITH_IM
        }
        public enum StructureQuantifier { ZERO_OR_ONE, ZERO_OR_MORE, ONE_OR_MORE }
        public enum WhitespaceType { NONE, START_OF_LINE, END_OF_LINE }

        public class Structure
        {
            public TokenName Name { get; set; }
        }
        public class Or : Structure
        {
            public required Structure[] Contents { get; set; }
            public override string ToString() => string.Join<Structure>(" or ", Contents);
        }
        public class OneOrMoreNoRepeat : Or;
        public class Vary(StructureQuantifier quantifier, Structure content) : Structure
        {
            public Structure Content { get; set; } = content;
            public StructureQuantifier Quantifier { get; init; } = quantifier;
            public bool ReturnNullIfEmpty { get; set; }
            public override string ToString() => $"{Quantifier switch
            {
                StructureQuantifier.ZERO_OR_ONE => "?",
                StructureQuantifier.ONE_OR_MORE => "+",
                _ => "*"
            }}({Content.ToString() ?? throw new InvalidOperationException()})";
        }
        public class Symbol(string value) : Structure
        {
            public string Value { get; set; } = value;
            public override string ToString() => Value;
        }
        public class Modifier(string value) : Structure
        {
            public string Value { get; set; } = value;
            public override string ToString() => Value;
        }
        public class FreeText(FreeTextType type, params Symbol[] cannotContain) : Structure
        {
            public FreeTextType Type { get; set; } = type;
            public Symbol[] CannotContain { get; set; } = cannotContain;
            public bool MustNotBeEmpty { get; set; }
            public Symbol? ProceedIfInvalidAfterEncountered { get; set; }
            public override string ToString() => Type.ToString();
        }
        public class Whitespace : Structure
        {
            public WhitespaceType Type { get; set; }
            public override string ToString() => Type switch
            {
                WhitespaceType.START_OF_LINE => "start of line", WhitespaceType.END_OF_LINE => "end of line", _ => "whitespace"
            };
        }
        public class And : Structure
        {
            public required Structure[] Contents { get; set; }
            public override string ToString() => Name == TokenName.SingleLineStructure ? nameof(SingleLineStructure) : string.Join<Structure>(", ", Contents);
        }

        public enum TokenName
        {
            None, FreeText, PlainText, Pattern, Label, RestrictionQuantifierNewLine, ExactAmountQuantifier, Optional, Modifier, MultiLine, Unordered, UnorderedGroup, Opener, Closer, Demarcate, SingleLineStructure,
            MatchMultiple, MatchMultipleSpread, Duplicate, Capitalize, Evaluate
        }
        public class Token
        {
            public int Index { get; set; }
            public int Length { get; set; }
            public string Text { get; set; }
            public TokenName Name { get; set; }
            public BacktrackContext BacktrackContext { get; set; }
            public override string ToString() => Text;
            //public override string ToString() => $"[{Index}, {Length}]";
        }
        public class FreeTextToken: Token
        {
            public List<int> EscapeIndices { get; set; } = [];
        }
        public class ComplexToken : Token
        {
            public List<Token> Children { get; set; }
            public override string ToString() => $"({(Name != TokenName.None ? $"{Name}:" : "")}{Text} => {string.Join(", ", Children)})";
            //public override string ToString() => $"({(Type != TokenType.None ? $"{Type}:" : "")}[{Index}, {Length}] => {string.Join(", ", Children)})";
        }

        public record struct BacktrackContext(int startingIndex, int endingIndex)
        {
            public int StartingIndex { get; set; } = startingIndex;
            public int EndingIndex { get; set; } = endingIndex;
        }

        public static void SetupCyclicRelationships()
        {
            ZeroOrOneAnyPatterns.Content = AnyPattern;
            ZeroOrOneAnyPatternsReplace.Content = AnyPatternReplace;
        }
    }
}
