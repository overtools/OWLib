using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.Helpers;
using static DataTool.Helper.Logger;

#nullable enable

namespace DataTool {
    public record ParsedArg {
        public readonly string Type;
        public HashSet<string> Allowed = [];
        public HashSet<string> Disallowed = [];
        public Dictionary<string, TagValue> Tags = new Dictionary<string, TagValue>();

        public ParsedArg(QueryType type) {
            Type = type.Name;
        }

        public ParsedArg Combine(ParsedArg? second) {
            if (second == null) return this;
            
            Dictionary<string, TagValue> combinedTags = Tags.ToDictionary();
            foreach (KeyValuePair<string, TagValue> tag in second.Tags) {
                combinedTags[tag.Key] = tag.Value;
            }

            return this with {
                Allowed = Allowed.Union(second.Allowed).ToHashSet(),
                Disallowed = Disallowed.Union(second.Disallowed).ToHashSet(),
                Tags = combinedTags
            };
        }

        public bool ShouldDo(string name, Dictionary<string, TagExpectedValue>? expectedVals = null) {
            if (expectedVals != null) {
                foreach (KeyValuePair<string, TagExpectedValue> expectedVal in expectedVals) {
                    if (!Tags.TryGetValue(expectedVal.Key.ToLowerInvariant(), out var givenValue)) continue;

                    if (!expectedVal.Value.Values.Any(x => {
                        if (x.StartsWith('!')) {
                            if (givenValue.IsEqual(x.AsSpan(1))) return false;
                        } else {
                            if (!givenValue.IsEqual(x)) return false;
                        }

                        return true;
                    })) {
                        return false;
                    }
                }
            }

            string nameReal = name.ToLowerInvariant();
            return (Allowed.Contains(nameReal) || Allowed.Contains("*")) && (!Disallowed.Contains(nameReal) || !Disallowed.Contains("*"));
        }
    }

    public class TagExpectedValue {
        public readonly HashSet<string> Values;

        public TagExpectedValue(params string[] args) {
            Values = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
        }
    }

    [DebuggerDisplay("ArgType: {" + nameof(Name) + "}")]
    public class QueryType(string name) {
        public readonly string Name = name;
        public string HumanName = "";
        public List<QueryTag> Tags = [];
        public List<string> Aliases = [];

        public string DynamicChoicesKey = "";
    }

    [DebuggerDisplay("ArgTag: {" + nameof(Name) + "}")]
    public class QueryTag(string name, string humanName, List<string> options, string? defaultValue = null) {
        public readonly string Name = name;
        public readonly string HumanName = humanName;
        public readonly List<string> Options = options;
        public readonly string? Default = defaultValue;

        public string DynamicChoicesKey = "";
    }

    public class TagValue(string value) {
        public readonly string Value = value;

