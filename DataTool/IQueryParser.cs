#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TACTLib.Client.HandlerArgs;
using TankLib.Helpers;

namespace DataTool {
    public record ParsedArg {
        public readonly string Type;
        public ParsedNameSetPair Values = new ParsedNameSetPair();
        public IgnoreCaseDict<ParsedNameSetPair> Tags = new IgnoreCaseDict<ParsedNameSetPair>();

        public ParsedArg(QueryType type) {
            Type = type.Name;
        }

        public ParsedArg Combine(ParsedArg? other) {
            if (other == null) return this;

            var combinedTags = new IgnoreCaseDict<ParsedNameSetPair>();
            foreach (var thisTag in Tags) {
                combinedTags.Add(thisTag.Key, thisTag.Value);
            }
            foreach (var otherTag in other.Tags) {
                if (!combinedTags.TryGetValue(otherTag.Key, out var firstTag)) {
                    combinedTags.Add(otherTag.Key, otherTag.Value);
                    continue;
                }

                combinedTags[otherTag.Key] = firstTag.Union(otherTag.Value);
            }

            return this with {
                Values = Values.Union(other.Values),
                Tags = combinedTags
            };
        }

        public bool ShouldDo(string name, Dictionary<string, TagExpectedValue>? expectedVals = null) {
            if (Values.IsDisallowed(name)) {
                // if disallowed by name, don't attempt to match tags
                return false;
            }
            
            if (expectedVals != null) {
                foreach (KeyValuePair<string, TagExpectedValue> expectedVal in expectedVals) {
                    if (!Tags.TryGetValue(expectedVal.Key, out var givenValue)) continue;

                    var explicitlyDisallowed = false;
                    var explicitlyAllowed = false;

                    foreach (var expectedValue in expectedVal.Value.Values) {
                        explicitlyDisallowed |= givenValue.IsDisallowed(expectedValue);
                        explicitlyAllowed |= givenValue.IsAllowed(expectedValue);
                    }

                    if (explicitlyDisallowed) {
                        return false;
                    }
                    
                    if (!explicitlyAllowed) {
                        // if the tag value is not explicitly allowed or disallowed
                        // try to match by exact unlock name instead
                        // this helps with owl skins, as the tag is set to "none" by default
                        // (if we allowed glob, (leagueteam=boston) would match everything due to unspecified Allowed)
                        return Values.Allowed.MatchesNoGlob(name);
                    }
                }
            }

            return Values.IsAllowed(name);
        }
    }

    public record ParsedHero {
        public required Dictionary<string, ParsedArg> Types;
        public bool Matched = false;
    }

    public class ParsedNameSet {
        private readonly IgnoreCaseDict<ParsedName> Map;
        public int Count => Map.Count;

        public ParsedNameSet() {
            Map = new IgnoreCaseDict<ParsedName>();
        }
        
        public void Add(ReadOnlySpan<char> value) {
            Add(value.ToString());
        }

        public void Add(string value) {
            Add(new ParsedName(value));
        }

        public void Add(ParsedName name) {
            Map.TryAdd(name.Value, name);
        }

        public bool MatchesNoGlob(string name) {
            // no need to convert string to lower, dict has comparer
            if (Map.TryGetValue(name, out var exactName)) {
                exactName.Matched = true;
                return true;
            }

            return false;
        }

        public bool Matches(string name) {
            if (MatchesNoGlob(name)) {
                return true;
            }

            if (Map.TryGetValue("*", out var globName)) {
                globName.Matched = true;
                return true;
            }

            return false;
        }

        public ParsedNameSet Union(ParsedNameSet other) {
            var output = new ParsedNameSet();

            foreach (var name in Map) {
                output.Map.TryAdd(name.Key, name.Value);
            }
            foreach (var name in other.Map) {
                output.Map.TryAdd(name.Key, name.Value);
            }

            return output;
        }

        public IEnumerator<ParsedName> GetEnumerator() => Map.Values.GetEnumerator();
    }

    public class ParsedNameSetPair {
        public ParsedNameSet Allowed = new ParsedNameSet();
        public ParsedNameSet Disallowed = new ParsedNameSet();
        public ScopedSpellCheck SpellCheck = new ScopedSpellCheck();

        public void Add(ReadOnlySpan<char> value) {
            if (value.StartsWith('!')) {
                Disallowed.Add(value.Slice(1));
            } else {
                Allowed.Add(value);
            }
        }

        public bool IsDisallowed(string name) {
            SpellCheck.Add(name);
            
            return Disallowed.Matches(name);
        }

