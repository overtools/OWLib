using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.Binding;
using OWLib.Types.STUD.GameParam;
using OWLib.Types.STUD.InventoryItem;
using OWLib.Writer;
using System.Reflection;
using System.Linq;
using OWLib.Types.Chunk;

namespace OverTool.ExtractLogic {
    class Skin {
        public static void FindTextures(ulong key, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
            ulong tgt = key;
            if (replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
            }
            if (!map.ContainsKey(tgt)) {
                return;
            }
            if (!parsed.Add(tgt)) {
                return;
            }

#if OUTPUT_MATERIALMATER
            Stream matMast = Util.OpenFile(map[tgt], handler);
            string outFilename = string.Format("./STUD/MaterialMaster/{0:X16}.mat", map[tgt].record.ContentKey.ToHexString());
            string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
            Directory.CreateDirectory(putPathname);
            Stream OutWriter = File.Create(outFilename);
            matMast.CopyTo(OutWriter);
            OutWriter.Close();
            matMast.Close();
#endif

            STUD record = new STUD(Util.OpenFile(map[tgt], handler));
            if (record.Instances.Length == 0) {
                return;
            }
            if (record.Instances[0] == null) {
                return;
            }
            MaterialMaster master = (MaterialMaster)record.Instances[0];
            if (master == null) {
                return;
            }
            foreach (MaterialMaster.MaterialMasterMaterial material in master.Materials) {
                ulong materialId = material.id;
                ulong materialKey = material.record.key;
                if (replace.ContainsKey(materialKey)) {
                    materialKey = replace[materialKey];
                }
                if (!map.ContainsKey(materialKey)) {
                    continue;
                }
                Material mat = new Material(Util.OpenFile(map[materialKey], handler), materialId);
                ulong definitionKey = mat.Header.ImageDefinition;
                if (replace.ContainsKey(definitionKey)) {
                    definitionKey = replace[definitionKey];
                }
                if (!map.ContainsKey(definitionKey)) {
                    continue;
                }
                ImageDefinition def = new ImageDefinition(Util.OpenFile(map[definitionKey], handler));
                if (!layers.ContainsKey(materialId)) {
                    layers.Add(materialId, new List<ImageLayer>());
                }
                for (int i = 0; i < def.Layers.Length; ++i) {
                    ImageLayer layer = def.Layers[i];
                    if (replace.ContainsKey(layer.Key)) {
                        layer.Key = replace[layer.Key];
                    }
                    layers[materialId].Add(layer);
                }
            }
        }

        public static void FindTexturesAnonymous8(ulong key, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
            ulong tgt = key;
            if (replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
            }
            if (!map.ContainsKey(tgt)) {
                return;
            }
            if (!parsed.Add(tgt)) {
                return;
            }

            ulong materialId = ulong.MaxValue;
            Material mat = new Material(Util.OpenFile(map[tgt], handler), materialId);
            ulong definitionKey = mat.Header.ImageDefinition;
            if (replace.ContainsKey(definitionKey)) {
                definitionKey = replace[definitionKey];
            }
            if (!map.ContainsKey(definitionKey)) {
                return;
            }
            ImageDefinition def = new ImageDefinition(Util.OpenFile(map[definitionKey], handler));
            if (!layers.ContainsKey(materialId)) {
                layers.Add(materialId, new List<ImageLayer>());
            }
            for (int i = 0; i < def.Layers.Length; ++i) {
                ImageLayer layer = def.Layers[i];
                if (replace.ContainsKey(layer.Key)) {
                    layer.Key = replace[layer.Key];
                }
                layers[materialId].Add(layer);
            }
        }

        public static void FindTexturesAnonymousB3(ulong key, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
            ulong tgt = key;
            if (replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
            }
            if (!map.ContainsKey(tgt)) {
                return;
            }
            if (!parsed.Add(tgt)) {
                return;
            }

            ulong materialId = ulong.MaxValue;
            ImageDefinition def = new ImageDefinition(Util.OpenFile(map[tgt], handler));
            if (def.Layers.Count() == 0) {
                return;
            }
            if (!layers.ContainsKey(materialId)) {
                layers.Add(materialId, new List<ImageLayer>());
            }
            for (int i = 0; i < def.Layers.Length; ++i) {
                ImageLayer layer = def.Layers[i];
                if (replace.ContainsKey(layer.Key)) {
                    layer.Key = replace[layer.Key];
                }
                layers[materialId].Add(layer);
            }
        }

