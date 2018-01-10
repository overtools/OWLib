using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Writer;
using OverTool.ExtractLogic;
using OWLib.Types.Map;
using OWLib.Types.STUD.Binding;
using System.Reflection;

namespace OverTool {
    class ExtractMap : IOvertool {
        public string Help => "output [maps]";
        public uint MinimumArgs => 1;
        public char Opt => 'M';
        public string FullOpt => "map";
        public string Title => "Extract Maps";
        public ushort[] Track => new ushort[1] { 0x9F };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];
            List<string> maps = flags.Positionals.Skip(3).ToList();

            bool skipCmodel = !flags.ExportCollision;

            for (int i = 0; i < maps.Count; ++i) {
                maps[i] = maps[i].ToUpperInvariant().TrimStart('0');
            }
            bool mapWildcard = maps.Count == 0;
            if (maps.Count > 0 && maps.Contains("*")) {
                mapWildcard = true;
            }

            char animEncoding = flags.AnimFormat;
            if (flags.Raw) {
                animEncoding = '+';
            }
            bool suppressAnimations = flags.SkipAnimations;
            if (animEncoding == '+' && !flags.RawAnimation) {
                suppressAnimations = true;
            }

            char modelEncoding = flags.ModelFormat;
            if (flags.Raw) {
                modelEncoding = '+';
            }
            bool suppressModels = flags.SkipModels;
            if (modelEncoding == '+' && !flags.RawModel) {
                suppressModels = true;
            }

