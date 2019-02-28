using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Utf8Json;

namespace DataTool.Flag {
    public class FlagParser {
        public static string[] AppArgs { get; set; } = Environment.GetCommandLineArgs()
                                                                  .Skip(1)
                                                                  .ToArray();

        public static string ArgFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.args");

        public static T Parse<T>() where T : ICLIFlags => Parse<T>(null, AppArgs);

        public static void CheckCollisions(Type baseType, Action<string, string> OnCollision) {
            var type = typeof(ICLIFlags);
            var flagSets = Assembly.GetExecutingAssembly()
                                   .GetTypes()
                                   .Where(x => !x.IsInterface && type.IsAssignableFrom(x) && !baseType.IsAssignableFrom(x));

            var baseFlags = new Dictionary<string, Type>();
            {
                var fields = baseType.GetFields();
                foreach (var field in fields) {
                    var flagAttr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                    if (flagAttr == null || flagAttr.AllPositionals) continue;

                    if (flagAttr.Positional > -1 && string.IsNullOrWhiteSpace(flagAttr.Flag)) continue;

                    baseFlags.Add(flagAttr.Flag, field.DeclaringType);
                    var aliasAttrs = field.GetCustomAttributes<AliasAttribute>(true)
                                          .ToArray();
                    foreach (var aliasAttr in aliasAttrs) baseFlags.Add(aliasAttr.Alias, field.DeclaringType);
                }
            }

            foreach (var flagSet in flagSets) {
                var currentFlags = new HashSet<string>();
                var fields       = flagSet.GetFields();
                foreach (var field in fields) {
                    var flagAttr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                    if (flagAttr == null || flagAttr.AllPositionals) continue;

                    if (flagAttr.Positional > -1 && string.IsNullOrWhiteSpace(flagAttr.Flag)) continue;

                    if (baseFlags.ContainsKey(flagAttr.Flag) && field.DeclaringType != baseFlags[flagAttr.Flag] || !currentFlags.Add(flagAttr.Flag)) OnCollision(flagAttr.Flag, flagSet.FullName + "::" + field.Name);

                    var aliasAttrs = field.GetCustomAttributes<AliasAttribute>(true)
                                          .ToArray();
                    foreach (var aliasAttr in aliasAttrs)
                        if (baseFlags.ContainsKey(aliasAttr.Alias) && field.DeclaringType != baseFlags[aliasAttr.Alias] || !currentFlags.Add(aliasAttr.Alias))
                            OnCollision(aliasAttr.Alias, flagSet.FullName + "::" + field.Name);
                }
            }
        }

        public static void Help<T>(bool simple, Dictionary<string, string> values) where T : ICLIFlags {
            var iface = typeof(T);

            var positonals = new List<string>();
            var singulars  = new List<string>();
            var help       = new List<string>();
            var fields     = iface.GetFields();
            foreach (var field in fields) {
                var flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null || flagattr.AllPositionals) continue;

                if (flagattr.Positional > -1) {
                    var x                     = flagattr.Flag;
                    if (!flagattr.Required) x = $"[{x}]";

                    if (!values.TryGetValue(x, out var v)) v = x;
                    positonals.Add(v);
                }

                var localFlags = new List<string>();
                localFlags.Add(flagattr.Flag);
                localFlags.AddRange(field.GetCustomAttributes<AliasAttribute>()
                                         .Select(x => x.Alias));
                foreach (var localFlag in localFlags) {
                    if (string.IsNullOrWhiteSpace(localFlag)) continue;
                    if (localFlag.Length == 1) {
                        singulars.Add(localFlag);
                    } else {
                        var x                                    = localFlag;
                        if (!values.TryGetValue(x, out var v)) v = "value";
                        var t                                    = field.FieldType;
                        if (t.IsGenericType &&
                            t.GetGenericTypeDefinition()
                             .IsAssignableFrom(typeof(Dictionary<,>)))
                            x = $"[--{x} key=value]";
                        else if (t.IsGenericType &&
                                 t.GetGenericTypeDefinition()
                                  .IsAssignableFrom(typeof(List<>)))
                            x = $"[--{x} value]";
                        else if (flagattr.Default != null && flagattr.NeedsValue)
                            x = $"[--{x}[={flagattr.Default}]]";
                        else if (flagattr.NeedsValue)
                            x = $"[--{x}={v}]";
                        else
                            x = $"[--{x}]";

                        help.Add(x);
                    }
                }
            }

            var helpstr                       = AppDomain.CurrentDomain.FriendlyName;
            if (singulars.Count  > 0) helpstr += $" [-{string.Join("", singulars)}]";
            if (help.Count       > 0) helpstr += $" {string.Join(" ",  help)}";
            if (positonals.Count > 0) helpstr += $" {string.Join(" ",  positonals)}";
            Console.Out.WriteLine(helpstr);
        }

