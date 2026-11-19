using static RegexerV2.CopyObjectWithMods;

namespace RegexerV2
{
    public static class SyntaxStructure
    {
        private static readonly Symbol OpenerSymbol = new("[["){ Name = TokenName.Opener };
        private static readonly Symbol CloserSymbol = new("]]"){ Name = TokenName.Closer };
        private static readonly Symbol DemarcateSymbol = new("|"){ Name = TokenName.Demarcate };
        private static readonly Symbol DemarcateAfterLabelSymbol = new("|"){ Name = TokenName.Demarcate, SuggestionName = TokenName.DemarcateAfterLabel };
        private static readonly Symbol CloseAmountQuantifierSymbol = new(">");
        private static readonly Symbol HyphenSymbol = new("-");
        private static readonly Symbol StartPlainTextSymbol = new("{");
        private static readonly Symbol EndPlainTextSymbol = new("}");
        private static readonly Symbol NewLineSymbol = new("\r\n");
        private static readonly Vary ZeroOrOneAnyPatterns = new(StructureQuantifier.ZERO_OR_ONE, AnyPattern){ ReturnNullIfNull = true };
        private static readonly Vary ZeroOrOneAnyPatternsReplace = new(StructureQuantifier.ZERO_OR_ONE, AnyPatternReplace){ ReturnNullIfNull = true };
        private static readonly FreeText LabelBeforeDemarcate = new(FreeTextType.LABEL, DemarcateAfterLabelSymbol) { MustNotBeEmpty = true, Name = TokenName.Label };
        internal static readonly FreeText SingleLineFreeText = new(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, CloserSymbol, OpenerSymbol){ Name = TokenName.FreeText, ProceedIfInvalidAfterEncountered = CloserSymbol };

        private static readonly string[] SingleLineStructureToAnyPatternPath =
            [nameof(Vary.Content), nameof(And.Contents), "1", nameof(Vary.Content), nameof(Or.Contents)];

        private static readonly Vary SingleLineStructures = new(StructureQuantifier.ONE_OR_MORE, new And
        {
            Name = TokenName.SingleLineStructure,
            Contents = [SingleLineFreeText, ZeroOrOneAnyPatterns]
        });

        private static readonly Vary SingleLineStructuresReplace = new(StructureQuantifier.ONE_OR_MORE, new And
        {
            Name = TokenName.SingleLineStructure,
            Contents = [SingleLineFreeText, ZeroOrOneAnyPatternsReplace]
        });