            IDataWriter animWriter = null;
            if (animEncoding != 0 && animEncoding != '+') {
                Assembly asm = typeof(IDataWriter).Assembly;
                Type t = typeof(IDataWriter);
                List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    if (animWriter != null) {
                        break;
                    }
                    if (tt.IsInterface) {
                        continue;
                    }

                    IDataWriter tmp = (IDataWriter)Activator.CreateInstance(tt);
                    for (int i = 0; i < tmp.Identifier.Length; ++i) {
                        if (tmp.Identifier[i] == animEncoding) {
                            animWriter = tmp;
                            break;
                        }
                    }
                }
            }

            IDataWriter modelWriter = null;
            if (modelEncoding != 0 && modelEncoding != '+') {
                Assembly asm = typeof(IDataWriter).Assembly;
                Type t = typeof(IDataWriter);
                List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    if (modelWriter != null) {
                        break;
                    }
                    if (tt.IsInterface) {
                        continue;
                    }

                    IDataWriter tmp = (IDataWriter)Activator.CreateInstance(tt);
                    for (int i = 0; i < tmp.Identifier.Length; ++i) {
                        if (tmp.Identifier[i] == modelEncoding) {
                            modelWriter = tmp;
                            break;
                        }
                    }
                }
            }

            List<ulong> masters = track[0x9F];
            List<byte> LODs = new List<byte>(new byte[5] { 0, 1, 128, 254, 255 });
            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
            foreach (ulong masterKey in masters) {
                if (!map.ContainsKey(masterKey)) {
                    continue;
                }
                STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
                if (masterStud.Instances == null) {
                    continue;
                }
                MapMaster master = (MapMaster)masterStud.Instances[0];
                if (master == null) {
                    continue;
                }

                string name = Util.GetString(master.Header.name.key, map, handler);
                if (string.IsNullOrWhiteSpace(name)) {
                    name = $"Unknown{GUID.Index(master.Header.data.key):X}";
                }
                if (!mapWildcard && !(maps.Contains(name.ToUpperInvariant()) || maps.Contains($"{GUID.Index(masterKey):X}"))) {
                    continue;
                }
                string outputPath = string.Format("{0}{1}{2}{1}{3:X}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(name), GUID.Index(master.Header.data.key));

                if (!map.ContainsKey(master.Header.data.key)) {
                    continue;
                }

                HashSet<ulong> parsed = new HashSet<ulong>();
                Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
                Dictionary<ulong, List<ulong>> soundData = new Dictionary<ulong, List<ulong>>();
                if (!map.ContainsKey(master.DataKey(1))) {
                    continue;
                }
                if (!map.ContainsKey(master.DataKey(2))) {
                    continue;
                }
                if (!map.ContainsKey(master.DataKey(8))) {
                    continue;
                }
                if (!map.ContainsKey(master.DataKey(0xB))) {
                    continue;
                }
                if (!map.ContainsKey(master.DataKey(0x11))) {
                    continue;
                }
                if (!map.ContainsKey(master.DataKey(0x10))) {
                    continue;
                }
                using (Stream mapStream = Util.OpenFile(map[master.DataKey(1)], handler)) {
                    Console.Out.WriteLine("Extracting map {0} with ID {1:X8}", name, GUID.Index(master.Header.data.key));
                    Map mapData = new Map(mapStream);
                    IDataWriter owmap = new OWMAPWriter();
                    Dictionary<ulong, List<string>>[] used = null;
                    if (!Directory.Exists(outputPath)) {
                        Directory.CreateDirectory(outputPath);
                    }
                    HashSet<ulong> soundDone = new HashSet<ulong>();
                    Sound.FindSoundsEx(master.Header.audio.key, soundDone, soundData, map, handler, replace, master.Header.data.key);
                    using (Stream map2Stream = Util.OpenFile(map[master.DataKey(2)], handler)) {
                        if (map2Stream != null) {
                            Map map2Data = new Map(map2Stream);
                            using (Stream map8Stream = Util.OpenFile(map[master.DataKey(8)], handler)) {
                                if (map8Stream != null) {
                                    Map map8Data = new Map(map8Stream);
                                    using (Stream mapBStream = Util.OpenFile(map[master.DataKey(0xB)], handler)) {
                                        if (mapBStream != null) {
                                            Map mapBData = new Map(mapBStream, true);
                                            using (Stream map11Stream = Util.OpenFile(map[master.DataKey(0x11)], handler)) {
                                                if (map11Stream != null) {
                                                    Map11 map11 = new Map11(map11Stream);
                                                    Sound.FindSoundsSTUD(map11.main, soundDone, soundData, map, handler, replace, masterKey, master.DataKey(0x11));
                                                    Sound.FindSoundsSTUD(map11.secondary, soundDone, soundData, map, handler, replace, masterKey, master.DataKey(0x11));
                                                }
                                            }

                                            mapBStream.Position = (long)(Math.Ceiling((float)mapBStream.Position / 16.0f) * 16); // Future proofing

                                            for (int i = 0; i < mapBData.STUDs.Count; ++i) {
                                                STUD stud = mapBData.STUDs[i];
                                                Sound.FindSoundsSTUD(stud, soundDone, soundData, map, handler, replace, master.DataKey(0xB), master.DataKey(0xB));
                                            }

                                            for (int i = 0; i < mapBData.Records.Length; ++i) {
                                                if (mapBData.Records[i] != null && mapBData.Records[i].GetType() != typeof(Map0B)) {
                                                    continue;
                                                }
                                                Map0B mapprop = (Map0B)mapBData.Records[i];
                                                if (!map.ContainsKey(mapprop.Header.binding)) {
                                                    continue;
                                                }
                                                Sound.FindSoundsEx(mapprop.Header.binding, soundDone, soundData, map, handler, replace, master.DataKey(0xB));
                                                HashSet<ulong> bindingModels = new HashSet<ulong>();
                                                Dictionary<ulong, List<ImageLayer>> bindingTextures = new Dictionary<ulong, List<ImageLayer>>();

                                                using (Stream bindingFile = Util.OpenFile(map[mapprop.Header.binding], handler)) {
                                                    STUD binding = new STUD(bindingFile, true, STUDManager.Instance, false, true);
                                                    foreach (ISTUDInstance instance in binding.Instances) {
                                                        if (instance == null) {
                                                            continue;
                                                        }
                                                        if (instance.Name != binding.Manager.GetName(typeof(ComplexModelRecord))) {
                                                            continue;
                                                        }
                                                        ComplexModelRecord cmr = (ComplexModelRecord)instance;
                                                        mapprop.ModelLook = cmr.Data.material.key;
                                                        mapprop.Model = cmr.Data.model.key;
                                                        Skin.FindAnimations(cmr.Data.animationList.key, soundData, animList, replace, parsed, map, handler, bindingModels, bindingTextures, mapprop.Model);
                                                        Skin.FindAnimations(cmr.Data.secondaryAnimationList.key, soundData, animList, replace, parsed, map, handler, bindingModels, bindingTextures, mapprop.Model);
                                                        break;
                                                    }
                                                }
                                                mapBData.Records[i] = mapprop;
                                            }

                                            using (Stream mapLStream = Util.OpenFile(map[master.DataKey(9)], handler)) {
                                                Map mapLData = new Map(mapLStream);
                                                using (Stream outputStream = File.Open($"{outputPath}{Util.SanitizePath(name)}{owmap.Format}", FileMode.Create, FileAccess.Write)) {
                                                    used = owmap.Write(outputStream, mapData, map2Data, map8Data, mapBData, mapLData, name, modelWriter);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    IDataWriter owmat = new OWMATWriter();
                    using (Stream map10Stream = Util.OpenFile(map[master.DataKey(0x10)], handler)) {
                        Map10 physics = new Map10(map10Stream);
                        using (Stream outputStream = File.Open($"{outputPath}physics{modelWriter.Format}", FileMode.Create, FileAccess.Write)) {
                            modelWriter.Write(physics, outputStream, new object[0]);
                        }
                    }
                    if (used != null) {
                        Dictionary<ulong, List<string>> models = used[0];
                        Dictionary<ulong, List<string>> materials = used[1];
                        Dictionary<ulong, Dictionary<ulong, List<ImageLayer>>> cache = new Dictionary<ulong, Dictionary<ulong, List<ImageLayer>>>();

                        if (!suppressModels) {
                            foreach (KeyValuePair<ulong, List<string>> modelpair in models) {
                                if (!map.ContainsKey(modelpair.Key)) {
                                    continue;
                                }
                                if (!parsed.Add(modelpair.Key)) {
                                    continue;
                                }
                                HashSet<string> extracted = new HashSet<string>();
                                using (Stream modelStream = Util.OpenFile(map[modelpair.Key], handler)) {
                                    Chunked mdl = new Chunked(modelStream, true);
                                    modelStream.Position = 0;
                                    if (modelEncoding != '+' && modelWriter != null) {
                                        foreach (string modelOutput in modelpair.Value) {
                                            if (!extracted.Add(modelOutput)) {
                                                continue;
                                            }
                                            using (Stream outputStream = File.Open($"{outputPath}{modelOutput}", FileMode.Create, FileAccess.Write)) {
                                                if (modelWriter.Write(mdl, outputStream, LODs, new Dictionary<ulong, List<ImageLayer>>(), new object[5] { null, null, null, null, skipCmodel })) {
                                                    if (!quiet) {
                                                        Console.Out.WriteLine("Wrote model {0}", modelOutput);
                                                    }
                                                } else {
                                                    if (!quiet) {
                                                        Console.Out.WriteLine("Failed to write model");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (flags.RawModel) {
                                        using (Stream outputStream = File.Open($"{outputPath}{GUID.LongKey(modelpair.Key):X12}.{GUID.Type(modelpair.Key):X3}", FileMode.Create, FileAccess.Write)) {
                                            if (modelWriter.Write(mdl, outputStream, LODs, new Dictionary<ulong, List<ImageLayer>>(), new object[5] { null, null, null, null, skipCmodel })) {
                                                if (!quiet) {
                                                    Console.Out.WriteLine("Wrote raw model {0:X12}.{1:X3}", GUID.LongKey(modelpair.Key), GUID.Type(modelpair.Key));
                                                }
                                            } else {
                                                if (!quiet) {
                                                    Console.Out.WriteLine("Failed to write model");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!suppressAnimations) {
                            foreach (KeyValuePair<ulong, ulong> kv in animList) {
                                ulong parent = kv.Value;
                                ulong key = kv.Key;
                                Stream animStream = Util.OpenFile(map[key], handler);
                                if (animStream == null) {
                                    continue;
                                }

                                Animation anim = new Animation(animStream);
                                animStream.Position = 0;

                                string outpath = string.Format("{0}Animations{1}{2:X12}{1}{5}{1}{3:X12}.{4:X3}", outputPath, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), GUID.Type(key), anim.Header.priority);
                                if (!Directory.Exists(Path.GetDirectoryName(outpath))) {
                                    Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                                }
                                if (flags.RawAnimation) {
                                    using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                                        animStream.CopyTo(outp);
                                        if (!quiet) {
                                            Console.Out.WriteLine("Wrote raw animation {0}", outpath);
                                        }
                                    }
                                }
                                if (animEncoding != '+' && animWriter != null) {
                                    outpath = string.Format("{0}Animations{1}{2:X12}{1}{5}{1}{3:X12}.{4}", outputPath, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), animWriter.Format, anim.Header.priority);
                                    using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                                        animWriter.Write(anim, outp);
                                        if (!quiet) {
                                            Console.Out.WriteLine("Wrote animation {0}", outpath);
                                        }
                                    }
                                }
                            }
                        }

                        if (!flags.SkipSound) {
                            Console.Out.WriteLine("Dumping sounds...");
                            string soundPath = $"{outputPath}Sounds{Path.DirectorySeparatorChar}";
                            if (!Directory.Exists(soundPath)) {
                                Directory.CreateDirectory(soundPath);
                            }

                            DumpVoice.Save(soundPath, soundData, map, handler, quiet);
                        }

                        if (!flags.SkipTextures) {
                            foreach (KeyValuePair<ulong, List<string>> matpair in materials) {
                                Dictionary<ulong, List<ImageLayer>> tmp = new Dictionary<ulong, List<ImageLayer>>();
                                if (cache.ContainsKey(matpair.Key)) {
                                    tmp = cache[matpair.Key];
                                } else {
                                    Skin.FindTextures(matpair.Key, tmp, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), map, handler);
                                    cache.Add(matpair.Key, tmp);
                                }
                                Dictionary<string, TextureType> types = new Dictionary<string, TextureType>();
                                foreach (KeyValuePair<ulong, List<ImageLayer>> kv in tmp) {
                                    ulong materialId = kv.Key;
                                    List<ImageLayer> sublayers = kv.Value;
                                    HashSet<ulong> materialParsed = new HashSet<ulong>();
                                    foreach (ImageLayer layer in sublayers) {
                                        if (!materialParsed.Add(layer.Key)) {
                                            continue;
                                        }
                                        KeyValuePair<string, TextureType> pair = Skin.SaveTexture(layer.Key, materialId, map, handler, outputPath, quiet, $"Textures/{GUID.Index(matpair.Key):X8}");
                                        if (pair.Key == null) {
                                            continue;
                                        }
                                        types.Add(pair.Key, pair.Value);
                                    }
                                }

                                foreach (string matOutput in matpair.Value) {
                                    if (File.Exists($"{outputPath}{matOutput}")) {
                                        continue;
                                    }
                                    using (Stream outputStream = File.Open($"{outputPath}{matOutput}", FileMode.Create, FileAccess.Write)) {
                                        if (owmat.Write(null, outputStream, null, tmp, new object[1] { types })) {
                                            if (!quiet) {
                                                Console.Out.WriteLine("Wrote material {0}", matOutput);
                                            }
                                        } else {
                                            if (!quiet) {
                                                Console.Out.WriteLine("Failed to write material");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
