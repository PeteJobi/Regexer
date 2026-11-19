using System;
using System.Collections.Generic;
using System.Text;

namespace RegexerV2
{
    internal static class CopyObjectWithMods
    {
        private static Branch ModificationsToBranch(int currentLevel, Modification[] modifications)
        {
            var propsProcessed = new Dictionary<string, LinearBase>();
            foreach (var modification in modifications)
            {
                if (currentLevel >= modification.Path.Length) continue;
                var currentProperty = modification.Path[currentLevel];
                if (currentLevel == modification.Path.Length - 1)
                {
                    if (!propsProcessed.ContainsKey(currentProperty))
                    {
                        propsProcessed.Add(currentProperty, modification.Value is ArrayItemValue arrayItemValue
                            ? new ArrayItemLeaf
                            {
                                Property = currentProperty,
                                ArrayItemValue = arrayItemValue
                            }
                            : new Leaf
                            {
                                Property = currentProperty,
                                Value = modification.Value
                            });
                    }
                    continue;
                }

                if (!propsProcessed.ContainsKey(currentProperty))
                {
                    propsProcessed[currentProperty] = new Linear
                    {
                        Property = currentProperty,
                        Branch = ModificationsToBranch(currentLevel + 1, modifications)
                    };
                }
            }

            return propsProcessed.Count switch
            {
                0 => throw new InvalidOperationException(),
                1 => propsProcessed.Values.ElementAt(0),
                _ => new Fork { Branches = [.. propsProcessed.Values] }
            };
        }

        public static Branch ModificationsToBranch(Modification[] modifications) => ModificationsToBranch(0, modifications);

        public static object Copy(object source, Branch path, bool byPassConstructors = false)
        {
            var branches = path switch
            {
                LinearBase linear => [linear],
                Fork fork => fork.Branches,
                _ => throw new ArgumentOutOfRangeException(nameof(path), path, null)
            };
            var willBeModified = branches.Select(lb => lb.Property).ToHashSet();
            if (!source.GetType().IsArray)
            {
                var dest = Activator.CreateInstance(source.GetType(), byPassConstructors) ?? throw new NullReferenceException();
                var sourceProps = source.GetType().GetProperties();
                foreach (var sourceProp in sourceProps)
                {
                    if (!willBeModified.Contains(sourceProp.Name))
                    {
                        //Set properties that won't be modified
                        dest.GetType().GetProperty(sourceProp.Name)!.SetValue(dest, sourceProp.GetValue(source));
                        continue;
                    }

                    foreach (var branch in branches)
                    {
                        if (branch.Property != sourceProp.Name) continue;

                        switch (branch)
                        {
                            case Leaf leaf:
                                dest.GetType().GetProperty(leaf.Property).SetValue(dest, leaf.Value);
                                break;
                            case Linear linear:
                                var newSource = sourceProp.GetValue(source) ?? throw new NullReferenceException();
                                var newDest = Copy(newSource, linear.Branch, byPassConstructors);
                                dest.GetType().GetProperty(linear.Property).SetValue(dest, newDest);
                                break;
                        }
                    }
                }

                return dest;
            }

            var sourceArray = (Array)source;
            var resultList = new List<object?>((IEnumerable<object?>)sourceArray.Clone());
            var indexOffsets = Enumerable.Repeat(0, sourceArray.Length + 1).ToArray(); //Length + 1 in case the user wants to append (insert at the end)
            for (var i = 0; i < sourceArray.Length + 1; i++) //Length + 1 in case the user wants to append (insert at the end)
            {
                foreach (var branch in branches)
                {
                    if (branch.Property != i.ToString()) continue;

                    int index;
                    switch (branch)
                    {
                        case Leaf leaf:
                            index = int.Parse(leaf.Property);
                            resultList[IndexOffset(index)] = leaf.Value;
                            break;
                        case ArrayItemLeaf arrayItemLeaf:
                            index = int.Parse(arrayItemLeaf.Property);
                            switch (arrayItemLeaf.ArrayItemValue.Action)
                            {
                                case ArrayItemAction.Replace:
                                    resultList.RemoveAt(IndexOffset(index));
                                    resultList.InsertRange(IndexOffset(index), arrayItemLeaf.ArrayItemValue.Values);
                                    indexOffsets[index] += arrayItemLeaf.ArrayItemValue.Values.Length - 1;
                                    break;
                                case ArrayItemAction.Insert:
                                    resultList.InsertRange(IndexOffset(index), arrayItemLeaf.ArrayItemValue.Values);
                                    indexOffsets[index] += arrayItemLeaf.ArrayItemValue.Values.Length;
                                    break;
                                case ArrayItemAction.Remove:
                                    resultList.RemoveAt(IndexOffset(index));
                                    indexOffsets[index] -= 1;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case Linear linear:
                            var newSource = sourceArray.GetValue(i) ?? throw new NullReferenceException();
                            var newDest = Copy(newSource, linear.Branch, byPassConstructors);
                            index = int.Parse(linear.Property);
                            resultList[IndexOffset(index)] = newDest;
                            break;
                    }
                }
            }

            var destArray = Array.CreateInstance(source.GetType().GetElementType(), resultList.Count);
            for (var i = 0; i < destArray.Length; i++)
            {
                destArray.SetValue(resultList[i], i);
            }
            return destArray;

            int IndexOffset(int index) => indexOffsets.Take(index + 1).Sum() + index;
        }

        internal class Modification(string[] path, object value)
        {
            public string[] Path { get; set; } = path;
            public object Value { get; set; } = value;
        }

        internal class Branch;

        internal class LinearBase : Branch
        {
            public required string Property { get; set; }
        }

        internal class Linear : LinearBase
        {
            public required Branch Branch { get; set; }
        }

        internal class Fork : Branch
        {
            public required LinearBase[] Branches { get; set; }
        }

        internal class Leaf : LinearBase
        {
            public object? Value { get; set; }
        }

        internal class ArrayItemLeaf : LinearBase
        {
            public required ArrayItemValue ArrayItemValue { get; set; }
        }

        internal class ArrayItemValue(object?[] values, ArrayItemAction action)
        {
            public object?[] Values { get; set; } = values;
            public ArrayItemAction Action { get; set; } = action;
        }

        internal enum ArrayItemAction
        {
            Replace, Insert, Remove
        }

        /** Example of Branch **/
        //new Reference(SingleLineStructuresReplace,
        //    new Reference.Modification(new Reference.Linear
        //    {
        //        Property = nameof(Vary.Content),
        //        Branch = new Reference.Linear
        //        {
        //            Property = nameof(And.Contents),
        //            Branch = new Reference.Linear
        //            {
        //                Property = "1",
        //                Branch = new Reference.Fork
        //                {
        //                    Branches = [
        //                        new Reference.Leaf
        //                        {
        //                            Property = nameof(Vary.Quantifier),
        //                            Value = StructureQuantifier.ONE_OR_MORE
        //                        },
        //                        new Reference.Linear
        //                        {
        //                            Property = nameof(Vary.Content),
        //                            Branch = new Reference.Linear
        //                            {
        //                                Property = nameof(And.Contents),
        //                                Branch = new Reference.Leaf
        //                                {
        //                                    Property = "2",
        //                                    Value = MatchMultipleSpread
        //                                }
        //                            }
        //                        }
        //                    ]
        //                }
        //            }
        //        }
        //    })
        //)
    }
}