        public static void FindAnimationsSoft(ulong key, Dictionary<ulong, List<ulong>> sound, Dictionary<ulong, ulong> animList, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, ulong parent = 0) {
            if (!map.ContainsKey(key)) {
                return;
            }
            if (GUID.Type(key) != 0x006) {
                return;
            }
            if (!parsed.Add(key)) {
                return;
            }

            using (Stream anim = Util.OpenFile(map[key], handler)) {
                if (anim == null) {
                    return;
                }
                using (BinaryReader reader = new BinaryReader(anim)) {
                    anim.Position = 0x18L;
                    ulong infokey = reader.ReadUInt64();
                    Sound.FindSoundsExD(infokey, new HashSet<ulong>(), sound, map, handler, replace, key);
                    FindDataChunked(infokey, sound, animList, replace, parsed, map, handler, models, layers, key);
                }
            }
        }      

        public static void FindDataChunked(ulong key, Dictionary<ulong, List<ulong>> sound, Dictionary<ulong, ulong> animList, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, ulong parent = 0) {
            if (replace.ContainsKey(key)) {
                key = replace[key];
            }
            if (!map.ContainsKey(key)) {
                return;
            }
            if (!parsed.Add(key)) {
                return;
            }

            using (Stream file = Util.OpenFile(map[key], handler)) {
                file.Position = 0;
                Chunked chunked = new Chunked(file);
                DMCE[] dmces = chunked.GetAllOfTypeFlat<DMCE>();
                foreach (DMCE dmce in dmces) {
                    if (models != null && dmce.Data.Model != 0) {
                        if (replace.ContainsKey(dmce.Data.Model)) {
                            models.Add(replace[dmce.Data.Model]);
                        } else {
                            models.Add(dmce.Data.Model);
                        }
                    }
                    if (layers != null && dmce.Data.Look != 0) {
                        FindTextures(dmce.Data.Look, layers, replace, parsed, map, handler);
                    }
                    if (animList != null && !animList.ContainsKey(dmce.Data.Animation) && dmce.Data.Animation != 0) {
                        if (replace.ContainsKey(dmce.Data.Animation)) {
                            animList[replace[dmce.Data.Animation]] = parent;
                            FindAnimationsSoft(replace[dmce.Data.Animation], sound, animList, replace, parsed, map, handler, models, layers, replace[dmce.Data.Animation]);
                        } else {
                            animList[dmce.Data.Animation] = parent;
                            FindAnimationsSoft(dmce.Data.Animation, sound, animList, replace, parsed, map, handler, models, layers, dmce.Data.Animation);
                        }
                    }
                }
                NECE[] neces = chunked.GetAllOfTypeFlat<NECE>();
                foreach (NECE nece in neces) {
                    if (nece.Data.Entity > 0) {
                        FindModels(nece.Data.Entity, new List<ulong>(), models, animList, layers, replace, parsed, map, handler, sound);
                    }
                }
                CECE[] ceces = chunked.GetAllOfTypeFlat<CECE>();
                foreach (CECE cece in ceces) {
                    if (animList != null && !animList.ContainsKey(cece.Data.Animation) && cece.Data.Animation != 0) {
                        animList[cece.Data.Animation] = parent;
                        FindAnimationsSoft(cece.Data.Animation, sound, animList, replace, parsed, map, handler, models, layers, cece.Data.Animation);
                    }
                }
                SSCE[] ssces = chunked.GetAllOfTypeFlat<SSCE>();
                foreach (SSCE ssce in ssces) {
                    if (layers != null) {
                        FindTexturesAnonymous8(ssce.Data.Material, layers, replace, parsed, map, handler);
                        FindTexturesAnonymousB3(ssce.Data.TextureDefinition, layers, replace, parsed, map, handler);
                    }
                }
                RPCE[] prces = chunked.GetAllOfTypeFlat<RPCE>();
                foreach (RPCE prce in prces) {
                    if (models != null) {
                        if (replace.ContainsKey(prce.Data.Model)) {
                            models.Add(replace[prce.Data.Model]);
                        } else {
                            models.Add(prce.Data.Model);
                        }
                    }
                }
                Sound.FindSoundsChunked(chunked, new HashSet<ulong>(), sound, map, handler, replace, parent, parent);
            }
        }

