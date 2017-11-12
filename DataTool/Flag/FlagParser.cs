using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataTool.Flag {
    public class FlagParser {
        public static T Parse<T>() where T : ICLIFlags {
            return Parse<T>(null, Environment.GetCommandLineArgs().Skip(1).ToArray());
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
                        if (flagattr.Default != null && flagattr.NeedsValue) {
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
            return Parse<T>(extraHelp, Environment.GetCommandLineArgs().Skip(1).ToArray());
        }

        public static T Parse<T>(Action extraHelp, string[] args) where T : ICLIFlags {
            if (args.Length == 0) {
                FullHelp<T>(extraHelp);
                return null;
            }

            Type iface = typeof(T);

            T instance = Activator.CreateInstance(iface) as T;
            if (instance == null) {
                return null;
            }

            HashSet<string> presence = new HashSet<string>();
            List<string> positionals = new List<string>();
            Dictionary<string, string> values = new Dictionary<string, string>();

            foreach (string arg in args) {
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
                        values[name] = value;
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
                            values[letter.ToString()] = value;
                        }
                    }
                } else {
                    positionals.Add(arg);
                }
            }

            FieldInfo[] fields = iface.GetFields();
            foreach (FieldInfo field in fields) {
                if (field.Name == "Positionals") {
                    field.SetValue(instance, positionals.ToArray());
                    continue;
                }
                CLIFlagAttribute flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                if (flagattr == null) {
                    continue;
                }
                object value = flagattr.Default;
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
                                    value = values[aliasattr.Alias];
                                }
                            }
                        }
                    } else {
                        value = values[flagattr.Flag];
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
                    if (flagattr.Parser != null && (value as string) != null && flagattr.Parser.Length == 2) {
                        Type parserclass = Type.GetType(flagattr.Parser[0]);
                        MethodInfo method = parserclass?.GetMethod(flagattr.Parser[1]);
                        if (method != null) {
                            ParameterInfo[] @params = method.GetParameters();
                            if (@params.Length == 1 && @params[0].ParameterType.Name == "String" && method.ReturnType.Name == "Object") {
                                value = method.Invoke(null, new object[] { (string)value });
                            }
                        }
                    }
                    field.SetValue(instance, value);
                } else if (flagattr.Required) {
                    Console.Error.WriteLine($"Flag {flagattr.Flag} is required");
                    FullHelp<T>(extraHelp);
                    return null;
                }
            }
            return instance;
        }
    }
}