        public bool IsAllowed(string name) {
            SpellCheck.Add(name);
            
            if (Allowed.Count == 0) {
                // nothing explicitly specified as allowed = anything
                return true;
            }
            return Allowed.Matches(name);
        }
        
        public ParsedNameSetPair Union(ParsedNameSetPair other) {
            return new ParsedNameSetPair {
                Allowed = Allowed.Union(other.Allowed),
                Disallowed = Disallowed.Union(other.Disallowed),
                SpellCheck = new ScopedSpellCheck() // don't copy
            };
        }
    }

    public record ParsedName {
        public readonly string Value;
        public bool Matched = false;

        public ParsedName(string value) {
            Value = value;

            if (value == "*") {
                // we don't care to look for issues matching this
                Matched = true;
            }
        }

        public bool IsEqual(ReadOnlySpan<char> query) {
            var equal = query.Equals(Value, StringComparison.InvariantCultureIgnoreCase);
            if (equal) {
                Matched = true;
            }
            return equal;
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }
    }

    public class TagExpectedValue {
        public readonly HashSet<string> Values;

        public TagExpectedValue(params string?[] args) {
            Values = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet()!;
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

    public interface IQueryParser {
        List<QueryType> QueryTypes { get; }
        string DynamicChoicesKey { get; }
    }

    public class QueryParser {
        public static void Log(string message = "") => Logger.Log(message);

        private readonly ScopedSpellCheck RootSpellCheck = new ScopedSpellCheck();

        protected virtual void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();

            Logger.Error("Error parsing query:");
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
                    Logger.Log(null, $"{indent + 1}{argTypeTag.Name}: ", false);
                    if (argTypeTag.Default != null) {
                        Logger.Log24Bit(ConsoleSwatch.XTermColor.Wheat, false, Console.Out, null, $"\"{argTypeTag.Default}\"");
                        Log();
                        foreach (string option in argTypeTag.Options) {
                            Log($"{indent + 2}{option}");
                        }
                    } else {
                        Logger.Log(null, $"{string.Join(", ", argTypeTag.Options.ToArray())}", false);
                        Log();
                    }
                }

                // todo: lol. well this code assumes all types have the same tags
                // to be fair the non-unlock impls override this method anyway..
                break;
            }
        }

