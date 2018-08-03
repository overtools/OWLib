using System;
using System.Globalization;
using System.Linq;
using TankLib.STU.Types;

namespace TankLib.STU {
    public static class InstanceExtensions {
        #region STUMapHeader

        public static ulong GetChunkRoot(this STUMapHeader w) {
            return (w.m_map & ~0xFFFFFFFF00000000ul) | 0x0DD0000100000000ul;
        }
        
        public static ulong GetChunkKey(this STUMapHeader w, byte type) {
            return (GetChunkRoot(w) & ~0xFFFF00000000ul) | ((ulong) type << 32);
        }
        
        public static ulong GetChunkKey(this STUMapHeader w, Enums.teMAP_PLACEABLE_TYPE type) {
            return (GetChunkRoot(w) & ~0xFFFF00000000ul) | ((ulong) type << 32);
        }
        #endregion
        
        #region STUResourceKey

        public static ulong GetKeyID(this STUResourceKey key) {
            return ulong.Parse(key.m_keyID, NumberStyles.HexNumber);
        }

        public static ulong GetReverseKeyID(this STUResourceKey key) {
            return BitConverter.ToUInt64(BitConverter.GetBytes(key.GetKeyID()).Reverse().ToArray(), 0);
        }

        public static string GetKeyValueString(this STUResourceKey key) {
            return BitConverter.ToString(key.m_key).Replace("-", string.Empty);
        }
        
        public static string GetKeyIDString(this STUResourceKey key) {
            return key.GetReverseKeyID().ToString("X16");
        }

        #endregion
        
        #region STUEntityDefinition
        
        public static T GetComponent<T>(this STUEntityDefinition w) where T : STUInstance {
            if (teStructuredData.Manager.InstancesInverted.TryGetValue(typeof(T), out uint hash)) {
                return w.m_componentMap[hash] as T;
            }
            return null;
        }
        #endregion
    }
}