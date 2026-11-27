using static RegexerV2.CopyObjectWithMods;

namespace RegexerV2
{
    public static class SyntaxStructure
    {
        private static readonly Symbol OpenerSymbol = new("[["){ Name = TokenName.Opener };
        private static readonly Symbol CloserSymbol = new("]]"){ Name = TokenName.Closer };
        private static readonly Symbol DemarcateSymbol = new("|"){ Name = TokenName.Demarcate };
        private static readonly Symbol Demarcate2Symbol = new("|"){ Name = TokenName.Demarcate, SuggestionName = TokenName.Demarcate2 };
        private static readonly Symbol Demarcate3Symbol = new("|"){ Name = TokenName.Demarcate, SuggestionName = TokenName.Demarcate3 };
        private static readonly Symbol DemarcateAfterLabelSymbol = new("|"){ Name = TokenName.Demarcate, SuggestionName = TokenName.DemarcateAfterLabel };
        private static readonly Symbol CloseAmountQuantifierSymbol = new(">");
        private static readonly Symbol HyphenSymbol = new("-");
        private static readonly Symbol CommaSymbol = new(",");
        private static readonly Symbol StartPlainTextSymbol = new("{");
        private static readonly Symbol EndPlainTextSymbol = new("}");
        private static readonly Symbol NewLineSymbol = new("\r\n");
        private static readonly Vary ZeroOrOneAnyPatterns = new(StructureQuantifier.ZERO_OR_ONE, AnyPattern){ ReturnNullIfNull = true };
        private static readonly Vary ZeroOrOneAnyPatternsReplace = new(StructureQuantifier.ZERO_OR_ONE, AnyPatternReplace){ ReturnNullIfNull = true };
        private static readonly FreeText LabelBeforeDemarcate = new(FreeTextType.LABEL, DemarcateAfterLabelSymbol) { Name = TokenName.Label };
        internal static readonly FreeText SingleLineFreeText = new(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, CloserSymbol, OpenerSymbol)
            { Name = TokenName.FreeText, ProceedIfInvalidAfterEncountered = CloserSymbol, CanBeEmpty = true };

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

        private static readonly Or Quantifier = new()
        {
            Contents =
            [
                new OneOrMoreNoRepeat
                {
                    Name = TokenName.Quantifier,
                    Contents =
                    [
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
                        new FreeText(FreeTextType.DIGITS, CloseAmountQuantifierSymbol, HyphenSymbol),
                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                        {
                            SuggestionName = TokenName.MaxAmountQuantifier,
                            Contents =
                            [
                                HyphenSymbol,
                                new FreeText(FreeTextType.DIGITS, CloseAmountQuantifierSymbol){ CanBeEmpty = true }
                            ]
                        }),
                        CloseAmountQuantifierSymbol,
                    ]
                }
            ]
        };

        private static readonly Reference SingleLineStructuresWithoutMultiLine = new(SingleLineStructures,
        [ //Remove MultiLine and StartOrEndOfPattern
            new Modification([.. SingleLineStructureToAnyPatternPath, "2"], new ArrayItemValue([], ArrayItemAction.Remove)),
            new Modification([.. SingleLineStructureToAnyPatternPath, "3"], new ArrayItemValue([], ArrayItemAction.Remove)),
        ]);
        private static readonly Vary MatchMultipleSingleLineStructures = new(StructureQuantifier.ZERO_OR_ONE, new And
        {
            SuggestionName = TokenName.MatchMultipleMatch,
            Contents = [
                DemarcateSymbol,
                SingleLineStructuresWithoutMultiLine
            ]
        });