        public static void FindAnimations(ulong key, Dictionary<ulong, List<ulong>> sound, Dictionary<ulong, ulong> animList, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, ulong parent = 0) {
            if (key == 0) {
                return;
            }
            ulong tgt = key;
            string tgtName = $"{GUID.LongKey(tgt):X12}.{GUID.Type(tgt):X3}";
            if (replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
            }

            if (!map.ContainsKey(tgt)) {
                return;
            }

            if (!parsed.Add(tgt)) {
                return;
            }

            STUD record = new STUD(Util.OpenFile(map[tgt], handler), true, STUDManager.Instance, false, true);
            if (record.Instances == null) {
                return;
            }
            foreach (ISTUDInstance inst in record.Instances) {
                if (inst == null) {
                    continue;
                }
                if (inst.Name == record.Manager.GetName(typeof(VictoryPoseItem))) {
                    VictoryPoseItem item = (VictoryPoseItem)inst;
                    FindAnimations(item.Data.f0BF.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                } else if (inst.Name == record.Manager.GetName(typeof(EmoteItem))) {
                    EmoteItem item = (EmoteItem)inst;
                    FindAnimations(item.Data.animation.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                } else if (inst.Name == record.Manager.GetName(typeof(HeroicIntroItem))) {
                    HeroicIntroItem item = (HeroicIntroItem)inst;
                    if (item.Data.f006.key > 0) {
                        animList[item.Data.f006.key] = parent;
                        FindAnimationsSoft(item.Data.f006.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                    }
                } else if (inst.Name == record.Manager.GetName(typeof(AnimationList))) {
                    AnimationList r = (AnimationList)inst;
                    foreach (AnimationList.AnimationListEntry entry in r.Entries) {
                        ulong bindingKey = entry.animation.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        if (!map.ContainsKey(bindingKey)) {
                            continue;
                        }
                        if (animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
                            continue;
                        }
                        animList[bindingKey] = parent;
                        FindAnimationsSoft(bindingKey, sound, animList, replace, parsed, map, handler, models, layers, bindingKey);
                    }
                } else if (inst.Name == record.Manager.GetName(typeof(Pose))) {
                    Pose r = (Pose)inst;
                    foreach (OWRecord animation in new OWRecord[3] { r.Header.animation1, r.Header.animation2, r.Header.animation3 }) {
                        ulong bindingKey = animation.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        if (!map.ContainsKey(bindingKey)) {
                            continue;
                        }
                        if (animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
                            continue;
                        }
                        animList[bindingKey] = parent;
                        FindAnimationsSoft(bindingKey, sound, animList, replace, parsed, map, handler, models, layers, bindingKey);
                    }
                } else if (inst.Name == record.Manager.GetName(typeof(AnimationListInfo))) {
                    AnimationListInfo r = (AnimationListInfo)inst;
                    foreach (AnimationListInfo.AnimationListEntry entry in r.Entries) {
                        FindAnimations(entry.secondary.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                    }
                } else if (inst.Name == record.Manager.GetName(typeof(AnimationListReference))) {
                    AnimationListReference r = (AnimationListReference)inst;
                    foreach (OWRecord animation in new OWRecord[5] { r.Header.unkD, r.Header.animation, r.Header.unk12, r.Header.unk15, r.Header.unk18 }) {
                        ulong bindingKey = animation.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        if (!map.ContainsKey(bindingKey)) {
                            continue;
                        }
                        if (animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
                            continue;
                        }
                        ulong keyid = GUID.Type(bindingKey);
                        if (keyid == 0x6) {
                            animList[bindingKey] = parent;
                            FindAnimationsSoft(bindingKey, sound, animList, replace, parsed, map, handler, models, layers, bindingKey);
                        } else if (keyid == 0x20 || keyid == 0x21) {
                            FindAnimations(bindingKey, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                        }
                    }
                } if (inst.Name == record.Manager.GetName(typeof(EffectReference))) {
                    EffectReference reference = (EffectReference)inst;
                    FindDataChunked(reference.Reference.key.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                }
            }
        }

        public static void FindModels(ulong key, List<ulong> ignore, HashSet<ulong> models, Dictionary<ulong, ulong> animList, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, List<ulong>> sound) {
            if (key == 0) {
                return;
            }
            ulong tgt = key;
            string tgtName = $"{GUID.LongKey(tgt):X12}.{GUID.Type(tgt):X3}";
            if (replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
            }

            if (!map.ContainsKey(tgt)) {
                return;
            }
            if (!parsed.Add(tgt)) {
                return;
            }

            switch (GUID.Type(tgt)) {
                case 0xC: // model
                    models.Add(tgt);
                    return;
                case 0x1A: // texture
                    FindTextures(tgt, layers, replace, parsed, map, handler);
                    return;
                case 0x20: // animation
                case 0x21:
                    FindAnimations(tgt, sound, animList, replace, parsed, map, handler, models, layers, 0);
                    return;
                case 0x7C: // string
                    return;
            }

            STUD record = new STUD(Util.OpenFile(map[tgt], handler), true, STUDManager.Instance, false, true);
            if (record.Instances == null) {
                return;
            }
            foreach (ISTUDInstance inst in record.Instances) {
                if (inst == null) {
                    continue;
                }
                if (inst.Name == record.Manager.GetName(typeof(ViewModelRecord))) {
                    ViewModelRecord r = (ViewModelRecord)inst;
                    ulong bindingKey = r.Data.binding.key;
                    if (replace.ContainsKey(bindingKey)) {
                        bindingKey = replace[bindingKey];
                    }
                    FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                }
                if (inst.Name == record.Manager.GetName(typeof(ComplexModelRecord))) {
                    ComplexModelRecord r = (ComplexModelRecord)inst;
                    ulong modelKey = r.Data.model.key;
                    if (replace.ContainsKey(modelKey)) {
                        modelKey = replace[modelKey];
                    }
                    if (ignore.Count > 0 && !ignore.Contains(GUID.LongKey(modelKey))) {
                        continue;
                    }
                    models.Add(modelKey);
                    FindAnimations(r.Data.animationList.key, sound, animList, replace, parsed, map, handler, models, layers, modelKey);
                    FindAnimations(r.Data.secondaryAnimationList.key, sound, animList, replace, parsed, map, handler, models, layers, modelKey);
                    ulong target = r.Data.material.key;
                    if (replace.ContainsKey(target)) {
                        target = replace[target];
                    }
                    FindTextures(target, layers, replace, parsed, map, handler);
                }
                if (inst.Name == record.Manager.GetName(typeof(ParameterRecord))) {
                    ParameterRecord r = (ParameterRecord)inst;
                    foreach (ParameterRecord.ParameterEntry entry in r.Parameters) {
                        ulong bindingKey = entry.parameter.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                    }
                }
                if (inst.Name == record.Manager.GetName(typeof(BindingRecord))) {
                    BindingRecord r = (BindingRecord)inst;
                    ulong bindingKey = r.Param.binding.key;
                    if (replace.ContainsKey(bindingKey)) {
                        bindingKey = replace[bindingKey];
                    }
                    FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                    bindingKey = r.Param.binding2.key;
                    if (replace.ContainsKey(bindingKey)) {
                        bindingKey = replace[bindingKey];
                    }
                    FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                }
                if (inst.Name == record.Manager.GetName(typeof(ChildGameParameterRecord))) {
                    ChildGameParameterRecord r = (ChildGameParameterRecord)inst;
                    ulong bindingKey = r.Param.binding.key;
                    if (replace.ContainsKey(bindingKey)) {
                        bindingKey = replace[bindingKey];
                    }
                    FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                    bindingKey = r.Param.binding2.key;
                    if (replace.ContainsKey(bindingKey)) {
                        bindingKey = replace[bindingKey];
                    }
                    FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                }
                if (inst.Name == record.Manager.GetName(typeof(SubModelRecord))) {
                    SubModelRecord r = (SubModelRecord)inst;
                    foreach (SubModelRecord.SubModelEntry entry in r.Entries) {
                        ulong bindingKey = entry.binding;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                    }
                }
                if (inst.Name == record.Manager.GetName(typeof(ChildParameterRecord))) {
                    ChildParameterRecord r = (ChildParameterRecord)inst;
                    foreach (ChildParameterRecord.Child br in r.Children) {
                        ulong bindingKey = br.parameter.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
                    }
                }
                if (inst.Name == record.Manager.GetName(typeof(AnimationCoreference))) {
                    AnimationCoreference r = (AnimationCoreference)inst;
                    foreach (AnimationCoreference.AnimationCoreferenceEntry entry in r.Entries) {
                        ulong bindingKey = entry.animation.key;
                        if (replace.ContainsKey(bindingKey)) {
                            bindingKey = replace[bindingKey];
                        }
                        if (!map.ContainsKey(bindingKey)) {
                            continue;
                        }
                        if (animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
                            continue;
                        }
                        animList[bindingKey] = 0;
                        FindAnimationsSoft(bindingKey, sound, animList, replace, parsed, map, handler, models, layers, bindingKey);
                    }
                }
                if (inst.Name == record.Manager.GetName(typeof(PoseList))) {
                    PoseList r = (PoseList)inst;
                    if (r.Header.reference.key != 0) {
                        FindAnimations(r.Header.reference.key, sound, animList, replace, parsed, map, handler, models, layers, 0);
                    }
                }
                if (inst.Name == record.Manager.GetName(typeof(EffectReference))) {
                    EffectReference reference = (EffectReference)inst;
                    FindDataChunked(reference.Reference.key.key, sound, animList, replace, parsed, map, handler, models, layers, tgt);
                }
            }
        }

        public static ulong FindReplacements(ulong key, int index, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HeroMaster master, SkinItem skin, bool skipFirst = false) {
            if (!map.ContainsKey(key)) {
                return 0;
            }
            if (!parsed.Add(key)) {
                return 0;
            }

            STUD record = new STUD(Util.OpenFile(map[key], handler));
            if (record.Instances[0] == null) {
                return 0;
            }
            if (record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverride))) {
                TextureOverride over = (TextureOverride)record.Instances[0];
                if (index > -1 && over.SubDefinitions.Length > index) {
                    FindReplacements(over.SubDefinitions[index].key, -1, replace, parsed, map, handler, master, skin);
                }
                if (skipFirst) {
                    return over.Header.icon.key;
                }
                for (int i = 0; i < over.Replace.Length; ++i) {
                    if (!map.ContainsKey(over.Target[i])) {
                        continue;
                    }
                    if (replace.ContainsKey(over.Replace[i])) {
                        continue;
                    }
                    replace[over.Replace[i]] = over.Target[i];
                }
                return over.Header.icon.key;
            } else if (record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverrideSecondary))) {
                TextureOverrideSecondary over = (TextureOverrideSecondary)record.Instances[0];
                for (int i = 0; i < over.Replace.Length; ++i) {
                    if (!map.ContainsKey(over.Target[i])) {
                        continue;
                    }
                    if (replace.ContainsKey(over.Replace[i])) {
                        continue;
                    }
                    replace[over.Replace[i]] = over.Target[i];
                }
            }
            return 0;
        }

        public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, string itemGroup, List<ulong> ignore, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags, ulong masterKey, int replacementIndex) {
            string path = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName), Util.SanitizePath(itemGroup));