        public static void FullHelp<T>(Action<bool> extraHelp, bool skipOpener = false) where T : ICLIFlags {
            var iface = typeof(T);

            var positionals    = new SortedList<int, string>();
            var maxpositionals = "positional".Length;
            var positionalsstr = new SortedList<int, string>();

            var helpflags    = new List<string>();
            var maxflags     = "flags".Length;
            var helpstrs     = new List<string>();
            var maxstrs      = "help".Length;
            var helpdefaults = new List<string>();

            var fields = iface.GetFields();

            foreach (var field in fields) {
                var flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null || flagattr.AllPositionals) continue;
                var aliasattrs = field.GetCustomAttributes<AliasAttribute>(true)
                                      .ToArray();

                var required = "";
                if (flagattr.Positional == -1) {
                    if (flagattr.Required) required = " (Required)";

                    if (flagattr.NeedsValue) required += " (Needs Value)";
                }

                var aliases = new List<string>();

                var prefix                             = "--";
                if (flagattr.Flag?.Length == 1) prefix = "-";
                if (flagattr.Positional   > -1) prefix = "";

                aliases.Add(prefix + flagattr.Flag);
                foreach (var aliasattr in aliasattrs) {
                    prefix = "--";
                    if (aliasattr.Alias.Length == 1) prefix = "-";
                    if (flagattr.Positional    > -1) prefix = "";
                    aliases.Add(prefix + aliasattr.Alias);
                }

                var helpflag = string.Join(" ", aliases);
                maxflags = Math.Max(maxflags, helpflag.Length);
                var helpstr = $"{flagattr.Help}{required}";
                maxstrs = Math.Max(maxstrs, helpstr.Length);
                var helpdefault                           = "";
                if (flagattr.Default != null) helpdefault = flagattr.Default.ToString();

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

            if (!skipOpener) Help<T>(true, new Dictionary<string, string>());
            Console.Out.WriteLine();
            if (helpstrs.Count > 0) {
                if (!skipOpener) Console.Out.WriteLine("Flags:");
                Console.Out.WriteLine($"  {{0, -{maxflags}}} | {{1, -{maxstrs}}} | {{2}}", "flag", "help", "default");
                Console.Out.WriteLine("".PadLeft(maxflags + maxstrs + 20, '-'));
                for (var i = 0; i < helpstrs.Count; ++i) {
                    var helpflag    = helpflags[i];
                    var helpstr     = helpstrs[i];
                    var helpdefault = helpdefaults[i];
                    Console.Out.WriteLine($"  {{0, -{maxflags}}} | {{1, -{maxstrs}}} | {{2}}", helpflag, helpstr, helpdefault);
                }
            }

            var maxindex = Math.Max("index".Length, (int) Math.Floor(positionals.Count / 10d) + 1);
            if (positionals.Count > 0) {
                Console.Out.WriteLine();
                if (!skipOpener) Console.Out.WriteLine("Positionals:");
                Console.Out.WriteLine($"  {{0, -{maxindex}}} | --{{1, -{maxpositionals}}} | {{2}}", "index", "positional", "help");
                Console.Out.WriteLine("".PadLeft(maxindex + maxpositionals + 30, '-'));
                foreach (var pair in positionals) {
                    var positional    = pair.Value;
                    var positionalstr = positionalsstr[pair.Key];
                    Console.Out.WriteLine($"  {{0, -{maxindex}}} | --{{1, -{maxpositionals}}} | {{2}}", pair.Key, positional, positionalstr);
                }
            }

            extraHelp?.Invoke(false);
        }

        public static T Parse<T>(Action<bool> extraHelp) where T : ICLIFlags { return Parse<T>(extraHelp, AppArgs); }

        public static T Parse<T>(Action<bool> extraHelp, string[] args) where T : ICLIFlags {
            if (args.Length == 0) {
                FullHelp<T>(extraHelp);
                return null;
            }

            var iface = typeof(T);

            if (!(Activator.CreateInstance(iface) is T instance)) return null;

            var presence         = new HashSet<string>();
            var positionals      = new List<object>();
            var values           = new Dictionary<string, string>();
            var dictionaryValues = new Dictionary<string, Dictionary<string, string>>();
            var arrayValues      = new Dictionary<string, List<string>>();

            var fields = iface.GetFields();
            foreach (var field in fields) {
                var flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null || flagattr.AllPositionals) continue;
                var t = field.FieldType;
                if (t.IsGenericType &&
                    t.GetGenericTypeDefinition()
                     .IsAssignableFrom(typeof(Dictionary<,>))) {
                    var aliasattrs = field.GetCustomAttributes<AliasAttribute>(true)
                                          .ToArray();
                    if (aliasattrs.Length > 0)
                        foreach (var aliasattr in aliasattrs)
                            dictionaryValues.Add(aliasattr.Alias, new Dictionary<string, string>());
                    dictionaryValues.Add(flagattr.Flag, new Dictionary<string, string>());
                }

                if (t.IsGenericType &&
                    t.GetGenericTypeDefinition()
                     .IsAssignableFrom(typeof(List<>))) {
                    var aliasattrs = field.GetCustomAttributes<AliasAttribute>(true)
                                          .ToArray();
                    if (aliasattrs.Length > 0)
                        foreach (var aliasattr in aliasattrs)
                            arrayValues.Add(aliasattr.Alias, new List<string>());
                    arrayValues.Add(flagattr.Flag, new List<string>());
                }
            }

