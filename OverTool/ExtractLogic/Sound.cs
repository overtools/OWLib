using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.STUD;
using OWLib.Types.STUD.Binding;
using OWLib.Types.STUD.GameParam;

namespace OverTool.ExtractLogic {
    class Sound {
        private static bool CheckAddEntry(Dictionary<ulong, List<ulong>> ret, ulong parent, ulong key) {
            ushort type = GUID.Type(key);
            if (type == 0x03F || type == 0x043 || type == 0x0B2 || type == 0x0BB) {
                if (!ret.ContainsKey(parent)) {
                    ret[parent] = new List<ulong>();
                }
                if (!ret[parent].Contains(key)) {
                    ret[parent].Add(key);
                }
                return true;
            }
            return false;
        }

        public static void FindSoundsEx(ulong key, HashSet<ulong> done, Dictionary<ulong, List<ulong>> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace, ulong parent) {
            if (replace.ContainsKey(key)) {
                key = replace[key];
            }
            if (!map.ContainsKey(key)) {
                return;
            }
            if (!done.Add(key)) {
                return;
            }
            if (CheckAddEntry(ret, parent, key)) {
                return;
            }

            using (Stream studStream = Util.OpenFile(map[key], handler)) {
                if (studStream == null) {
                    return;
                }
                STUD stud = new STUD(studStream, true, STUDManager.Instance, false, true);
                FindSoundsSTUD(stud, done, ret, map, handler, replace, parent, key);
            }
        }


        public static void FindSoundsExD(ulong key, HashSet<ulong> done, Dictionary<ulong, List<ulong>> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace, ulong parent) {
            if (replace.ContainsKey(key)) {
                key = replace[key];
            }
            if (!map.ContainsKey(key)) {
                return;
            }
            if (!done.Add(key)) {
                return;
            }
            if (CheckAddEntry(ret, parent, key)) {
                return;
            }

            using (Stream effectStream = Util.OpenFile(map[key], handler)) {
                if (effectStream == null) {
                    return;
                }
                Chunked chunked = new Chunked(effectStream, true, ChunkManager.Instance);
                FindSoundsChunked(chunked, done, ret, map, handler, replace, parent, key);
            }
        }

        public static void FindSoundsChunked(Chunked chunked, HashSet<ulong> done, Dictionary<ulong, List<ulong>> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace, ulong parent, ulong mykey) {
            OSCE[] osces = chunked.GetAllOfTypeFlat<OSCE>();
            foreach (OSCE osce in osces) {
                FindSoundsEx(osce.Data.Sound, done, ret, map, handler, replace, mykey);
            }

            FECE[] feces = chunked.GetAllOfTypeFlat<FECE>();
            foreach (FECE fece in feces) {
                FindSoundsExD(fece.Data.Effect, done, ret, map, handler, replace, mykey);
            }
        }

        private static ulong MutateKey(ulong key, ushort value) {
            return (key & ~0xFFFF00000000ul) | (((ulong)value) << 32);
        }