            SkinItem skin = (SkinItem)itemStud.Instances[0];

            HashSet<ulong> models = new HashSet<ulong>();
            Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
            HashSet<ulong> parsed = new HashSet<ulong>();
            Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
            Dictionary<ulong, List<ulong>> sound = new Dictionary<ulong, List<ulong>>();

            ulong guiIcon = ExtractData(skin, master, true, models, animList, parsed, layers, replace, sound, ignore, replacementIndex, map, handler);
            
            Save(master, path, heroName, itemName, replace, parsed, models, layers, animList, flags, track, map, handler, masterKey, false, quiet, sound, guiIcon);
        }

        public static ulong ExtractData(SkinItem skin, HeroMaster master, bool findReplacements, HashSet<ulong> models, Dictionary<ulong, ulong> animList, HashSet<ulong> parsed, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, Dictionary<ulong, List<ulong>> sound, List<ulong> ignore, int replacementIndex, Dictionary<ulong, Record> map, CASCHandler handler) {
            ulong guiIcon = 0;
            if (findReplacements) {
                guiIcon = FindReplacements(skin.Data.skin.key, replacementIndex, replace, parsed, map, handler, master, skin);
            }

            ulong bindingKey = master.Header.binding.key;
            if (replace.ContainsKey(bindingKey)) {
                bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            bindingKey = master.Header.child1.key;
            if (replace.ContainsKey(bindingKey)) {
                bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            bindingKey = master.Header.child2.key;
            if (replace.ContainsKey(bindingKey)) {
                bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            bindingKey = master.Header.child3.key;
            if (replace.ContainsKey(bindingKey)) {
                bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            bindingKey = master.Header.child4.key;
            if (replace.ContainsKey(bindingKey)) {
                bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            foreach (HeroMaster.HeroChild1 child in master.Child1) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            }
            foreach (HeroMaster.HeroChild2 child in master.Child3) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            }
            foreach (HeroMaster.HeroChild2 child in master.Child3) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler, sound);
            }

            return guiIcon;
        }

        private static bool tryOpt(List<char> opts, int index, char target, bool @default = false) {
            if (opts.Count > index && opts[index] == target) {
                return !@default;
            }
            return @default;
        }

        private static char tryOptChar(List<char> opts, int index, char @default) {
            if (opts.Count > index) {
                return opts[index];
            }
            return @default;
        }

        public static void Save(HeroMaster master, string path, string heroName, string itemName, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> animList, OverToolFlags flags, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, ulong heroKey, bool external, bool quiet, Dictionary<ulong, List<ulong>> sound, ulong guiIcon) {
            char modelEncoding = flags.ModelFormat;
            if (flags.Raw) {
                modelEncoding = '+';
            }
            char animEncoding = flags.AnimFormat;
            if (flags.Raw) {
                animEncoding = '+';
            }
            bool suppressTextures = flags.SkipTextures;
            bool suppressAnimations = flags.SkipAnimations;
            if (animEncoding == '+' && !flags.RawAnimation) {
                suppressAnimations = true;
            }
            bool suppressModels = flags.SkipModels;
            if (modelEncoding == '+' && !flags.RawModel) {
                suppressModels = true;
            }
            bool suppressSounds = flags.SkipSound;
            bool exportCollision = flags.ExportCollision;
            bool suppressRefpose = flags.SkipRefpose;
            bool suppressGUI = flags.SkipGUI;
            bool raw = flags.Raw;

            Dictionary<string, TextureType> typeInfo = new Dictionary<string, TextureType>();
            if (!suppressTextures) {
                foreach (KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
                    ulong materialId = kv.Key;
                    List<ImageLayer> sublayers = kv.Value;
                    HashSet<ulong> materialParsed = new HashSet<ulong>();
                    foreach (ImageLayer layer in sublayers) {
                        if (!materialParsed.Add(layer.Key)) {
                            continue;
                        }
                        KeyValuePair<string, TextureType> stt = SaveTexture(layer.Key, materialId, map, handler, path, quiet);
                        if (stt.Key == null) {
                            continue;
                        }
                        typeInfo.Add(stt.Key, stt.Value);
                    }
                }
            }

            IDataWriter writer = null;
            string mtlPath = null;
            if (modelEncoding != 0 && modelEncoding != '+') {
                Assembly asm = typeof(IDataWriter).Assembly;
                Type t = typeof(IDataWriter);
                List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    if (writer != null) {
                        break;
                    }
                    if (tt.IsInterface) {
                        continue;
                    }

                    IDataWriter tmp = (IDataWriter)Activator.CreateInstance(tt);
                    for (int i = 0; i < tmp.Identifier.Length; ++i) {
                        if (tmp.Identifier[i] == modelEncoding) {
                            writer = tmp;
                            break;
                        }
                    }
                }
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


            if (writer == null) {
                writer = new OWMDLWriter();
            }

            if (!suppressTextures && typeInfo.Count > 0) {
                if (writer.GetType() == typeof(OWMDLWriter) || modelEncoding == '+') {
                    IDataWriter tmp = new OWMATWriter();
                    mtlPath = $"{path}material{tmp.Format}";
                    using (Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
                        if (tmp.Write(null, outp, null, layers, new object[3] { typeInfo, Path.GetFileName(mtlPath), $"{heroName} Skin {itemName}" })) {
                            if (!quiet) {
                                Console.Out.WriteLine("Wrote materials {0}", mtlPath);
                            }
                        } else {
                            if (!quiet) {
                                Console.Out.WriteLine("Failed to write material");
                            }
                        }
                    }
                } else if (writer.GetType() == typeof(OBJWriter)) {
                    writer = new OBJWriter();
                    IDataWriter tmp = new MTLWriter();
                    mtlPath = $"{path}material{tmp.Format}";
                    using (Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
                        if (tmp.Write(null, outp, null, layers, new object[3] { false, Path.GetFileName(mtlPath), $"{heroName} Skin {itemName}" })) {
                            if (!quiet) {
                                Console.Out.WriteLine("Wrote materials {0}", mtlPath);
                            }
                        } else {
                            if (!quiet) {
                                Console.Out.WriteLine("Failed to write material");
                            }
                        }
                    }
                }
            }

            IDataWriter refpose = new RefPoseWriter();

            bool skipCmodel = !exportCollision;

            if (!suppressModels) {
                List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });
                foreach (ulong key in models) {
                    if (!map.ContainsKey(key)) {
                        continue;
                    }

                    string outpath;

                    if (!Directory.Exists(Path.GetDirectoryName(path))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }

                    outpath = $"{path}{GUID.LongKey(key):X12}.{GUID.Type(key):X3}";
                    if (flags.RawModel) {
                        using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                            Util.OpenFile(map[key], handler).CopyTo(outp);
                            if (!quiet) {
                                Console.Out.WriteLine("Wrote raw model {0}", outpath);
                            }
                        }
                    }

                    if (modelEncoding == '+') { // raw
                        continue;
                    }

                    Chunked mdl = new Chunked(Util.OpenFile(map[key], handler));

                    if (!suppressRefpose && mdl.HasChunk<lksm>()) {
                        outpath = $"{path}{GUID.LongKey(key):X12}_refpose{refpose.Format}";
                        using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                            if (refpose.Write(mdl, outp, null, null, null)) {
                                if (!quiet) {
                                    Console.Out.WriteLine("Wrote reference pose {0}", outpath);
                                }
                            }
                        }
                    }

