using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Utf8Json;

namespace DataTool.Flag {
    public class FlagParser {
        public static string[] AppArgs { get; set; } = Environment.GetCommandLineArgs().Skip(1).ToArray();

        public static T Parse<T>() where T : ICLIFlags {
            return Parse<T>(null, AppArgs);
        }

        public static void CheckCollisions(Type baseType, Action<string, string> OnCollision) {
            Type type = typeof(ICLIFlags);
            IEnumerable<Type> flagSets = Assembly.GetExecutingAssembly().GetTypes().Where(x => !x.IsInterface && type.IsAssignableFrom(x) && !baseType.IsAssignableFrom(x));

            HashSet<string> baseFlags = new HashSet<string>();
            {
                FieldInfo[] fields = baseType.GetFields();
                foreach (FieldInfo field in fields) {
                    CLIFlagAttribute flagAttr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                    if (flagAttr == null) {
                        continue;
                    }

                    if (flagAttr.Positional > -1) {
                        continue;
                    }

                    baseFlags.Add(flagAttr.Flag);
                    AliasAttribute[] aliasAttrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();
                    foreach (AliasAttribute aliasAttr in aliasAttrs) {
                        baseFlags.Add(flagAttr.Flag);
                    }
                }
            }

            foreach (Type flagSet in flagSets) {
                HashSet<string> currentFlags = new HashSet<string>();
                FieldInfo[] fields = flagSet.GetFields();
                foreach (FieldInfo field in fields) {
                    CLIFlagAttribute flagAttr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                    if (flagAttr == null) {
                        continue;
                    }

                    if (flagAttr.Positional > -1) {
                        continue;
                    }

                    if (baseFlags.Contains(flagAttr.Flag) || !currentFlags.Add(flagAttr.Flag)) {
                        OnCollision(flagAttr.Flag, flagSet.FullName);
                    }
                    
                    AliasAttribute[] aliasAttrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();
                    foreach (AliasAttribute aliasAttr in aliasAttrs) {
                        if (baseFlags.Contains(aliasAttr.Alias) || !currentFlags.Add(aliasAttr.Alias)) {
                            OnCollision(aliasAttr.Alias, flagSet.FullName);
                        }
                    }
                }
            }
        }

