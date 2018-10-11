using System;
using System.Runtime.Serialization;

namespace TankLib.STU {
    /// <summary>Asset reference</summary>
    public class teStructuredDataAssetRef<T> : ISerializable_STU {
        [IgnoreDataMember]
        public ulong Padding;
        public teResourceGUID GUID;

        public Type GetRefType() {
            Type type = typeof(T);
            return type == typeof(ulong) ? null : type;
        }

        public static implicit operator teResourceGUID(teStructuredDataAssetRef<T> assetRef) {
            if (assetRef == null) return new teResourceGUID();
            return assetRef.GUID;
        }
        
        public static implicit operator ulong(teStructuredDataAssetRef<T> assetRef) {
            if (assetRef == null) return 0;
            return assetRef.GUID.GUID;
        }

        public void Deserialize(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V2) {
                Padding = 0xFFFFFFFFFFFFFFFF; // no padding out of array
                ulong guid = data.Data.ReadUInt64();
                Deobfuscate(data.HeaderChecksum, field.Hash, guid);
            } else if (data.Format == teStructuredDataFormat.V1) {
                Padding = data.Data.ReadUInt64();
                ulong guid = data.Data.ReadUInt64();
                GUID = (teResourceGUID) guid;
            }
        }

        public void Deserialize_Array(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V2) {
                Padding = data.DynData.ReadUInt64();
                ulong guid = data.DynData.ReadUInt64();
                Deobfuscate(data.HeaderChecksum, field.Hash, guid);
            } else if (data.Format == teStructuredDataFormat.V1) {
                Padding = data.Data.ReadUInt64();
                ulong guid = data.Data.ReadUInt64();
                GUID = (teResourceGUID) guid;
            }
        }
        
        private void Deobfuscate(ulong headerChecksum, uint fieldHash, ulong guid) {
            ulong fieldHash64 = fieldHash;
            fieldHash64 |= fieldHash64 << 32;
            guid ^= fieldHash64 ^ headerChecksum;
            guid = guid.SwapBytes(0, 3).SwapBytes(7, 1).SwapBytes(2, 6).SwapBytes(4, 5);
            
            GUID = new teResourceGUID(guid);
        }

        public override string ToString() {
            return GUID.ToString();
        }
    }
}