        public static void FindSoundsSTUD(STUD stud, HashSet<ulong> done, Dictionary<ulong, List<ulong>> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace, ulong parent, ulong mykey) {
            foreach (ISTUDInstance instance in stud.Instances) {
                if (instance == null) {
                    continue;
                }

                if (instance.Name == stud.Manager.GetName(typeof(GenericRecordReference))) {
                    GenericRecordReference inst = (GenericRecordReference)instance;
                    FindSoundsEx(inst.Reference.key.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(UISoundList))) {
                    UISoundList inst = (UISoundList)instance;
                    foreach (UISoundList.SoundListEntry[] list in inst.Entries) {
                        foreach (UISoundList.SoundListEntry entry in list) {
                            FindSoundsExD(entry.sound, done, ret, map, handler, replace, mykey);
                        }
                    }
                } else if (instance.Name == stud.Manager.GetName(typeof(GenericSoundReference))) {
                    GenericSoundReference inst = (GenericSoundReference)instance;
                    FindSoundsEx(inst.Reference.key.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(ChildGameParameterRecord))) {
                    ChildGameParameterRecord inst = (ChildGameParameterRecord)instance;
                    FindSoundsEx(inst.Param.binding.key, done, ret, map, handler, replace, mykey);
                    FindSoundsEx(inst.Param.binding2.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(SoundMasterList))) {
                    SoundMasterList smr = (SoundMasterList)instance;
                    foreach (ulong key in smr.Sound) {
                        FindSoundsEx(key, done, ret, map, handler, replace, mykey);
                    }
                    if (smr.Owner != null) {
                        foreach (ulong key in smr.Owner) {
                            FindSoundsEx(key, done, ret, map, handler, replace, mykey);
                        }
                    }
                } else if (instance.Name == stud.Manager.GetName(typeof(SoundOwner))) {
                    SoundOwner owner = (SoundOwner)instance;
                    FindSoundsEx(owner.Data.soundbank.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(SoundBank))) {
                    SoundBank sb = (SoundBank)instance;
                    FindSoundsEx(sb.Data.soundbank.key, done, ret, map, handler, replace, mykey);
                    if (sb.SFX != null) {
                        foreach (OWRecord record in sb.SFX) {
                            FindSoundsEx(record.key, done, ret, map, handler, replace, mykey);
                        }
                    }
                    if (sb.Music != null) {
                        foreach (OWRecord record in sb.Music) {
                            FindSoundsEx(record.key, done, ret, map, handler, replace, mykey);
                        }
                    }
                } else if (instance.Name == stud.Manager.GetName(typeof(ParameterRecord))) {
                    ParameterRecord parameter = (ParameterRecord)instance;
                    foreach (ParameterRecord.ParameterEntry entry in parameter.Parameters) {
                        FindSoundsEx(entry.parameter.key, done, ret, map, handler, replace, mykey);
                    }
                } else if (instance.Name == stud.Manager.GetName(typeof(SoundBindingReference))) {
                    SoundBindingReference smr = (SoundBindingReference)instance;
                    FindSoundsEx(smr.Reference.sound.key, done, ret, map, handler, replace, MutateKey(mykey, (ushort)smr.Reference.Typus));
                } else if (instance.Name == stud.Manager.GetName(typeof(BindingRecord))) {
                    BindingRecord record = (BindingRecord)instance;
                    FindSoundsEx(record.Param.binding.key, done, ret, map, handler, replace, mykey);
                    FindSoundsEx(record.Param.binding2.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(SoundFX))) {
                    SoundFX record = (SoundFX)instance;
                    FindSoundsEx(record.Param.binding.key, done, ret, map, handler, replace, mykey);
                    FindSoundsEx(record.Param.binding2.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(ChildParameterRecord))) {
                    ChildParameterRecord record = (ChildParameterRecord)instance;
                    FindSoundsEx(record.Header.binding.key, done, ret, map, handler, replace, mykey);
                    foreach (ChildParameterRecord.Child child in record.Children) {
                        FindSoundsEx(child.parameter.key, done, ret, map, handler, replace, mykey);
                    }
                } else if (instance.Name == stud.Manager.GetName(typeof(EffectReference))) {
                    EffectReference reference = (EffectReference)instance;
                    FindSoundsExD(reference.Reference.key.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(BindingEffectReference))) {
                    BindingEffectReference reference = (BindingEffectReference)instance;
                    // Exports a LOT of system sound effects unrelated to the origin.
                    FindSoundsExD(reference.Reference.effect.key, done, ret, map, handler, replace, mykey);
                } else if (instance.Name == stud.Manager.GetName(typeof(GenericKeyReference))) {
                    GenericKeyReference reference = (GenericKeyReference)instance;
                    FindSoundsEx(reference.Reference.key.key, done, ret, map, handler, replace, mykey);
                }
            }
        }

        public static Dictionary<ulong, List<ulong>> FindSounds(HeroMaster master, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace = null, ulong parent = 0, Dictionary<ulong, List<ulong>> @base = null) {
            if (master == null) {
                return null;
            }

            Dictionary<ulong, List<ulong>> ret = @base;
            if (ret == null) {
                ret = new Dictionary<ulong, List<ulong>>();
            }

            HashSet<ulong> done = new HashSet<ulong>();

            if (replace == null) {
                replace = new Dictionary<ulong, ulong>();
            }

            FindSoundsEx(master.Header.binding.key, done, ret, map, handler, replace, parent);
            FindSoundsEx(master.Header.child1.key, done, ret, map, handler, replace, parent);
            FindSoundsEx(master.Header.child2.key, done, ret, map, handler, replace, parent);
            FindSoundsEx(master.Header.child3.key, done, ret, map, handler, replace, parent);
            FindSoundsEx(master.Header.child4.key, done, ret, map, handler, replace, parent);
            ulong bindingKey = 0;
            foreach (HeroMaster.HeroChild1 child in master.Child1) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindSoundsEx(bindingKey, done, ret, map, handler, replace, parent);
            }
            foreach (HeroMaster.HeroChild2 child in master.Child3) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindSoundsEx(bindingKey, done, ret, map, handler, replace, parent);
            }
            foreach (HeroMaster.HeroChild2 child in master.Child3) {
                bindingKey = child.record.key;
                if (replace.ContainsKey(bindingKey)) {
                    bindingKey = replace[bindingKey];
                }
                FindSoundsEx(bindingKey, done, ret, map, handler, replace, parent);
            }
            return ret;
        }
    }
}