                    string mdlName = $"{heroName} Skin {itemName}_{GUID.Index(key):X}";

                    outpath = $"{path}{GUID.LongKey(key):X12}{writer.Format}";

                    using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                        if (writer.Write(mdl, outp, lods, layers, new object[5] { true, Path.GetFileName(mtlPath), mdlName, null, skipCmodel })) {
                            if (!quiet) {
                                Console.Out.WriteLine("Wrote model {0}", outpath);
                            }
                        } else {
                            if (!quiet) {
                                Console.Out.WriteLine("Failed to write model");
                            }
                        }
                    }
                }
            }

            if (!suppressAnimations) {
                foreach (KeyValuePair<ulong, ulong> kv in animList) {
                    ulong parent = kv.Value;
                    ulong key = kv.Key;

                    Stream animation = Util.OpenFile(map[key], handler);
                    if (animation == null) {
                        continue;
                    }



                    Animation anim = new Animation(animation, true);
                    animation.Position = 0;
                    string outpath = string.Format("{0}{5}{1}{2:X12}{1}{6}{1}{3:X12}.{4:X3}", path, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), GUID.Type(key), external ? "" : "Animations", anim.Header.priority);
                    if (!Directory.Exists(Path.GetDirectoryName(outpath))) {
                        Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                    }
                    if (flags.RawAnimation) {
                        using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                            animation.CopyTo(outp);
                            animation.Close();
                            if (!quiet) {
                                Console.Out.WriteLine("Wrote raw animation {0}", outpath);
                            }
                        }
                    }
                    if (animWriter != null && animEncoding != '+') {
                        outpath = string.Format("{0}{5}{1}{2:X12}{1}{6}{1}{3:X12}{4}", path, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), animWriter.Format, external ? "" : "Animations", anim.Header.priority);
                        using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                            try {
                                animWriter.Write(anim, outp, new object[] { });
                                if (!quiet) {
                                    Console.Out.WriteLine("Wrote animation {0}", outpath);
                                }
                            } catch {
                                if (!quiet) {
                                    Console.Error.WriteLine("Error with animation {0:X12}.{1:X3}", GUID.Index(key), GUID.Type(key));
                                }
                            }
                        }
                    }
                }
            }

            if (!suppressSounds) {
                Dictionary<ulong, List<ulong>> soundData = null;
                if (master != null) {
                    Console.Out.WriteLine("Dumping voice bites for hero {0} with skin {1}", heroName, itemName);
                    soundData = Sound.FindSounds(master, track, map, handler, replace, heroKey, sound);
                } else {
                    soundData = sound;
                }

                if (soundData != null && soundData.Count > 0) {
                    string outpath = $"{path}Sound{Path.DirectorySeparatorChar}";
                    if (!Directory.Exists(outpath)) {
                        Directory.CreateDirectory(outpath);
                    }
                    DumpVoice.Save(outpath, soundData, map, handler, quiet, replace);
                }
            }

            if (!suppressGUI && master != null) {
                string output = string.Format("{0}GUI{1}", path, Path.DirectorySeparatorChar);

                if (Directory.Exists(output)) {
                    Directory.CreateDirectory(output);
                }

                HashSet<ulong> done = new HashSet<ulong>();
                if (done.Add(master.Header.texture1.key)) {
                    SaveIcon(output, master.Header.texture1.key, replace, map, handler, quiet);
                }
                if (done.Add(master.Header.texture2.key)) {
                    SaveIcon(output, master.Header.texture2.key, replace, map, handler, quiet);
                }
                if (done.Add(master.Header.texture3.key)) {
                    SaveIcon(output, master.Header.texture3.key, replace, map, handler, quiet);
                }
                if (done.Add(master.Header.texture4.key)) {
                    SaveIcon(output, master.Header.texture4.key, replace, map, handler, quiet);
                }

                if (guiIcon > 0 && done.Add(guiIcon)) {
                    SaveIcon(output, guiIcon, replace, map, handler, quiet);
                }
            }