        public bool IsEqual(string query) {
            return string.Equals(query, Value, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public bool IsEqual(ReadOnlySpan<char> query) {
            return query.Equals(Value, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public interface IQueryParser { // I want a consistent style
        List<QueryType> QueryTypes { get; }
        Dictionary<string, string>? QueryNameOverrides { get; }

        string DynamicChoicesKey { get; }
    }

    public class QueryParser {
        protected static readonly SymSpell SymSpell = new SymSpell(128, 4);
        
        protected virtual void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();

            Log("Error parsing query:");
            Log($"{indent + 1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be separated by spaces");
            Log($"{indent + 1}All hero and item names are in your selected locale");

            Log("\r\nTypes:");
            foreach (QueryType argType in types) {
                Log($"{indent + 1}{argType.Name} - {argType.HumanName}");
            }

            Log("\r\nTags:");

            foreach (QueryType argType in types) {
                foreach (QueryTag argTypeTag in argType.Tags) {
                    LogSL($"{indent + 1}{argTypeTag.Name}:");
                    if (argTypeTag.Default != null) {
                        TankLib.Helpers.Logger.Log24Bit(ConsoleSwatch.XTermColor.Wheat, false, Console.Out, null, $" \"{argTypeTag.Default}\"");
                    }

                    Log();
                    foreach (string option in argTypeTag.Options) {
                        Log($"{indent + 2}{option}");
                    }
                }

                // todo: lol. well this code assumes all types have the same tags
                // to be fair the non-unlock impls override this method anyway..
                break;
            }
        }

        protected Dictionary<string, Dictionary<string, ParsedArg>>? ParseQuery(
            ICLIFlags flags,
            List<QueryType> queryTypes,
            Dictionary<string, string>? queryNameOverrides = null,
            Dictionary<teResourceGUID, string>? namesForThisLocale = null) {
            if (queryTypes.Count == 0) {
                // the query parser needs to operate on at least one type
                queryTypes = [new QueryType("SyntheticType")];
            }

            var queryTypeMap = new Dictionary<string, QueryType>();
            foreach (var queryType in queryTypes) {
                queryTypeMap.Add(queryType.Name.ToLowerInvariant(), queryType);
                
                foreach (var alias in queryType.Aliases) {
                    queryTypeMap.Add(alias.ToLowerInvariant(), queryType);
                }
            }

            var inputArguments = flags.Positionals.AsSpan(3);
            if (inputArguments.Length == 0) return null;
            
            Dictionary<string, Dictionary<string, ParsedArg>> output = new Dictionary<string, Dictionary<string, ParsedArg>>();

            foreach (string opt in inputArguments) {
                if (opt.StartsWith("--")) continue; // ok so this is a flag
                string[] split = opt.Split('|');

                // okay.. we dont just parse heroes.
                // but idk which other term is most unambiguous here
                string hero = split[0].ToLowerInvariant();
                if (queryNameOverrides != null && queryNameOverrides.TryGetValue(hero, out var toolUnderstandableName)) {
                    hero = toolUnderstandableName;
                }

                if (namesForThisLocale != null && !namesForThisLocale.ContainsValue(hero)) {
                    var foundGuidForGivenName = IO.TryGetLocalizedName(0x75, hero);
                    if (foundGuidForGivenName != null && namesForThisLocale.TryGetValue(foundGuidForGivenName.Value, out var nameForThisLocale)) {
                        hero = nameForThisLocale;
                    }
                }
                
                var heroOutput = new Dictionary<string, ParsedArg>();
                output[hero] = heroOutput;

                var afterHero = split.AsSpan(1);
                if (afterHero.Length == 0) {
                    // just "Reaper"
                    // everything for this hero
                    
                    foreach (QueryType type in queryTypes) {
                        var parsedArg = new ParsedArg(type);
                        parsedArg.Allowed.Add("*"); // allow anything
                        PopulateDefaultTags(type, parsedArg);
                        
                        heroOutput.Add(type.Name, parsedArg);
                    }
                    continue;
                }

                foreach (string afterHeroOpt in afterHero) {
                    var typeNameLength = afterHeroOpt.IndexOf('=');
                    if (typeNameLength == -1) throw new Exception("invalid query");
                    var givenTypeName = afterHeroOpt.AsSpan(0, typeNameLength).ToString().ToLowerInvariant();

                    List<QueryType> typesMatchingName = [];
                    if (givenTypeName == "*") {
                        typesMatchingName = queryTypes;
                    } else if (!queryTypeMap.TryGetValue(givenTypeName, out var typeObject)) {
                        Log($"\r\nUnknown type: {givenTypeName}\r\n");
                        QueryHelp(queryTypes);
                        return null;
                    } else {
                        typesMatchingName.Add(typeObject);
                    }

                    var givenValues = afterHeroOpt.AsSpan(typeNameLength + 1).ToString().Split(',');
                    foreach (QueryType queryType in typesMatchingName) {
                        var parsedArg = new ParsedArg(queryType);
                        heroOutput.Add(queryType.Name, parsedArg);
                        // todo: using .Add here can of course fail but we would previously only use the 2nd occurrence.. its better to explode
                        
                        // todo: rewrite this parse loop...
                        bool isBracket = false;
                        foreach (string item in givenValues) {
                            string realItem = item.ToLowerInvariant();
                            bool nextNotBracket = false;

                            if (item.StartsWith('(') && item.EndsWith(')')) {
                                realItem = item.Substring(1);
                                realItem = realItem.Remove(realItem.Length - 1);
                                isBracket = true;
                                nextNotBracket = true;
                            } else if (item.StartsWith('(')) {
                                isBracket = true;
                                realItem = item.Substring(1);
                            } else if (item.EndsWith(')')) {
                                nextNotBracket = true;
                                realItem = item.Remove(item.Length - 1);
                            }

                            if (!isBracket) {
                                if (!realItem.StartsWith('!')) {
                                    parsedArg.Allowed.Add(realItem);
                                } else {
                                    parsedArg.Disallowed.Add(realItem.Substring(1));
                                }
                            } else {
                                string[] kv = realItem.Split('=');
                                string tagName = kv[0].ToLowerInvariant();
                                string tagValue = kv[1].ToLowerInvariant();
                                QueryTag? tagObj = queryType.Tags.FirstOrDefault(x => x.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
                                if (tagObj == null) {
                                    Log($"\r\nUnknown tag: {tagName}\r\n");
                                    QueryHelp(queryTypes);
                                    return null;
                                }

                                TagValue valueObject = new TagValue(tagValue);
                                parsedArg.Tags[tagName] = valueObject;
                            }

                            if (nextNotBracket) isBracket = false;
                        }

                        PopulateDefaultTags(queryType, parsedArg);

                        if (parsedArg.Allowed.Count == 0 && parsedArg.Tags.Count > 0) {
                            // the query string only gave tags
                            // set allowed to all (tag filtering still applies)
                            parsedArg.Allowed = ["*"];
                        }
                    }
                }
            }

            return output;
        }

        private static void PopulateDefaultTags(QueryType typeObj, ParsedArg parsedArg) {
            foreach (QueryTag tagObj in typeObj.Tags) {
                if (tagObj.Default == null) continue;
                            
                string tagName = tagObj.Name.ToLowerInvariant();
                // dont override user given value
                if (parsedArg.Tags.ContainsKey(tagName)) continue;

                parsedArg.Tags.Add(tagName, new TagValue(tagObj.Default));
            }
        }

        protected static Dictionary<string, ParsedArg> GetQuery(Dictionary<string, Dictionary<string, ParsedArg>> parsedHeroes, params string?[] namesToMatch) {
            Dictionary<string, ParsedArg> output = new Dictionary<string, ParsedArg>();
            foreach (string? nameToMatch in namesToMatch) {
                if (nameToMatch == null) continue;
                
                string nameInvariant = nameToMatch.ToLowerInvariant();
                if (!parsedHeroes.TryGetValue(nameInvariant, out var parsedHero)) continue;
                
                foreach (KeyValuePair<string, ParsedArg> parsedType in parsedHero) {
                    if (output.TryGetValue(parsedType.Key, out ParsedArg? existingArg)) {
                        output[parsedType.Key] = existingArg.Combine(parsedType.Value);
                    } else {
                        output[parsedType.Key] = parsedType.Value.Combine(null); // clone for safety
                    }
                }
            }

            return output;
        }
    }
}
