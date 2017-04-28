using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using OWLib.Writer;
using System.Linq;
using OWLib.Types;
using OWLib;

namespace ConvertTool {
    public class Program {
        private static List<IDataWriter> writers;

        public static void Main(string[] args) {

            writers = new List<IDataWriter>();

            Assembly asm = typeof(IDataWriter).Assembly;
            Type t = typeof(IDataWriter);
            List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
            foreach (Type tt in types) {
                if (tt.IsInterface) {
                    continue;
                }

                writers.Add((IDataWriter)Activator.CreateInstance(tt));
            }

            if (args.Length < 3) {
                Console.Out.WriteLine("Usage (model): ConvertTool.exe file type [model args] output_file");
                Console.Out.WriteLine("Usage (animation): ConvertTool.exe file type output_file");
                Console.Out.WriteLine("type can be:");
                Console.Out.WriteLine("  t - supprt - type  - {0, -30} - normal extension", "name");
                Console.Out.WriteLine("".PadLeft(60, '-'));
                foreach (IDataWriter w in writers) {
                    if (!w.SupportLevel.HasFlag(WriterSupport.MODEL)) {
                        continue;
                    }
                    Console.Out.WriteLine("  {0} - {1} - {2} - {3,-30} - {4}", w.Identifier[0], SupportLevel(w.SupportLevel), TypeLevel(w.SupportLevel), w.Name, w.Format);
                }
                foreach (IDataWriter w in writers) {
                    if (w.SupportLevel.HasFlag(WriterSupport.MODEL)) {
                        continue;
                    }
                    Console.Out.WriteLine("  {0} - {1} - {2} - {3,-30} - {4}", w.Identifier[0], SupportLevel(w.SupportLevel), TypeLevel(w.SupportLevel), w.Name, w.Format);
                }
                Console.Out.WriteLine("vutbpm = vertex / uv / attachment / bone / pose / material support");
                Console.Out.WriteLine("ampre = anim / model / map / refpose / material definition");
                Console.Out.WriteLine("model args:");
                Console.Out.WriteLine("  -l n - only save LOD, where N is lod");
                Console.Out.WriteLine("  -t   - save attachment points (sockets)");
                Console.Out.WriteLine("  -L   - only save first LOD found");
                Console.Out.WriteLine("  -c   - save collision models");
                return;
            }
            
            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());

            string dataFile = args[0];
            char type = args[1][0];
            string outputFile = args[args.Length - 1];

            IDataWriter writer = null;
            foreach (IDataWriter w in writers) {
                if (w.Identifier.Contains(type)) {
                    writer = w;
                    break;
                }
            }
            if (writer == null) {
                Console.Error.WriteLine("Unsupported format {0}", type);
                return;
            }

            Console.Out.WriteLine("Opening {0}", dataFile);
            using (Stream dataStream = File.Open(dataFile, FileMode.Open, FileAccess.Read)) {
                if (writer.SupportLevel.HasFlag(WriterSupport.ANIM)) {
                    Console.Out.WriteLine("Converting animation...");
                    Animation anim = new Animation(dataStream, false);
                    using (Stream output = File.Open(outputFile, FileMode.Create, FileAccess.Write)) {
                        if (writer.Write(anim, output, new object[] { })) {
                            Console.Out.WriteLine("Wrote animation");
                        } else {
                            Console.Out.WriteLine("Failed to write animation");
                        }
                    }
                } else if (writer.SupportLevel.HasFlag(WriterSupport.MODEL)) {
                    List<byte> lods = null;
                    bool attachments = false;
                    bool firstLod = false;
                    bool skipCmodel = true;
                    if (args.Length > 3) {
                        int i = 2;
                        while (i < args.Length - 2) {
                            string arg = args[i];
                            ++i;
                            if (arg[0] == '-') {
                                if (arg[1] == 'l') {
                                    if (lods == null) {
                                        lods = new List<byte>();
                                    }
                                    byte b = byte.Parse(args[i], System.Globalization.NumberStyles.Number);
                                    lods.Add(b);
                                    ++i;
                                } else if (arg[1] == 'L') {
                                    firstLod = true;
                                } else if (arg[1] == 't') {
                                    attachments = true;
                                } else if (arg[1] == 'c') {
                                    skipCmodel = false;
                                }
                            } else {
                                continue;
                            }
                        }
                    }
                    Chunked model = new Chunked(dataStream);
                    using (Stream output = File.Open(outputFile, FileMode.Create, FileAccess.Write)) {
                        Console.Out.WriteLine("Converting model...");
                        if (writer.Write(model, output, lods, new Dictionary<ulong, List<ImageLayer>>(), new object[] { attachments, null, null, firstLod, skipCmodel })) {
                            Console.Out.WriteLine("Wrote model");
                        } else {
                            Console.Out.WriteLine("Failed to write model");
                        }
                    }
                } else {
                    Console.Out.WriteLine("Cant export.");
                }
            }
        }

        private static string SupportLevel(WriterSupport supportLevel) {
            char[] r = new char[6] { '.', '.', '.', '.', '.', '.'};

            if (supportLevel.HasFlag(WriterSupport.VERTEX)) {
                r[0] = 'v';
            }
            if (supportLevel.HasFlag(WriterSupport.UV)) {
                r[1] = 'u';
            }
            if (supportLevel.HasFlag(WriterSupport.ATTACHMENT)) {
                r[2] = 't';
            }
            if (supportLevel.HasFlag(WriterSupport.BONE)) {
                r[3] = 'b';
            }
            if (supportLevel.HasFlag(WriterSupport.POSE)) {
                r[4] = 'p';
            }
            if (supportLevel.HasFlag(WriterSupport.MATERIAL)) {
                r[5] = 'm';
            }

            return new string(r);
        }

        private static string TypeLevel(WriterSupport supportLevel) {
            char[] r = new char[5] { '.', '.', '.', '.', '.' };

            if (supportLevel.HasFlag(WriterSupport.ANIM)) {
                r[0] = 'a';
            }
            if (supportLevel.HasFlag(WriterSupport.MODEL)) {
                r[1] = 'm';
            }
            if (supportLevel.HasFlag(WriterSupport.MAP)) {
                r[2] = 'p';
            }
            if (supportLevel.HasFlag(WriterSupport.REFPOSE)) {
                r[3] = 't';
            }
            if (supportLevel.HasFlag(WriterSupport.MATERIAL_DEF)) {
                r[4] = 'e';
            }

            return new string(r);
        }
    }
}
