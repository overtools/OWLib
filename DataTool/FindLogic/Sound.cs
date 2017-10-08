using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Helper.STUHelper;

namespace DataTool.FindLogic {
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class SoundInfo : IEquatable<SoundInfo> {
        public Common.STUGUID GUID;
        public string Subtitle;
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
        public static void AddGUID(Dictionary<ulong, List<SoundInfo>> sounds, Common.STUGUID newElement, ulong parentKey, string subtitle) {
            if (newElement == null) return;
            if (!sounds.ContainsKey(parentKey)) {
                sounds[parentKey] = new List<SoundInfo>();
            }

            SoundInfo newSound = new SoundInfo {GUID = newElement, Subtitle = subtitle};

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
        
        public static Dictionary<ulong, List<SoundInfo>> FindSounds(Dictionary<ulong, List<SoundInfo>> existingSounds, Common.STUGUID soundGUID) {
            if (existingSounds == null) {
                existingSounds = new Dictionary<ulong, List<SoundInfo>>();
            }

            if (soundGUID == null) return existingSounds;
            
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
                    STUSoundMaster th = GetInstance<STUSoundMaster>(soundGUID);
                    if (th == null) break;
                    foreach (STUSoundHolder soundThingy in th.SoundHolders) {
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
                            AddGUID(existingSounds, th2.Sound1?.SoundResource, soundGUID, subtitle1);
                            AddGUID(existingSounds, th2.Sound2?.SoundResource, soundGUID, subtitle2);
                            AddGUID(existingSounds, th2.Sound3?.SoundResource, soundGUID, subtitle3);
                            AddGUID(existingSounds, th2.Sound4?.SoundResource, soundGUID, subtitle4);
                            if (th2.Sound2 != null) {
                                Debugger.Break();
                            }
                            if (th2.Sound3 != null) {
                                Debugger.Break();
                            }
                            if (th2.Sound4 != null) {
                                Debugger.Break();
                            }
                        }
                        if (soundThingy.SoundDataContainer?.SoundbankMasterResource == null) continue;
                        FindSounds(existingSounds, soundThingy.SoundDataContainer.SoundbankMasterResource);
                    }
                    break;
                case 0x02C:
                    STUSound sbM = GetInstance<STUSound>(soundGUID);
                    AddGUID(existingSounds, sbM?.Inner?.Soundbank, soundGUID, null);
                    if (sbM?.Inner?.Sounds == null) break;
                    foreach (Common.STUGUID sound in sbM.Inner.Sounds) {
                        AddGUID(existingSounds, sound, soundGUID, null);
                    }
                    break;
                case 0x043:
                    AddGUID(existingSounds, soundGUID, soundGUID, null);
                    break;
            }

            return existingSounds;
        }
    }
}