            for (var i = 0; i < args.Length; ++i) {
                var arg = args[i];
                if (arg[0] == '-') {
                    if (arg[1] == '-') {
                        var name  = arg.Substring(2);
                        var value = "true";
                        if (name.Contains('=')) {
                            value = name.Substring(name.IndexOf('=') + 1);
                            name  = name.Substring(0, name.IndexOf('='));
                        }

                        presence.Add(name);
                        if (dictionaryValues.ContainsKey(name)) {
                            var key = args[++i];
                            if (key.Contains('=')) {
                                value = key.Substring(key.IndexOf('=') + 1);
                                key   = key.Substring(0, key.IndexOf('='));
                            }

                            dictionaryValues[name][key] = value;
                        } else if (arrayValues.ContainsKey(name)) {
                            arrayValues[name]
                                .Add(args[++i]);
                        } else {
                            values[name] = value;
                        }
                    } else {
                        var letters = arg.Substring(1)
                                         .ToArray();
                        foreach (var letter in letters) {
                            if (letter == '=') break;
                            if (letter == '?' || letter == 'h') {
                                Help<T>(false, new Dictionary<string, string>());
                                extraHelp?.Invoke(true);
                                return null;
                            }

                            presence.Add(letter.ToString());

                            var value                    = "true";
                            if (arg.Contains('=')) value = arg.Substring(arg.IndexOf('=') + 1);
                            var name                     = letter.ToString();
                            if (dictionaryValues.ContainsKey(name)) {
                                var key = args[++i];
                                if (key.Contains('=')) {
                                    value = key.Substring(key.IndexOf('=') + 1);
                                    key   = key.Substring(0, key.IndexOf('='));
                                }

                                dictionaryValues[name][key] = value;
                            } else if (arrayValues.ContainsKey(name)) {
                                arrayValues[name]
                                    .Add(args[++i]);
                            } else {
                                values[name] = value;
                            }
                        }
                    }
                } else {
                    positionals.Add(arg);
                }
            }
            

            var flagAttributes = fields.Select(x => (field: x, attribute: x.GetCustomAttribute<CLIFlagAttribute>(true)))
                                       .Where(x => x.attribute != null)
                                       .OrderBy(x => x.attribute.Positional)
                                       .ToArray();

            var positionalsField = default(FieldInfo);
            
            
            var newPositionals = new List<object>(Enumerable.Repeat(default(object), Math.Max(positionals.Count, flagAttributes.Max(x => x.attribute.Positional) + 1)));

            var positionalTicker = 0;
            foreach (var (field, flagAttribute) in flagAttributes.Where(x => x.attribute.Positional > -1)) {
                if (!field.GetCustomAttributes<AliasAttribute>().Select(x => x.Alias).Concat(new[] {flagAttribute.Flag}).Any(x => presence.Contains(x))) {
                    newPositionals[flagAttribute.Positional] = positionals.ElementAtOrDefault(positionalTicker);
                    positionalTicker += 1;
                }
            }

            foreach (var positional in positionals.Skip(positionalTicker)) {
                newPositionals[positionalTicker] = positionals.ElementAtOrDefault(positionalTicker);
                positionalTicker += 1;
            }

            positionals = newPositionals;
            
