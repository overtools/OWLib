using System.Runtime.InteropServices;
using OWLib;
using OWReplayLib.Serializer;

namespace OWReplayLib.Types {
    public static class Common {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FileReference {
            public ulong Key;

            public override string ToString() {
                return GUID.AsString(Key);
            }

            public static implicit operator ulong(FileReference obj) {
                return obj.Key;
            }
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IntFileReference {
            public uint Key;

            public override string ToString() {
                return $"{GUID.LongKey(Key):X8}";
            }

            public static implicit operator uint(IntFileReference obj) {
                return obj.Key;
            }
        }

        public class HeroInfo : ReadableData {
            public IntFileReference SkinId;
            public IntFileReference WeaponSkinId;
            public IntFileReference HighlightIntro;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(IntFileReference))]
            public IntFileReference[] SprayIds;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(IntFileReference))]
            public IntFileReference[] VoiceLineIds;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(IntFileReference))]
            public IntFileReference[] EmoteIds;

            public int Unknown;
            
            public FileReference HeroMasterKey;
        }
    }
}