        public static void Help<T>(bool simple) where T : ICLIFlags {
            Type iface = typeof(T);

            List<string> positonals = new List<string>();
            List<string> singulars = new List<string>();
            List<string> help = new List<string>();
            bool hasflags = false;

            FieldInfo[] fields = iface.GetFields();
            foreach (FieldInfo field in fields) {
                CLIFlagAttribute flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null) {
                    continue;
                }
                if (flagattr.Positional > -1) {
                    string x = flagattr.Flag;
                    if (!flagattr.Required) {
                        x = $"[{x}]";
                    }
                    positonals.Add(x);
                } else if (!simple) {
                    if (flagattr.Flag.Length == 1) {
                        singulars.Add(flagattr.Flag);
                    } else {
                        string x = flagattr.Flag;
                        Type t = field.FieldType;
                        if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))) {
                            x = $"[--{x} key=value]";
                        } else if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) {
                            x = $"[--{x} value]";
                        } else if (flagattr.Default != null && flagattr.NeedsValue) {
                            x = $"[--{x}[={flagattr.Default}]]";
                        } else if (flagattr.NeedsValue) {
                            x = $"[--{x}=value]";
                        } else {
                            x = $"[--{x}]";
                        }
                        help.Add(x);
                    }
                } else {
                    hasflags = true;
                }
            }

            string helpstr = AppDomain.CurrentDomain.FriendlyName;
            if (simple && hasflags) {
                helpstr += $" [--flags]";
            } else {
                if (singulars.Count > 0) {
                    helpstr += $" [-{string.Join("", singulars)}]";
                }
                if (help.Count > 0) {
                    helpstr += $" {string.Join(" ", help)}";
                }
            }
            if (positonals.Count > 0) {
                helpstr += $" {string.Join(" ", positonals)}";
            }
            Console.Out.WriteLine(helpstr);
        }

        public static void FullHelp<T>(Action extraHelp, bool skipOpener = false) where T : ICLIFlags {
            Type iface = typeof(T);

            SortedList<int, string> positionals = new SortedList<int, string>();
            int maxpositionals = "positional".Length;
            SortedList<int, string> positionalsstr = new SortedList<int, string>();

            List<string> helpflags = new List<string>();
            int maxflags = "flags".Length;
            List<string> helpstrs = new List<string>();
            int maxstrs = "help".Length;
            List<string> helpdefaults = new List<string>();

            FieldInfo[] fields = iface.GetFields();

            foreach (FieldInfo field in fields) {
                CLIFlagAttribute flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null) {
                    continue;
                }
                AliasAttribute[] aliasattrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();

                string required = "";
                if (flagattr.Required) {
                    required = " (Required)";
                }
                if (flagattr.NeedsValue) {
                    required += " (Needs Value)";
                }
                List<string> aliases = new List<string>();

                string prefix = "--";
                if (flagattr.Flag?.Length == 1) {
                    prefix = "-";
                }
                if (flagattr.Positional > -1) {
                    prefix = "";
                }

                aliases.Add(prefix + flagattr.Flag);
                foreach (AliasAttribute aliasattr in aliasattrs) {
                    prefix = "--";
                    if (aliasattr.Alias.Length == 1) {
                        prefix = "-";
                    }
                    if (flagattr.Positional > -1) {
                        prefix = "";
                    }
                    aliases.Add(prefix + aliasattr.Alias);
                }

                string helpflag = string.Join(" ", aliases);
                maxflags = Math.Max(maxflags, helpflag.Length);
                string helpstr = $"{flagattr.Help}{required}";
                maxstrs = Math.Max(maxstrs, helpstr.Length);
                string helpdefault = "";
                if (flagattr.Default != null) {
                    helpdefault = flagattr.Default.ToString();
                }

                if (flagattr.Positional > -1) {
                    positionals.Add(flagattr.Positional, helpflag);
                    maxpositionals = Math.Max(maxpositionals, helpflag.Length);
                    positionalsstr.Add(flagattr.Positional, helpstr);
                } else {
                    helpstrs.Add(helpstr);
                    helpflags.Add(helpflag);
                    helpdefaults.Add(helpdefault);
                }
            }

            if (!skipOpener) {
                Help<T>(true);
            }
            Console.Out.WriteLine();
            if (helpstrs.Count > 0) {
                if (!skipOpener) {
                    Console.Out.WriteLine("Flags:");
                }
                Console.Out.WriteLine($"  {{0, -{maxflags}}} | {{1, -{maxstrs}}} | {{2}}", "flag", "help", "default");
                Console.Out.WriteLine("".PadLeft(maxflags + maxstrs + 20, '-'));
                for (int i = 0; i < helpstrs.Count; ++i) {
                    string helpflag = helpflags[i];
                    string helpstr = helpstrs[i];
                    string helpdefault = helpdefaults[i];
                    Console.Out.WriteLine($"  {{0, -{maxflags}}} | {{1, -{maxstrs}}} | {{2}}", helpflag, helpstr, helpdefault);
                }
            }
            int maxindex = Math.Max("index".Length, ((int)Math.Floor(positionals.Count / 10d)) + 1);
            if (positionals.Count > 0) {
                Console.Out.WriteLine();
                if (!skipOpener) {
                    Console.Out.WriteLine("Positionals:");
                }
                Console.Out.WriteLine($"  {{0, -{maxindex}}} | {{1, -{maxpositionals}}} | {{2}}", "index", "positional", "help");
                Console.Out.WriteLine("".PadLeft(maxindex + maxpositionals + 30, '-'));
                foreach (KeyValuePair<int, string> pair in positionals) {
                    string positional = pair.Value;
                    string positionalstr = positionalsstr[pair.Key];
                    Console.Out.WriteLine($"  {{0, -{maxindex}}} | {{1, -{maxpositionals}}} | {{2}}", pair.Key, positional, positionalstr);
                }
            }
            extraHelp?.Invoke();
        }

        public static T Parse<T>(Action extraHelp) where T : ICLIFlags {
            return Parse<T>(extraHelp, AppArgs);
        }

        private enum FieldKind {
            Default,
            Array,
            Dictionary
        }

        public static T Parse<T>(Action extraHelp, string[] args) where T : ICLIFlags {
            if (args.Length == 0) {
                FullHelp<T>(extraHelp);
                return null;
            }

            Type iface = typeof(T);

            if (!(Activator.CreateInstance(iface) is T instance)) {
                return null;
            }

            HashSet<string> presence = new HashSet<string>();
            List<string> positionals = new List<string>();
            Dictionary<string, string> values = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, string>> dictionaryValues = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, List<string>> arrayValues = new Dictionary<string, List<string>>();

            FieldInfo[] fields = iface.GetFields();
            foreach (FieldInfo field in fields) {
                if (field.Name == "Positionals") {
                    continue;
                }
                CLIFlagAttribute flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null) {
                    continue;
                }
                Type t = field.FieldType;
                if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))) {
                    AliasAttribute[] aliasattrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();
                    if (aliasattrs.Length > 0) {
                        foreach (AliasAttribute aliasattr in aliasattrs) {
                            dictionaryValues.Add(aliasattr.Alias, new Dictionary<string, string>());
                        }
                    }
                    dictionaryValues.Add(flagattr.Flag, new Dictionary<string, string>());
                }
                if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) {
                    AliasAttribute[] aliasattrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();
                    if (aliasattrs.Length > 0) {
                        foreach (AliasAttribute aliasattr in aliasattrs) {
                            arrayValues.Add(aliasattr.Alias, new List<string>());
                        }
                    }
                    arrayValues.Add(flagattr.Flag, new List<string>());
                }
            }

            for(int i = 0; i < args.Length; ++i) {
                string arg = args[i];
                if (arg[0] == '-') {
                    if (arg[1] == '-') {
                        string name = arg.Substring(2);
                        if (name.ToLower() == "help") {
                            FullHelp<T>(extraHelp);
                            return null;
                        }
                        string value = "true";
                        if (name.Contains('=')) {
                            value = name.Substring(name.IndexOf('=') + 1);
                            name = name.Substring(0, name.IndexOf('='));
                        }
                        presence.Add(name);
                        if (dictionaryValues.ContainsKey(name)) {
                            string key = args[++i];
                            if (key.Contains('='))
                            {
                                value = key.Substring(key.IndexOf('=') + 1);
                                key = key.Substring(0, key.IndexOf('='));
                            }
                            dictionaryValues[name][key] = value;
                        } else if (arrayValues.ContainsKey(name)) {
                            arrayValues[name].Add(args[++i]);
                        } else {
                            values[name] = value;
                        }
                    } else {
                        char[] letters = arg.Substring(1).ToArray();
                        foreach (char letter in letters) {
                            if (letter == '=') {
                                break;
                            }
                            if (letter == '?' || letter == 'h') {
                                Help<T>(false);
                                return null;
                            }
                            presence.Add(letter.ToString());

                            string value = "true";
                            if (arg.Contains('=')) {
                                value = arg.Substring(arg.IndexOf('=') + 1);
                            }
                            string name = letter.ToString();
                            if (dictionaryValues.ContainsKey(name)) {
                                string key = args[++i];
                                if (key.Contains('=')) {
                                    value = key.Substring(key.IndexOf('=') + 1);
                                    key = key.Substring(0, key.IndexOf('='));
                                }
                                dictionaryValues[name][key] = value;
                            } else if (arrayValues.ContainsKey(name)) {
                                arrayValues[name].Add(args[++i]);
                            } else {
                                values[name] = value;
                            }
                        }
                    }
                } else {
                    positionals.Add(arg);
                }
            }
            
            foreach (FieldInfo field in fields) {
                if (field.Name == "Positionals") {
                    field.SetValue(instance, positionals.ToArray());
                    continue;
                }
                CLIFlagAttribute flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null) {
                    continue;
                }
                FieldKind kind = FieldKind.Default;
                Type t = field.FieldType;
                if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))) {
                    kind = FieldKind.Dictionary;
                }
                else if (t.IsGenericType && t.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) {
                    kind = FieldKind.Array;
                }
                object value = flagattr.Default;
                object key = string.Empty;
                bool insertedValue = false;
                if (flagattr.Positional > -1) {
                    if (positionals.Count > flagattr.Positional) {
                        value = positionals[flagattr.Positional];
                    }
                } else {
                    if (!presence.Contains(flagattr.Flag)) {
                        AliasAttribute[] aliasattrs = field.GetCustomAttributes<AliasAttribute>(true).ToArray();
                        if (aliasattrs.Length > 0) {
                            foreach (AliasAttribute aliasattr in aliasattrs) {
                                if (presence.Contains(aliasattr.Alias)) {
                                    insertedValue = true;
                                    if (kind == FieldKind.Default) {
                                        value = values[aliasattr.Alias];
                                    } else if (kind == FieldKind.Dictionary) {
                                        value = dictionaryValues[aliasattr.Alias];
                                    } else if (kind == FieldKind.Array) {
                                        value = arrayValues[aliasattr.Alias];
                                    }
                                }
                            }
                        }
                    } else {
                        insertedValue = true;
                        if (kind == FieldKind.Default) {
                            value = values[flagattr.Flag];
                        } else if (kind == FieldKind.Dictionary) {
                            value = dictionaryValues[flagattr.Flag];
                        } else if (kind == FieldKind.Array) {
                            value = arrayValues[flagattr.Flag];
                        }
                    }
                }
                if (value != null) {
                    if (flagattr.Valid != null) {
                        if (!flagattr.Valid.Contains(value.ToString())) {
                            Console.Error.WriteLine($"Value {value} is invalid for flag {flagattr.Flag}, valid values are {string.Join(", ", flagattr.Valid)}");
                            FullHelp<T>(extraHelp);
                            return null;
                        }
                    }
                    if (flagattr.Parser != null && insertedValue && flagattr.Parser.Length == 2) {
                        Type parserclass = Type.GetType(flagattr.Parser[0]);
                        MethodInfo method = parserclass?.GetMethod(flagattr.Parser[1]);
                        if (method != null) {
                            ParameterInfo[] @params = method.GetParameters();
                            if (kind == FieldKind.Default) {
                                if (@params.Length == 1 && @params[0].ParameterType.Name == "String" && method.ReturnType.Name == "Object") {
                                    value = method.Invoke(null, new object[] { (string)value });
                                }
                            } else if (kind == FieldKind.Dictionary) {
                                if (@params.Length == 1 && @params[0].ParameterType.Name == "Dictionary`2" && method.ReturnType.Name == "Object") {
                                    value = method.Invoke(null, new object[] { (Dictionary<string, string>)value });
                                }
                            } else if (kind == FieldKind.Array) {
                                if (@params.Length == 1 && @params[0].ParameterType.Name == "List`1" && method.ReturnType.Name == "Object") {
                                    value = method.Invoke(null, new object[] { (List<string>)value });
                                }
                            }
                        }
                    }
                    field.SetValue(instance, value);
                } else if (flagattr.Required) {
                    Console.Error.WriteLine(string.IsNullOrWhiteSpace(flagattr.Flag) ? $"Positional {flagattr.Positional} is required" : $"Flag {flagattr.Flag} is required");

                    FullHelp<T>(extraHelp);
                    return null;
                } else if(field.FieldType.IsClass && field.FieldType.GetConstructor(Type.EmptyTypes) != null) {
                    field.SetValue(instance, Activator.CreateInstance(field.FieldType));
                }
            }
            return instance;
        }
        
        public static string ArgFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.args");
        
        public static void LoadArgs() {
            if (File.Exists(ArgFilePath)) {
                AppArgs = JsonSerializer.Deserialize<string[]>(File.ReadAllText(ArgFilePath)).Concat(AppArgs).Distinct().ToArray();
            }
        }

        public static void SaveArgs(params string[] extra) {
            DeleteArgs();
            var args = AppArgs.Where(x => x.StartsWith("-")).Concat(extra.Where(x => !string.IsNullOrWhiteSpace(x))).Reverse().ToArray();
            File.WriteAllText(ArgFilePath, JsonSerializer.ToJsonString(args));
        }

        public static void ResetArgs() {
            AppArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        }

        public static void DeleteArgs() {
            if (File.Exists(ArgFilePath)) {
                File.Delete(ArgFilePath);
            }
        }
    }
}