            foreach (var (field, flagAttribute) in flagAttributes) {
                if (flagAttribute.AllPositionals) {
                    positionalsField = field;
                    continue;
                }

                var kind = FieldKind.Default;
                var t    = field.FieldType;
                if (t.IsGenericType &&
                    t.GetGenericTypeDefinition()
                     .IsAssignableFrom(typeof(Dictionary<,>)))
                    kind = FieldKind.Dictionary;
                else if (t.IsGenericType &&
                         t.GetGenericTypeDefinition()
                          .IsAssignableFrom(typeof(List<>))) kind = FieldKind.Array;
                var    value         = flagAttribute.Default;
                object key           = string.Empty;
                var    insertedValue = false;
                if (flagAttribute.Positional > -1 && positionals.Count > flagAttribute.Positional && positionals[flagAttribute.Positional] != null) value = positionals[flagAttribute.Positional] ?? value;

                if (!string.IsNullOrWhiteSpace(flagAttribute.Flag)) {
                    if (!presence.Contains(flagAttribute.Flag)) {
                        var aliasattrs = field.GetCustomAttributes<AliasAttribute>(true)
                                              .ToArray();
                        if (aliasattrs.Length > 0)
                            foreach (var aliasattr in aliasattrs)
                                if (presence.Contains(aliasattr.Alias)) {
                                    insertedValue = true;
                                    if (kind == FieldKind.Default)
                                        value = values[aliasattr.Alias];
                                    else if (kind == FieldKind.Dictionary)
                                        value                               = dictionaryValues[aliasattr.Alias];
                                    else if (kind == FieldKind.Array) value = arrayValues[aliasattr.Alias];
                                }
                    } else {
                        insertedValue = true;
                        if (kind == FieldKind.Default)
                            value = values[flagAttribute.Flag];
                        else if (kind == FieldKind.Dictionary)
                            value                               = dictionaryValues[flagAttribute.Flag];
                        else if (kind == FieldKind.Array) value = arrayValues[flagAttribute.Flag];
                    }

                    if (flagAttribute.Positional > 0) {
                        
                    }
                }

                if (value != null) {
                    if (flagAttribute.Valid != null)
                        if (!flagAttribute.Valid.Contains(value.ToString())) {
                            Console.Error.WriteLine($"Value {value} is invalid for flag {flagAttribute.Flag}, valid values are {string.Join(", ", flagAttribute.Valid)}");
                            FullHelp<T>(extraHelp);
                            return null;
                        }

                    if (flagAttribute.Parser != null && insertedValue && flagAttribute.Parser.Length == 2) {
                        var parserclass = Type.GetType(flagAttribute.Parser[0]);
                        var method      = parserclass?.GetMethod(flagAttribute.Parser[1]);
                        if (method != null) {
                            var @params = method.GetParameters();
                            if (kind == FieldKind.Default) {
                                if (@params.Length == 1 &&
                                    @params[0]
                                        .ParameterType.Name ==
                                    "String" &&
                                    method.ReturnType.Name == "Object") value = method.Invoke(null, new object[] { (string) value });
                            } else if (kind == FieldKind.Dictionary) {
                                if (@params.Length == 1 &&
                                    @params[0]
                                        .ParameterType.Name ==
                                    "Dictionary`2" &&
                                    method.ReturnType.Name == "Object") value = method.Invoke(null, new object[] { (Dictionary<string, string>) value });
                            } else if (kind == FieldKind.Array) {
                                if (@params.Length == 1 &&
                                    @params[0]
                                        .ParameterType.Name ==
                                    "List`1" &&
                                    method.ReturnType.Name == "Object") value = method.Invoke(null, new object[] { (List<string>) value });
                            }
                        }
                    }

                    field.SetValue(instance, value);
                } else if (flagAttribute.Required) {
                    Console.Error.WriteLine(string.IsNullOrWhiteSpace(flagAttribute.Flag) ? $"Positional {flagAttribute.Positional} is required" : $"Flag {flagAttribute.Flag} is required");

                    FullHelp<T>(extraHelp);
                    return null;
                } else if (field.FieldType.IsClass && field.FieldType.GetConstructor(Type.EmptyTypes) != null) {
                    field.SetValue(instance, Activator.CreateInstance(field.FieldType));
                }
            }

            if (positionalsField != default) {
                positionalsField.SetValue(instance, positionals.Select(x => x?.ToString()).ToArray());
            }

            return instance;
        }

        public static void LoadArgs() {
            if (File.Exists(ArgFilePath))
                AppArgs = JsonSerializer.Deserialize<string[]>(File.ReadAllText(ArgFilePath))
                                        .Concat(AppArgs)
                                        .Distinct()
                                        .ToArray();
        }

        public static void SaveArgs(params string[] extra) {
            DeleteArgs();
            var args = AppArgs.Where(x => x.StartsWith("-"))
                              .Concat(extra.Where(x => !string.IsNullOrWhiteSpace(x)))
                              .Reverse()
                              .ToArray();
            File.WriteAllText(ArgFilePath, JsonSerializer.ToJsonString(args));
        }

        public static void ResetArgs() {
            AppArgs = Environment.GetCommandLineArgs()
                                 .Skip(1)
                                 .ToArray();
        }

        public static void DeleteArgs() {
            if (File.Exists(ArgFilePath)) File.Delete(ArgFilePath);
        }

        private enum FieldKind {
            Default,
            Array,
            Dictionary
        }
    }
}
