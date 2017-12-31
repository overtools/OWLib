using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.Statescript.Components;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class SoundInfo : IEquatable<SoundInfo> {
        public Common.STUGUID GUID;
        public string Subtitle;
        public string Name;
        internal string DebuggerDisplay => $"{GUID.ToString()}{(Subtitle != null ? $" - {Subtitle}" : "")}";

        public bool Equals(SoundInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GUID, other.GUID);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SoundInfo) obj);
        }

        public override int GetHashCode() {
            return GUID != null ? GUID.GetHashCode() : 0;
        }
    }
    
    public static class Sound {
        public static void AddGUID(Dictionary<ulong, List<SoundInfo>> sounds, Common.STUGUID newElement, ulong parentKey, string subtitle, string name, bool forceZero) {
            if (newElement == null) return;
            if (forceZero) parentKey = 0;
            if (!sounds.ContainsKey(parentKey)) {
                sounds[parentKey] = new List<SoundInfo>();
            }

            SoundInfo newSound = new SoundInfo {GUID = newElement, Subtitle = subtitle, Name = name};

            if (!sounds[parentKey].Contains(newSound)) {
                sounds[parentKey].Add(newSound);
            } else {
                SoundInfo existing = sounds[parentKey].FirstOrDefault(x => Equals(x, newSound));
                if (existing == null) return;
                if (existing.Subtitle == null && subtitle != null) {
                    existing.Subtitle = subtitle;
                }
            }
        }

        public static Dictionary<ulong, List<SoundInfo>> FindSounds(Dictionary<ulong, List<SoundInfo>> existingSounds,
            STUSound07ASub sub07A, string name=null, bool forceZero=false, ulong toplevelKey=0, Dictionary<ulong, ulong> replacements=null) {
            existingSounds = FindSounds(existingSounds, sub07A?.Sound3, name, forceZero, toplevelKey, replacements);
            existingSounds = FindSounds(existingSounds, sub07A?.Sound1, name, forceZero, toplevelKey, replacements);
            existingSounds = FindSounds(existingSounds, sub07A?.Sound2, name, forceZero, toplevelKey, replacements);
            return existingSounds;
        }

        public static Dictionary<ulong, List<SoundInfo>> FindSoundsChunked(Dictionary<ulong, List<SoundInfo>> existingSounds, Common.STUGUID soundGUID, string name=null, bool forceZero=false, ulong toplevelKey=0, Dictionary<ulong, ulong> replacements=null) {
            if (existingSounds == null) {
                existingSounds = new Dictionary<ulong, List<SoundInfo>>();
            }
            if (soundGUID == null) return existingSounds;
            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (replacements.ContainsKey(soundGUID)) soundGUID = new Common.STUGUID(replacements[soundGUID]);

            using (Stream chunkStream = OpenFile(soundGUID)) {
                if (chunkStream == null) {
                    return existingSounds;
                }
                Chunked chunked = new Chunked(chunkStream, true, ChunkManager.Instance);
                
                OSCE[] osces = chunked.GetAllOfTypeFlat<OSCE>();
                foreach (OSCE osce in osces) {
                    existingSounds = FindSounds(existingSounds, osce.Data.Sound, name, forceZero, toplevelKey, replacements);
                }

                FECE[] feces = chunked.GetAllOfTypeFlat<FECE>();
                foreach (FECE fece in feces) {   // good variable name
                    existingSounds = FindSounds(existingSounds, fece.Data.Effect, name, forceZero, toplevelKey, replacements);
                }
            }

            return existingSounds;
        }

        private static Dictionary<ulong, List<SoundInfo>> FindSounds(Dictionary<ulong, List<SoundInfo>> existingSounds, ulong soundGuid, string name=null, bool forceZero=false, ulong toplevelKey=0, Dictionary<ulong, ulong> replacements=null) {
            return FindSounds(existingSounds, new Common.STUGUID(soundGuid), name, forceZero, toplevelKey, replacements);
        }

        public static Dictionary<ulong, List<SoundInfo>> FindSounds(Dictionary<ulong, List<SoundInfo>> existingSounds, Common.STUGUID soundGUID, string name=null, bool forceZero=false, ulong toplevelKey=0, Dictionary<ulong, ulong> replacements=null) {
            if (existingSounds == null) {
                existingSounds = new Dictionary<ulong, List<SoundInfo>>();
            }

            if (soundGUID == null) return existingSounds;
            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (replacements.ContainsKey(soundGUID)) soundGUID = new Common.STUGUID(replacements[soundGUID]);
            if (toplevelKey == 0) toplevelKey = soundGUID;
            
            // 05F todos:
            
            // hero references, for interactions?
            // BC474019|SoundDataContainer:
            //     093FCEEB|m_093FCEEB: 0
            //     38F3ED5E|m_38F3ED5E: null
            //     401F5484|m_401F5484:
            //         0000000002C2.078|unknown type
            //     4FF98D41|m_4FF98D41:
            //         0619C597|m_0619C597: 0
            //         4FF98D41|m_4FF98D41:
            //             0619C597|m_0619C597: 11
            //             07D0F7AA|m_07D0F7AA: 1
            //             57D96E27|m_57D96E27: 0
            //             8C8C5285|m_8C8C5285:
            //                 000000000004.075|Hero: Mercy

            switch (GUID.Type(soundGUID)) {
                case 0x05F:
                    STUVoiceMaster th = GetInstance<STUVoiceMaster>(soundGUID);
                    if (th == null) break;
                    foreach (STUVoiceLineInstance soundThingy in th.VoiceLineInstances) {
                        string subtitle1 = null;
                        string subtitle2 = null;
                        string subtitle3 = null;
                        string subtitle4 = null;
                        if (soundThingy.Subtitle != null) {
                            STUSubtitleContainer subtitleContainer = GetInstance<STUSubtitleContainer>(soundThingy.Subtitle);
                            // todo: I presume that these match up with the 4 sounds
                            subtitle1 = subtitleContainer?.Subtitle1?.Text;
                            subtitle2 = subtitleContainer?.Subtitle2?.Text;
                            subtitle3 = subtitleContainer?.Subtitle3?.Text;
                            subtitle4 = subtitleContainer?.Subtitle4?.Text;
                        }
                        STUSoundConainer th2 = soundThingy.SoundContainer;
                        if (th2 != null) {
                            AddGUID(existingSounds, th2.Sound1?.SoundResource, toplevelKey, subtitle1, name, forceZero);
                            AddGUID(existingSounds, th2.Sound2?.SoundResource, toplevelKey, subtitle2, name, forceZero);
                            AddGUID(existingSounds, th2.Sound3?.SoundResource, toplevelKey, subtitle3, name, forceZero);
                            AddGUID(existingSounds, th2.Sound4?.SoundResource, toplevelKey, subtitle4, name, forceZero);
                        }
                        if (soundThingy.SoundDataContainer?.SoundbankMasterResource == null) continue;
                        existingSounds = FindSounds(existingSounds, soundThingy.SoundDataContainer.SoundbankMasterResource, null, forceZero, toplevelKey, replacements);
                    }
                    break;
                case 0x02C:
                    STUSound sbM = GetInstance<STUSound>(soundGUID);
                    AddGUID(existingSounds, sbM?.Inner?.Soundbank, toplevelKey, null, name, forceZero);
                    if (sbM?.Inner?.Sounds != null) {
                        foreach (Common.STUGUID sound in sbM.Inner.Sounds) {
                            AddGUID(existingSounds, sound, toplevelKey, null, name, forceZero);
                        }
                    }
                    if (sbM?.Inner?.SoundOther != null) {
                        foreach (Common.STUGUID music in sbM.Inner.SoundOther) {
                            AddGUID(existingSounds, music, toplevelKey, null, name, forceZero);
                        }
                    }
                    break;
                case 0x043:
                    AddGUID(existingSounds, soundGUID, toplevelKey, null, name, forceZero);
                    break;
                case 0x0D:
                    existingSounds = FindSoundsChunked(existingSounds, soundGUID, null, forceZero, toplevelKey, replacements);
                    break;
                case 0x07A:
                    STUSound07A x07A = GetInstance<STUSound07A>(soundGUID);
                    if (x07A == null) break;
                    // eww
                    existingSounds = FindSounds(existingSounds, x07A.Sub1, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub2, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub3, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub4, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub5, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub6, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub7, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub8, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub9, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub10, name, forceZero, toplevelKey, replacements);
                    existingSounds = FindSounds(existingSounds, x07A.Sub11, name, forceZero, toplevelKey, replacements);
                    break;
                case 0x04A:
                    FindSoundsChunked(existingSounds, soundGUID, null, forceZero, toplevelKey);
                    break;
                case 0x049:
                    STUUISoundList uiSoundList = GetInstance<STUUISoundList>(soundGUID);
                    foreach (STUUISoundListEffectContainer effectContainer in uiSoundList.EffectContainer) {
                        foreach (STUUISoundListEffect effect in effectContainer.Effects) {
                            FindSounds(existingSounds, effect.Effect, null, forceZero, toplevelKey, replacements);
                        }
                    }
                    break;
                case 0x03:
                    STUEntityDefinition container = GetInstance<STUEntityDefinition>(soundGUID);
                    foreach (KeyValuePair<ulong,STUEntityComponent> statescriptComponent in container.Components) {
                        STUEntityComponent component = statescriptComponent.Value;
                        if (component == null) continue;
                        if (component.GetType() == typeof(STUEntityVoiceMaster)) {
                            STUEntityVoiceMaster ssSoundMaster = component as STUEntityVoiceMaster;
                            existingSounds = FindSounds(existingSounds, ssSoundMaster?.VoiceMaster, null, forceZero, toplevelKey, replacements);
                        } else if (component.GetType() == typeof(STUStatescript07A)) {
                            STUStatescript07A ss07A = component as STUStatescript07A;
                            existingSounds = FindSounds(existingSounds, ss07A?.GUIDx07A, null, forceZero, toplevelKey, replacements);
                        } else if (component.GetType() == typeof(STUFirstPersonComponent)) {
                            // hmm, references another 003
                            // STU_9D28963F ss9D28963F = component as STU_9D28963F;
                            // existingSounds = FindSounds(existingSounds, ss9D28963F?.m_A83C2C26, replacements);
                        } else if (component.GetType() == typeof(STUStatescript049)) {
                            STUStatescript049 ss049 = component as STUStatescript049;
                            existingSounds = FindSounds(existingSounds, ss049?.GUIDx049, null, forceZero, toplevelKey, replacements);
                        } 
                        // else if (component.GetType() == typeof(STU_FD024F42)) {
                        //     STU_FD024F42 ssFD = component as STU_FD024F42;
                        //     if (ssFD?.m_B634821A != null) {
                        //         foreach (STU_7B6EA463 ss7B in ssFD.m_B634821A) {
                        //             existingSounds = FindSounds(existingSounds, ss7B.m_C71EA6BC, null, forceZero, toplevelKey, replacements);
                        //         }
                        //     }
                        // }
                    }
                    break;
                default:
                    Debugger.Log(0, "DataTool.FindLogic.Sound", $"[DataTool.FindLogic.Sound] Unhandled type: {GUID.Type(soundGUID):X3}\n");
                    break;
            }

            return existingSounds;
        }
    }
}