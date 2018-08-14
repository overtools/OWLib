using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using static DataTool.Helper.Logger;

namespace DataTool {
    public class ParsedArg {
        public string Type;
        public List<string> Allowed;
        public List<string> Disallowed;
        public Dictionary<string, TagValue> Tags;


        public ParsedArg Combine(ParsedArg second) {
            if (second == null) return new ParsedArg { Type = Type, Allowed = Allowed, Disallowed = Disallowed, Tags = Tags};
            Dictionary<string, TagValue> tagsNew = Tags;
            foreach (KeyValuePair<string,TagValue> tag in second.Tags) {
                tagsNew[tag.Key] = tag.Value;
            }
            return new ParsedArg {Type = Type, Allowed = Allowed.Concat(second.Allowed).ToList(), 
                Disallowed = Disallowed.Concat(second.Disallowed).ToList(), Tags = tagsNew};
        }

        public bool ShouldDo(string name, Dictionary<string, TagExpectedValue> tagVals=null) {
            if (tagVals != null) {
                foreach (KeyValuePair<string, TagExpectedValue> tagVal in tagVals) {
                    if (!Tags.ContainsKey(tagVal.Key.ToLowerInvariant())) continue;

                    if (!tagVal.Value.Values.Where(x => x != null).Any(x => {
                        TagValue tag = Tags[tagVal.Key.ToLowerInvariant()];

                        if (x.StartsWith("!")) {
                            if (tag.IsEqual(x.Remove(0, 1))) return false;
                        } else {
                            if (!tag.IsEqual(x)) return false;
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
        public List<string> Values;
        
        public TagExpectedValue(params string[] args) {
            Values = args.ToList();
        }
    }
    
    [DebuggerDisplay("ArgType: {" + nameof(Name) + "}")]
    public class QueryType {
        public string Name;
        public List<QueryTag> Tags;
    }

    [DebuggerDisplay("ArgTag: {" + nameof(Name) + "}")]
    public class QueryTag {
        public string Name;
        public List<string> Options;
        public Type ValueType = typeof(TagValue);

        public QueryTag(string name, List<string> options) {
            Name = name;
            Options = options;
        }
    }

    public class TagValue {
        public string Value;
        public QueryTag Tag;

        public virtual bool IsEqual(string query) {
            return StringEqual(query, Value);
        }

        protected bool StringEqual(string a, string b) {
            return string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public interface IQueryParser {  // I want a consistent style
        List<QueryType> QueryTypes {get;}
        Dictionary<string, string> QueryNameOverrides {get;}
    }

    public class QueryParser {
        protected virtual void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();

            Log("Error parsing query:");
            Log($"{indent + 1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log(
                $"{indent + 1}Each query should be surrounded by \", and individual queries should be seperated by spaces");

            Log("\r\nTypes:");
            foreach (QueryType argType in types) {
                Log($"{indent + 1}{argType.Name}");
            }

            Log("\r\nTags:");

            foreach (QueryType argType in types) {
                if (argType.Tags == null) continue;
                foreach (QueryTag argTypeTag in argType.Tags) {
                    Log($"{indent + 1}{argTypeTag.Name}:");
                    foreach (string option in argTypeTag.Options) {
                        Log($"{indent + 2}{option}");
                    }
                }
                break; // erm, ok
            }
        }

        protected Dictionary<string, Dictionary<string, ParsedArg>> ParseQuery(ICLIFlags flags,
            List<QueryType> queryTypes, Dictionary<string, string> queryNameOverrides) {
            string[] result = new string[flags.Positionals.Length - 3];
            Array.Copy(flags.Positionals, 3, result, 0, flags.Positionals.Length - 3);

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes =
                new Dictionary<string, Dictionary<string, ParsedArg>>();

            foreach (string opt in result) {
                if (opt.StartsWith("--")) continue; // ok so this is a flag
                string[] split = opt.Split('|');

                string hero = split[0].ToLowerInvariant();
                if (queryNameOverrides != null && queryNameOverrides.ContainsKey(hero)) {
                    hero = queryNameOverrides[hero];
                }

                string[] afterOpts = new string[split.Length - 1];
                Array.Copy(split, 1, afterOpts, 0, split.Length - 1);

                parsedTypes[hero] = new Dictionary<string, ParsedArg>();

                if (afterOpts.Length == 0) {
                    foreach (QueryType type in queryTypes) {
                        parsedTypes[hero][type.Name] = new ParsedArg {
                            Type = type.Name,
                            Allowed = new List<string> {"*"},
                            Disallowed = new List<string>(),
                            Tags = new Dictionary<string, TagValue>()
                        };
                    }
                    // everything for this hero
                } else {
                    foreach (string afterHeroOpt in afterOpts) {
                        string[] afterSplit = afterHeroOpt.Split('=');

                        string type = afterSplit[0].ToLowerInvariant();
                        
                        List<QueryType> types = new List<QueryType>();
                        if (type == "*") {
                            types = queryTypes;
                        } else {
                            types.Add(queryTypes.FirstOrDefault(x => x.Name.ToLowerInvariant() == type));
                        }


                        foreach (QueryType typeObj in types) {
                            if (typeObj == null) {
                            Log($"\r\nUnknown type: {type}\r\n");
                            QueryHelp(queryTypes);
                            return null;
                        }

                        parsedTypes[hero][typeObj.Name] = new ParsedArg {
                            Type = typeObj.Name,
                            Allowed = new List<string>(),
                            Disallowed = new List<string>(),
                            Tags = new Dictionary<string, TagValue>()
                        };

                        string[] items = new string[afterSplit.Length - 1];
                        Array.Copy(afterSplit, 1, items, 0, afterSplit.Length - 1);
                        items = string.Join("=", items).Split(',');
                        bool isBracket = false;
                            foreach (string item in items) {
                                string realItem = item.ToLowerInvariant();
                                bool nextNotBracket = false;

                                if (item.StartsWith("(") && item.EndsWith(")")) {
                                    realItem = item.Remove(0, 1);
                                    realItem = realItem.Remove(realItem.Length - 1);
                                    isBracket = true;
                                    nextNotBracket = true;
                                } else if (item.StartsWith("(")) {
                                    isBracket = true;
                                    realItem = item.Remove(0, 1);
                                } else if (item.EndsWith(")")) {
                                    nextNotBracket = true;
                                    realItem = item.Remove(item.Length - 1);
                                }

                                if (!isBracket) {
                                    if (!realItem.StartsWith("!")) {
                                        parsedTypes[hero][typeObj.Name].Allowed.Add(realItem);
                                    } else {
                                        parsedTypes[hero][typeObj.Name].Disallowed.Add(realItem.Remove(0, 1));
                                    }
                                } else {
                                    string[] kv = realItem.Split('=');
                                    string tagName = kv[0].ToLowerInvariant();
                                    string tagValue = kv[1].ToLowerInvariant();
                                    QueryTag tagObj =
                                        typeObj.Tags.FirstOrDefault(x => x.Name.ToLowerInvariant() == tagName);
                                    if (tagObj == null) {
                                        Log($"\r\nUnknown tag: {tagName}\r\n");
                                        QueryHelp(queryTypes);
                                        return null;
                                    }

                                    TagValue valueObject = (TagValue)Activator.CreateInstance(tagObj.ValueType);
                                    valueObject.Value = tagValue;
                                    valueObject.Tag = tagObj;

                                    parsedTypes[hero][typeObj.Name].Tags[tagName] = valueObject;
                                }
                                if (nextNotBracket) isBracket = false;
                            }

                            if (parsedTypes[hero][typeObj.Name].Allowed.Count == 0 &&
                                parsedTypes[hero][typeObj.Name].Tags.Count > 0) {
                                parsedTypes[hero][typeObj.Name].Allowed = new List<string> {"*"};
                            }
                        }
                    }
                }
            }
            return parsedTypes;
        }

        protected Dictionary<string, ParsedArg> GetQuery(Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes, params string[] names) {
            Dictionary<string, ParsedArg> config = new Dictionary<string, ParsedArg>();
            foreach (string name in names) {
                if (name == null) continue;
                string theName = name.ToLowerInvariant();
                if (!parsedTypes.ContainsKey(theName)) continue;
                foreach (KeyValuePair<string,ParsedArg> parsedArg in parsedTypes[theName]) {
                    if (config.ContainsKey(parsedArg.Key)) {
                        config[parsedArg.Key] = config[parsedArg.Key].Combine(parsedArg.Value);
                    } else {
                        config[parsedArg.Key] = parsedArg.Value.Combine(null); // clone for safety
                    }
                }
            }
            return config;
        }
    }
}