        private static readonly And MultiLineBase = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Modifier("ml"),
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents = [
                        DemarcateSymbol,
                        new OneOrMoreNoRepeat
                        {
                            Name = TokenName.Quantifier,
                            Contents = [
                                new Modifier("o"), //Zero or more lines
                                new Modifier("g") //Greedy
                            ]
                        }
                    ]
                }),
                CloserSymbol,
                new Whitespace{ Type = WhitespaceType.END_OF_LINE }
            ]
        };
        private static readonly Or MultiLine = new() { 
            Contents = [
                new Reference(MultiLineBase, [
                    new Modification([nameof(And.Contents), "0"], new ArrayItemValue([new Whitespace{ Type = WhitespaceType.START_OF_DOCUMENT }], ArrayItemAction.Insert)),
                    new Modification([nameof(And.Contents), "4", nameof(Vary.Content), nameof(And.Contents), "1", nameof(OneOrMoreNoRepeat.Contents), "0"],
                        new ArrayItemValue([], ArrayItemAction.Remove)),
                ]),
                new Reference(MultiLineBase, [new Modification([nameof(And.Contents), "0"],
                    new ArrayItemValue([new Whitespace { Type = WhitespaceType.START_OF_LINE_NOT_DOCUMENT }], ArrayItemAction.Insert))])
        ]};

        private static readonly And MultiLineReplace = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                new Whitespace{ Type = WhitespaceType.START_OF_LINE },
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, OpenerSymbol){ Name = TokenName.FreeText },
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Modifier("ml"),
                CloserSymbol,
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, NewLineSymbol){ Name = TokenName.FreeText },
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
                        LabelBeforeDemarcate,
                        DemarcateAfterLabelSymbol
                    ]
                }),
                new Modifier("u"),
                DemarcateSymbol,
                SingleLineStructures,
                CloserSymbol
            ]
        }){ Name = TokenName.UnorderedGroup };

        private static readonly Vary UnorderedReplace = new(StructureQuantifier.ONE_OR_MORE, new And
        {
            Name = TokenName.Unordered,
            Contents = [
                new Whitespace(),
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Or
                {
                    Contents = [
                        new And
                        {
                            Contents = [
                                new Modifier("ui"),
                                DemarcateSymbol,
                                SingleLineStructures
                            ]
                        },
                        new And
                        {
                            Contents = [
                                new Modifier("u"),
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Contents = [
                                        DemarcateSymbol,
                                        SingleLineStructures
                                    ]
                                })
                            ]
                        }
                    ]
                },
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
                        new Or
                        {
                            Contents = [
                                new And
                                {
                                    Contents = [
                                        LabelBeforeDemarcate,
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            Contents = [
                                                DemarcateAfterLabelSymbol,
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
                                                                new OneOrMoreNoRepeat
                                                                {
                                                                    Name = TokenName.Quantifier,
                                                                    Contents = [
                                                                        new Modifier("o"), //Zero or more
                                                                        new Modifier("g") //Greedy
                                                                    ]
                                                                },
                                                                new And
                                                                {
                                                                    Name = TokenName.ExactAmountQuantifier,
                                                                    Contents =
                                                                    [
                                                                        new Symbol("<"),
                                                                        new FreeText(FreeTextType.DIGITS, CloseAmountQuantifierSymbol, HyphenSymbol){ MustNotBeEmpty = true },
                                                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                                        {
                                                                            SuggestionName = TokenName.MaxAmountQuantifier,
                                                                            Contents =
                                                                            [
                                                                                HyphenSymbol,
                                                                                new FreeText(FreeTextType.DIGITS, CloseAmountQuantifierSymbol){ MustNotBeEmpty = true }
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
                        new And
                        {
                            Contents =
                            [
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Contents = [
                                        LabelBeforeDemarcate,
                                        DemarcateAfterLabelSymbol
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
                                            SuggestionName = TokenName.MatchMultipleMatch,
                                            Contents = [
                                                DemarcateSymbol,
                                                new Reference(SingleLineStructures, [
                                                    new Modification([..SingleLineStructureToAnyPatternPath, "2"], new ArrayItemValue([], ArrayItemAction.Remove))
                                                ])
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

        private static readonly And SimplePatternReplace = new()
        {
            Name = TokenName.Pattern,
            Contents =
            [
                OpenerSymbol,
                new FreeText(FreeTextType.LABEL, DemarcateAfterLabelSymbol, CloserSymbol) { MustNotBeEmpty = true, Name = TokenName.Label },
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents =
                    [
                        DemarcateAfterLabelSymbol,
                        new Or
                        {
                            Contents =
                            [
                                new And
                                {
                                    Name = TokenName.Duplicate,
                                    Contents = [
                                        new Modifier("d"),
                                        DemarcateSymbol,
                                        new FreeText(FreeTextType.EXPRESSION_WITH_I, DemarcateSymbol, CloserSymbol){ MustNotBeEmpty = true },
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            SuggestionName = TokenName.DuplicateSeparator,
                                            Contents = [
                                                DemarcateSymbol,
                                                new FreeText(FreeTextType.PLAIN_TEXT, CloserSymbol){ Name = TokenName.PlainText }
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

        private static readonly Reference PatternReplace = new(SimplePatternReplace, [
            new Modification(
                [nameof(And.Contents), "2", nameof(Vary.Content), nameof(And.Contents), "1", nameof(Or.Contents), "0"],
                new ArrayItemValue([
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
                            SingleLineStructuresReplace
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
                                SuggestionName = TokenName.MatchMultipleReplace,
                                Contents = [
                                    DemarcateSymbol,
                                    new Reference(SingleLineStructuresReplace, [new Modification(SingleLineStructureToAnyPatternPath, new Structure[]{ SimplePatternReplace })])
                                ]
                            })
                        ]
                    }
                ], ArrayItemAction.Insert))
        ]);

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

        public static void SetupCyclicRelationships()
        {
            ZeroOrOneAnyPatterns.Content = AnyPattern;
            ZeroOrOneAnyPatternsReplace.Content = AnyPatternReplace;
            ReplaceStructure.Content = ScanTreeToResolveReferences(ReplaceStructure.Content, []);
        }

        private class Reference(Structure referenceNode, Modification[] modifications) : Structure
        {
            public readonly Structure CurrentNode = referenceNode;
            public readonly Modification[] Modifications = modifications;
        }

        private static Structure ScanTreeToResolveReferences(Structure structure, HashSet<Structure> alreadyResolved)
        {
            switch (structure)
            {
                case Reference reference:
                    var resolvedReference = (Structure)Copy(reference.CurrentNode, ModificationsToBranch(reference.Modifications), true);
                    return ScanTreeToResolveReferences(resolvedReference, alreadyResolved);
                case Vary vary:
                    if (!alreadyResolved.Add(vary)) return structure;
                    vary.Content = ScanTreeToResolveReferences(vary.Content, alreadyResolved);
                    return vary;
                case And and:
                    if (!alreadyResolved.Add(and)) return structure;
                    for (var i = 0; i < and.Contents.Length; i++)
                    {
                        and.Contents[i] = ScanTreeToResolveReferences(and.Contents[i], alreadyResolved);
                    }
                    return and;
                case Or or:
                    if (!alreadyResolved.Add(or)) return structure;
                    for (var i = 0; i < or.Contents.Length; i++)
                    {
                        or.Contents[i] = ScanTreeToResolveReferences(or.Contents[i], alreadyResolved);
                    }
                    return or;
            }

            return structure;
        }

        public enum FreeTextType
        {
            PRE_PATTERN_TEXT, SINGLE_LINE_PRE_PATTERN_TEXT, LABEL, PLAIN_TEXT, DIGITS, EXPRESSION_WITH_I, EXPRESSION_WITH_IM
        }
        public enum StructureQuantifier { ZERO_OR_ONE, ZERO_OR_MORE, ONE_OR_MORE }
        public enum WhitespaceType { NONE, START_OF_DOCUMENT, START_OF_LINE, START_OF_LINE_NOT_DOCUMENT, END_OF_LINE }

        public class Structure
        {
            public TokenName Name { get; set; }
            public TokenName? SuggestionName { get; set; }
        }
        public class Or : Structure
        {
            public required Structure[] Contents { get; set; }
            public override string ToString() => $"({string.Join<Structure>(" or ", Contents)})";
        }
        public class OneOrMoreNoRepeat : Or;
        public class Vary(StructureQuantifier quantifier, Structure content) : Structure
        {
            private Vary() : this(StructureQuantifier.ONE_OR_MORE, new Modifier(".")) { }

            public Structure Content { get; set; } = content;
            public StructureQuantifier Quantifier { get; init; } = quantifier;
            public Structure? Separator { get; set; }
            public bool ReturnNullIfNull { get; set; }
            public override string ToString() => $"{Quantifier switch
            {
                StructureQuantifier.ZERO_OR_ONE => "?",
                StructureQuantifier.ONE_OR_MORE => "+",
                _ => "*"
            }}({Content})";
        }
        public class Symbol(string value) : Structure
        {
            private Symbol() : this(".") { }

            public string Value { get; set; } = value;
            public override string ToString() => Value;
        }
        public class Modifier(string value) : Structure
        {
            private Modifier() : this(".") { }

            public string Value { get; set; } = value;
            public override string ToString() => Value;
        }
        public class FreeText(FreeTextType type, params Symbol[] cannotContain) : Structure
        {
            private FreeText() : this(FreeTextType.LABEL) { }

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
                WhitespaceType.START_OF_LINE or WhitespaceType.START_OF_LINE_NOT_DOCUMENT => "start of line",
                WhitespaceType.START_OF_DOCUMENT => "start of document",
                WhitespaceType.END_OF_LINE => "end of line",
                _ => "whitespace"
            };
        }
        public class And : Structure
        {
            public required Structure[] Contents { get; set; }
            public override string ToString() => Name == TokenName.SingleLineStructure ? nameof(SingleLineStructures) : string.Join<Structure>(", ", Contents);
        }

        public enum TokenName
        {
            None, FreeText, PlainText, Pattern, Label, RestrictionQuantifierNewLine, ExactAmountQuantifier, MaxAmountQuantifier, Optional, Modifier, MultiLine, Unordered, UnorderedGroup, Opener, Closer, Demarcate, SingleLineStructure,
            MatchMultiple, MatchMultipleReplace, MatchMultipleSpread, MatchMultipleMatch, Duplicate, DuplicateSeparator, Capitalize, Evaluate, DemarcateAfterLabel, Quantifier
        }
        public class Token
        {
            public int Index { get; set; }
            public int Length { get; set; }
            public string Text { get; set; }
            public TokenName Name { get; set; }
            public override string ToString() => Text;
        }
        public class FreeTextToken: Token
        {
            public List<int> EscapeIndices { get; set; } = [];
        }
        public class ComplexToken : Token
        {
            public required List<Token> Children { get; set; }
            public override string ToString() => $"({(Name != TokenName.None ? $"{Name}:" : "")}{Text} => {string.Join(", ", Children)})";
        }

        public class SuggestionToken(Structure suggestion)
        {
            public List<TokenName> Ancestors { get; set; } = [];
            public Structure Suggestion { get; set; } = suggestion;
            public int PlacementIndex { get; set; }
            public string LabelFreeTextEnteredSoFar { get; set; } = string.Empty;

            public override string ToString() => $"({(Ancestors.Count > 0 ? $"{string.Join("->", Ancestors)}: " : "")}{Suggestion})";
        }
    }
}