        protected Dictionary<string, ParsedHero>? ParseQuery(
            ICLIFlags flags,
            List<QueryType> queryTypes,
            IgnoreCaseDict<string>? queryNameOverrides = null,
            IgnoreCaseDict<string>? localizedNameOverrides = null) {
            if (queryTypes.Count == 0) {
                // the query parser needs to operate on at least one type
                queryTypes = [new QueryType("SyntheticType")];
            }

            var queryTypeMap = new IgnoreCaseDict<QueryType>();
            foreach (var queryType in queryTypes) {
                queryTypeMap.Add(queryType.Name, queryType);

                foreach (var alias in queryType.Aliases) {
                    queryTypeMap.Add(alias, queryType);
                }
            }
            
            // we don't want to add queryNameOverrides to skellcheck as it contains joke names (maps)
            // we don't want to add localizedNameOverrides to spellcheck as it may contain filtered hero names (npcs)
            // (the names are also all lowercase... which doesn't look great)

            var inputArguments = flags.Positionals.AsSpan(3);
            if (inputArguments.Length == 0) return null;

            Dictionary<string, ParsedHero> output = new IgnoreCaseDict<ParsedHero>();

            foreach (string opt in inputArguments) {
                if (opt.StartsWith("--")) continue; // ok so this is a flag
                string[] split = opt.Split('|');

                // okay.. we dont just parse heroes.
                // but idk which other term is most unambiguous here
                var hero = split[0];
                if (queryNameOverrides != null && queryNameOverrides.TryGetValue(hero, out var toolUnderstandableName)) {
                    hero = toolUnderstandableName;
                } 
                if (localizedNameOverrides != null && localizedNameOverrides.TryGetValue(hero, out var nameForThisLocale)) {
                    hero = nameForThisLocale;
                }

                var heroOutput = new IgnoreCaseDict<ParsedArg>();
                output[hero] = new ParsedHero {
                    Types = heroOutput
                };

                var afterHero = split.AsSpan(1);
                if (afterHero.Length == 0) {
                    // just "Reaper"
                    // everything for this hero

                    foreach (QueryType type in queryTypes) {
                        var parsedArg = new ParsedArg(type);
                        PopulateDefaultTags(type, parsedArg);

                        heroOutput.Add(type.Name, parsedArg);
                    }
                    continue;
                }

                foreach (string afterHeroOpt in afterHero) {
                    var typeNameLength = afterHeroOpt.IndexOf('=');
                    if (typeNameLength == -1) throw new Exception("invalid query");
                    var givenTypeName = afterHeroOpt.AsSpan(0, typeNameLength).ToString();
                    var givenValuesSpan = afterHeroOpt.AsSpan(typeNameLength + 1);

                    List<QueryType> typesMatchingName = [];
                    if (givenTypeName == "*") {
                        typesMatchingName = queryTypes;
                    } else if (!queryTypeMap.TryGetValue(givenTypeName, out var typeObject)) {
                        LogUnknownType(queryTypes, givenTypeName);
                        return null;
                    } else {
                        typesMatchingName.Add(typeObject);
                    }
                    
                    foreach (QueryType queryType in typesMatchingName) {
                        var parsedArg = new ParsedArg(queryType);
                        heroOutput.Add(queryType.Name, parsedArg);
                        // todo: using .Add here can of course fail but we would previously only use the 2nd occurrence.. its better to explode

                        // todo: rewrite this parse loop...
                        var insideTag = false;
                        foreach (var itemRange in givenValuesSpan.Split(',')) {
                            var itemBody = givenValuesSpan[itemRange];
                            
                            var endingTag = false;
                            if (itemBody.StartsWith('(')) {
                                insideTag = true;
                                itemBody = itemBody[1..];
                            } 
                            if (itemBody.EndsWith(')')) {
                                // todo: error if not insideTag
                                endingTag = true;
                                itemBody = itemBody[..^1];
                            }
                            // (^ can be both begin and end on same iter)

                            if (!insideTag) {
                                parsedArg.Values.Add(itemBody);
                            } else {
                                string[] kv = itemBody.ToString().Split('=');
                                
                                var tagName = kv[0];
                                QueryTag? tagObj = queryType.Tags.FirstOrDefault(x => x.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
                                if (tagObj == null) {
                                    LogUnknownTag(queryTypes, queryType, tagName);
                                    return null;
                                }
                                
                                var tagValue = kv[1];

                                if (!parsedArg.Tags.TryGetValue(tagName, out var tagValues)) {
                                    tagValues = new ParsedNameSetPair();
                                    parsedArg.Tags.Add(tagName, tagValues);
                                }
                                tagValues.Add(tagValue);
                            }

                            if (endingTag) insideTag = false;
                        }
                        
                        // todo: error if insideTag

                        PopulateDefaultTags(queryType, parsedArg);
                    }
                }
            }

            return output;
        }

        private void LogUnknownType(List<QueryType> queryTypes, string typeName) {
            Log($"\r\nUnknown type: {typeName}");
            
            var typeSpellCheck = new ScopedSpellCheck();
            foreach (var queryType in queryTypes) {
                typeSpellCheck.Add(queryType.Name);
                foreach (var alias in queryType.Aliases) {
                    typeSpellCheck.Add(alias);
                }
            }
            typeSpellCheck.LogSpellCheck(typeName);
            
            Log("\r\n\r\n");
            QueryHelp(queryTypes);
        }

        private void LogUnknownTag(List<QueryType> queryTypes, QueryType queryType, string tagName) {
            Log($"\r\nUnknown tag: {tagName}");
            
            var tagSpellCheck = new ScopedSpellCheck();
            foreach (var tagType in queryType.Tags) {
                tagSpellCheck.Add(tagType.Name);
            }
            tagSpellCheck.LogSpellCheck(tagName);
                      
            Log("\r\n\r\n");
            QueryHelp(queryTypes);
        }

        private static void PopulateDefaultTags(QueryType typeObj, ParsedArg parsedArg) {
            foreach (QueryTag tagObj in typeObj.Tags) {
                if (tagObj.Default == null) continue;

                string tagName = tagObj.Name;
                // dont override user given value
                if (parsedArg.Tags.ContainsKey(tagName)) continue;

                var pair = new ParsedNameSetPair();
                pair.Allowed.Add(new ParsedName(tagObj.Default) {
                    Matched = true // not relevant for warnings
                });
                parsedArg.Tags.Add(tagName, pair);
            }
        }

        protected Dictionary<string, ParsedArg> GetQuery(Dictionary<string, ParsedHero> parsedHeroes, params string?[] namesToMatch) {
            IgnoreCaseDict<ParsedArg> output = new IgnoreCaseDict<ParsedArg>();
            foreach (string? nameToMatch in namesToMatch) {
                if (nameToMatch == null) continue;
                
                if (!parsedHeroes.TryGetValue(nameToMatch, out var parsedHero)) {
                    RootSpellCheck.Add(nameToMatch);
                    continue;
                }

                parsedHero.Matched = true;
                foreach (KeyValuePair<string, ParsedArg> parsedType in parsedHero.Types) {
                    if (output.TryGetValue(parsedType.Key, out ParsedArg? existingArg)) {
                        output[parsedType.Key] = existingArg.Combine(parsedType.Value);
                    } else {
                        output[parsedType.Key] = parsedType.Value.Combine(null); // clone for safety
                    }
                }
            }

            return output;
        }

        public void LogUnknownQueries(Dictionary<string, ParsedHero>? parsedHeroes) {
            if (parsedHeroes == null) return;
            var anyUnknown = false;
            var unknownBaseSkin = false;
            
            foreach (var hero in parsedHeroes) {
                if (!hero.Value.Matched) {
                    LogUnknownQuery(unknownPart => unknownPart, hero.Key, RootSpellCheck);
                    anyUnknown = true;
                    continue;
                }
                
                foreach (var type in hero.Value.Types) {
                    var anyMatchedInType = type.Value.Values.Allowed.Count == 0;
                    
                    foreach (var allowed in type.Value.Values.Allowed) {
                        if (allowed.Matched) {
                            anyMatchedInType = true;
                            continue;
                        }

                        if (allowed.IsEqual("overwatch 1") || allowed.IsEqual("overwatch 2") ||
                            allowed.IsEqual("classic") || allowed.IsEqual("valorous") ||
                            allowed.IsEqual("守望先锋") || allowed.IsEqual("守望先锋归来")) {
                            // (IsEqual sets matched flag, but doesn't matter at this point)
                            unknownBaseSkin = true;
                        }
                        
                        LogUnknownQuery(unknownPart => $"{hero.Key}|{type.Key}={unknownPart}", 
                                      allowed.Value, type.Value.Values.SpellCheck);
                        anyUnknown = true;
                    }
                    
                    foreach (var tag in type.Value.Tags) {
                        if (!anyMatchedInType) break; // will be misleading
                        
                        foreach (var allowed in tag.Value.Allowed) {
                            if (allowed.Matched) continue;
                        
                            LogUnknownQuery(unknownPart => $"{hero.Key}|{type.Key}=({tag.Key}={unknownPart}", 
                                          allowed.Value, tag.Value.SpellCheck);
                            anyUnknown = true;
                        }
                    }
                }
            }

            if (!anyUnknown) return;

            var createArgs = Program.Client.CreateArgs;
            var textLanguage = createArgs.TextLanguage;
            var isEnglish = textLanguage == "enUS";
            var isChinese = textLanguage == "zhCN";
            var isChinaClient = ((ClientCreateArgs_Tank)createArgs.HandlerArgs!).ManifestRegion == ClientCreateArgs_Tank.REGION_CN;

            if (unknownBaseSkin) {
                if (isChinaClient && isEnglish) {
                    Logger.Warn("Query", "On the Chinese client, \"Overwatch 1\" skins are renamed to \"Classic\"");
                    Logger.Warn("Query", "On the Chinese client, \"Overwatch 2\" skins are renamed to \"Valorous\"");
                } else if (isChinese) {
                    Logger.Warn("Query", "In Chinese, \"Overwatch 1\" skins are renamed to \"守望先锋\"");
                    Logger.Warn("Query", "In Chinese, \"Overwatch 2\" skins are renamed to \"守望先锋归来\"");
                } else if (isChinaClient) {
                    Logger.Warn("Query", "On the Chinese client, \"Overwatch 1\" and \"Overwatch 2\" skins have different names. Check in-game");
                }
            }
            
            // ("your game language" could mean language specified to tool... idk how else to word)
            Logger.Warn("Query", $"Your game language is set to {textLanguage}. The names of any Heroes, Unlocks, Maps, etc should be entered exactly how they appear in-game using that language.");
        }

        private static void LogUnknownQuery(Func<string, string> formatter, string unknownPart, ScopedSpellCheck spellCheck) {
            Logger.Error("Query", $"Found nothing matching your query of \"{formatter(unknownPart)}\"");

            var suggestion = spellCheck.TryGetSuggestion(unknownPart);
            if (suggestion != null) {
                Logger.Warn("SpellCheck", $"Did you mean: \"{formatter(suggestion)}\" ?");
            }
        }
    }
}