#if OUTPUT_ANIMSTUD
            foreach (ulong key in animList.Values) {
                if (key == 0) {
                    continue;
                }
                string outFilename = $"{path}STUD{Path.DirectorySeparatorChar}Animation{Path.DirectorySeparatorChar}{key:X16}.stud";
                if (File.Exists(outFilename)) {
                    continue;
                }
                Stream stud = Util.OpenFile(map[key], handler);
                if (stud == null) {
                    continue;
                }
                if (!Directory.Exists(Path.GetDirectoryName(outFilename))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(outFilename));
                }
                using (Stream OutWriter = File.Create(outFilename)) {
                    stud.CopyTo(OutWriter);
                }
                stud.Close();
            }
#endif
        }

        public static void SaveIcon(string output, ulong key, Dictionary<ulong, ulong> replace, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet) {
            if (replace.ContainsKey(key)) {
                key = replace[key];
            }
            if (key != 0) {
                SaveTexture(key, 0, map, handler, output, quiet);
            }
        }

        public static KeyValuePair<string, TextureType> SaveTexture(ulong key, ulong material, Dictionary<ulong, Record> map, CASCHandler handler, string outp, bool quiet, string prefix = "Textures") {
            string name = $"{GUID.LongKey(key):X12}.dds";
            if (material > 0) {
                name = $"{prefix}{Path.DirectorySeparatorChar}{material:X16}{Path.DirectorySeparatorChar}{name}";
            }
            TextureType @type = TextureType.Unknown;

            if (!map.ContainsKey(key)) {
                return new KeyValuePair<string, TextureType>(name, @type);
            }

            string path = $"{outp}{Path.DirectorySeparatorChar}{name}";

            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            ulong imageDataKey = (key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
            bool dbl = map.ContainsKey(imageDataKey);
            using (Stream mainFile = Util.OpenFile(map[key], handler)) {
                if (mainFile == null || mainFile.Length == 0) {
                    return new KeyValuePair<string, TextureType>(null, 0);
                }

                using (MemoryStream output = new MemoryStream()) {
                    if (map.ContainsKey(imageDataKey)) {
                        Texture tex = new Texture(mainFile, Util.OpenFile(map[imageDataKey], handler));
                        tex.Save(output, true);
                        @type = tex.Format;
                    } else {
                        TextureLinear tex = new TextureLinear(mainFile);
                        tex.Save(output, true);
                        @type = tex.Header.Format();
                    }
                    if (File.Exists(path)) {
                        return new KeyValuePair<string, TextureType>(name, @type);
                    }
                    if (output.Length > 0) {
                        output.Position = 0;
                        using (Stream file = File.Open(path, FileMode.Create, FileAccess.Write)) {
                            output.CopyTo(file);
                        }
                    }
                }
            }
            if (!quiet) {
                Console.Out.WriteLine("Wrote texture {0}", path);
            }
            return new KeyValuePair<string, TextureType>(name, @type);
        }
    }
}