        private static readonly And MultiLine = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                new Whitespace{ Type = WhitespaceType.START_OF_LINE },
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Modifier("ml"),
                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Contents = [
                        DemarcateSymbol,
                        Quantifier
                    ]
                }),
                new Reference(MatchMultipleSingleLineStructures, [new Modification([nameof(Vary.Content), nameof(And.SuggestionName)], TokenName.MultiLineMatch)]),
                CloserSymbol,
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
                SingleLineStructuresWithoutMultiLine,
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

        private static FreeText MatchIndexDigits => new(FreeTextType.DIGITS, CommaSymbol, Demarcate3Symbol);
        private static readonly And MatchIndexReplace = new()
        {
            Name = TokenName.KeyPattern,
            Contents =
            [
                OpenerSymbol,
                new Symbol("#"),
                new Vary(StructureQuantifier.ZERO_OR_ONE, new Or
                {
                    Contents = [
                        new And
                        {
                            Name = TokenName.Evaluate,
                            Contents = [
                                DemarcateAfterLabelSymbol,
                                new Modifier("e"),
                                DemarcateSymbol,
                                new FreeText(FreeTextType.EXPRESSION_WITH_IC, CloserSymbol)
                            ]
                        },
                        new And
                        {
                            Name = TokenName.Addition,
                            Contents = [
                                DemarcateAfterLabelSymbol,
                                new Modifier("a"),
                                DemarcateSymbol,
                                new Or
                                {
                                    Contents = [
                                        new OneOrMoreNoRepeat
                                        {
                                            Name = TokenName.StartMiddleEnd,
                                            Contents = [new Modifier("x"), new Modifier("y"), new Modifier("z")]
                                        },
                                        new Vary(StructureQuantifier.ONE_OR_MORE, MatchIndexDigits){ Name = TokenName.Indices, Separator = CommaSymbol },
                                        new And
                                        {
                                            Name = TokenName.StartIntervalEnd,
                                            Contents = [
                                                new Modifier("s"),
                                                new Symbol(":"),
                                                MatchIndexDigits,
                                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                {
                                                    Contents = [
                                                        CommaSymbol,
                                                        new Modifier("+"),
                                                        new Symbol(":"),
                                                        MatchIndexDigits
                                                    ]
                                                }),
                                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                                {
                                                    Contents = [
                                                        CommaSymbol,
                                                        new Modifier("e"),
                                                        new Symbol(":"),
                                                        MatchIndexDigits
                                                    ]
                                                })
                                            ]
                                        }
                                    ]
                                },
                                Demarcate2Symbol,
                                SingleLineStructuresReplace
                            ]
                        }
                    ]
                }),
                CloserSymbol
            ]
        };

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
                        new FreeText(FreeTextType.LABEL, CloserSymbol){ Name = TokenName.Label },
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
                                                        Quantifier,
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
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new FreeText(FreeTextType.LABEL, StartPlainTextSymbol){ Name = TokenName.Label }),
                                        StartPlainTextSymbol,
                                        new FreeText(FreeTextType.PLAIN_TEXT, EndPlainTextSymbol){ Name = TokenName.PlainText, CanBeEmpty = true },
                                        EndPlainTextSymbol
                                    ]
                                }
                            ]
                        },
                        new And
                        {
                            Contents = [
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Contents = [
                                        LabelBeforeDemarcate,
                                        DemarcateAfterLabelSymbol
                                    ]
                                }),
                                new And
                                {
                                    Name = TokenName.Optional,
                                    Contents = [
                                        new Modifier("o"),
                                        DemarcateSymbol,
                                        SingleLineStructuresWithoutMultiLine
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
                                        new FreeText(FreeTextType.PLAIN_TEXT, EndPlainTextSymbol){ Name = TokenName.PlainText, CanBeEmpty = true },
                                        EndPlainTextSymbol,
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            Contents = [
                                                Demarcate2Symbol,
                                                Quantifier
                                            ]
                                        }),
                                        MatchMultipleSingleLineStructures
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
                new FreeText(FreeTextType.LABEL, DemarcateAfterLabelSymbol, CloserSymbol) { Name = TokenName.Label },
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
                                    Name = TokenName.Optional,
                                    Contents =
                                    [
                                        new Or
                                        {
                                            Contents = [new Modifier("oi"), new Modifier("o")]
                                        },
                                        DemarcateSymbol,
                                        new Reference(SingleLineStructuresReplace,
                                        [//Remove MultiLineReplace
                                            new Modification([..SingleLineStructureToAnyPatternPath, "3"], new ArrayItemValue([], ArrayItemAction.Remove))
                                        ])
                                    ]
                                },
                                new And
                                {
                                    Name = TokenName.Duplicate,
                                    Contents = [
                                        new Modifier("d"),
                                        DemarcateSymbol,
                                        new FreeText(FreeTextType.EXPRESSION_WITH_ICJD, DemarcateSymbol, CloserSymbol),
                                        new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                        {
                                            SuggestionName = TokenName.DuplicateSeparator,
                                            Contents = [
                                                DemarcateSymbol,
                                                new FreeText(FreeTextType.PLAIN_TEXT, CloserSymbol){ Name = TokenName.PlainText, CanBeEmpty = true }
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
                                        new FreeText(FreeTextType.EXPRESSION_WITH_ICJDM, CloserSymbol),
                                    ]
                                }
                            ]
                        }
                    ]
                }),
                CloserSymbol
            ]
        };

        private static readonly Vary MatchMultipleReorder = new(StructureQuantifier.ZERO_OR_ONE, new And
        {
            Name = TokenName.MatchMultipleReorder,
            Contents =
            [
                Demarcate2Symbol,
                new Or
                {
                    Contents =
                    [
                        new Symbol("<>"),
                        new Vary(StructureQuantifier.ONE_OR_MORE, new And
                        {
                            Name = TokenName.MatchMultipleSortByCapture,
                            Contents =
                            [
                                new Or { Contents = [new Symbol(">"), new Symbol("<")] },
                                new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                                {
                                    Contents =
                                    [
                                        new Symbol(":"),
                                        new FreeText(FreeTextType.LABEL, CommaSymbol, Demarcate2Symbol, 
                                            CloserSymbol) { Name = TokenName.Label }
                                    ]
                                })
                            ]
                        }) { Separator = CommaSymbol }
                    ]
                }
            ]
        });

        private static readonly Vary MatchMultipleSingleLineStructuresReplace = new(StructureQuantifier.ZERO_OR_ONE, new And
        {
            SuggestionName = TokenName.MatchMultipleReplace,
            Contents =
            [
                Demarcate3Symbol,
                new Reference(SingleLineStructuresReplace, [
                    new Modification(SingleLineStructureToAnyPatternPath, new Structure[] { 
                        PatternReplace,
                        new Reference(MatchIndexReplace, [
                            new Modification([nameof(And.Contents), "1"], new Or { Contents = [new Symbol("##"), new Symbol("#")] }),
                            new Modification([nameof(And.Contents), "3", nameof(Vary.Content), nameof(And.Contents), "5"], new Reference(SingleLineStructuresReplace, [
                                new Modification(SingleLineStructureToAnyPatternPath, new Structure[] { PatternReplace }) //Replace SingleLineStructuresReplace in the MatchIndexReplace with a version that only allows PatternReplace
                            ])),
                        ])
                    })
                ])
            ]
        });

        private static readonly And MatchMultipleReplace = new()
        {
            Name = TokenName.MatchMultiple,
            Contents =
            [
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Modifier("m"),
                DemarcateSymbol,
                new FreeText(FreeTextType.PLAIN_TEXT, DemarcateSymbol, CloserSymbol) { Name = TokenName.PlainText, CanBeEmpty = true },
                MatchMultipleReorder,
                MatchMultipleSingleLineStructuresReplace,
                CloserSymbol
            ]
        };

        private static readonly And MultiLineReplace = new()
        {
            Name = TokenName.MultiLine,
            Contents = [
                new Whitespace{ Type = WhitespaceType.START_OF_LINE },
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, OpenerSymbol){ Name = TokenName.FreeText, CanBeEmpty = true },
                OpenerSymbol,
                LabelBeforeDemarcate,
                DemarcateAfterLabelSymbol,
                new Modifier("ml"),
                MatchMultipleReorder,
                new Reference(MatchMultipleSingleLineStructuresReplace, [new Modification([nameof(Vary.Content), nameof(And.SuggestionName)], TokenName.MultiLineReplace)]),
                CloserSymbol,
                new FreeText(FreeTextType.SINGLE_LINE_PRE_PATTERN_TEXT, NewLineSymbol){ Name = TokenName.FreeText, CanBeEmpty = true },
                new Whitespace{ Type = WhitespaceType.END_OF_LINE }
            ]
        };

        private static readonly Or StartOrEndOfPattern = new()
        {
            Contents = [
                new Anchor(AnchorType.START_OF_PATTERN, new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Name = TokenName.KeyPattern,
                    Contents = [
                        OpenerSymbol,
                        new Symbol("^"),
                        CloserSymbol
                    ]
                })),
                new Anchor(AnchorType.END_OF_PATTERN, new Vary(StructureQuantifier.ZERO_OR_ONE, new And
                {
                    Name = TokenName.KeyPattern,
                    Contents = [
                        OpenerSymbol,
                        new Symbol("$"),
                        CloserSymbol
                    ]
                }))
            ]
        };

        private static readonly Or AnyPattern = new() { Contents = [Pattern, Unordered, MultiLine, StartOrEndOfPattern] };

        private static readonly Or AnyPatternReplace = new() { Contents = [PatternReplace, MatchMultipleReplace, UnorderedReplace, MultiLineReplace, MatchIndexReplace] };

        public static readonly Vary FindStructure = new (StructureQuantifier.ZERO_OR_MORE, new Or
                {
                    Contents = [
                        MultiLine,
                        new And //Other patterns
                        {
                            Contents =
                            [
                                new FreeText(FreeTextType.PRE_PATTERN_TEXT, OpenerSymbol, NewLineSymbol){ Name = TokenName.FreeText, CanBeEmpty = true },
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
                        new FreeText(FreeTextType.PRE_PATTERN_TEXT, OpenerSymbol, NewLineSymbol){ Name = TokenName.FreeText, CanBeEmpty = true },
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
            PRE_PATTERN_TEXT, SINGLE_LINE_PRE_PATTERN_TEXT, LABEL, PLAIN_TEXT, DIGITS, EXPRESSION_WITH_IC, EXPRESSION_WITH_ICJD, EXPRESSION_WITH_ICJDM
        }
        public enum StructureQuantifier { ZERO_OR_ONE, ZERO_OR_MORE, ONE_OR_MORE }
        public enum WhitespaceType { NONE, START_OF_LINE, END_OF_LINE }
        public enum AnchorType { START_OF_PATTERN, END_OF_PATTERN }

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
            public bool CanBeEmpty { get; set; }
            public Symbol? ProceedIfInvalidAfterEncountered { get; set; }
            public override string ToString() => Type.ToString();
        }
        public class Whitespace : Structure
        {
            public WhitespaceType Type { get; set; }
            public override string ToString() => Type switch
            {
                WhitespaceType.START_OF_LINE => "start of line",
                WhitespaceType.END_OF_LINE => "end of line",
                _ => "whitespace"
            };
        }
        public class And : Structure
        {
            public required Structure[] Contents { get; set; }
            public override string ToString() => Name == TokenName.SingleLineStructure ? nameof(SingleLineStructures) : string.Join<Structure>(", ", Contents);
        }
        public class Anchor(AnchorType type, Structure content) : Structure
        {
            public AnchorType Type { get; set; } = type;
            public Structure Content { get; set; } = content;
            public override string ToString() => Type switch
            {
                AnchorType.START_OF_PATTERN => $"^({Content})",
                AnchorType.END_OF_PATTERN => $"({Content})$",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private class Reference(Structure referenceNode, Modification[] modifications) : Structure
        {
            public readonly Structure CurrentNode = referenceNode;
            public readonly Modification[] Modifications = modifications;
        }

        public enum TokenName
        {
            None, FreeText, PlainText, Pattern, KeyPattern, Label, RestrictionQuantifierNewLine, ExactAmountQuantifier, MaxAmountQuantifier, Optional, Symbol, Modifier, MultiLine, Unordered, UnorderedGroup, Opener, Closer, Demarcate, Demarcate2,
            Demarcate3, SingleLineStructure, MatchMultiple, MatchMultipleReplace, MatchMultipleReorder, MatchMultipleSortByCapture, MatchMultipleMatch, MultiLineReplace, MultiLineMatch, Duplicate, DuplicateSeparator, Capitalize, Evaluate,
            DemarcateAfterLabel, Quantifier, StartMiddleEnd, StartIntervalEnd, Indices, Addition
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
