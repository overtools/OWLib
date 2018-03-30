using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TankLib.Helpers;

namespace TankLib.STU {
    #region V2
    public sealed class STUBag<T> : List<T>, ISerializable where T : ISerializable, new() {
        public void Deserialize(BinaryReader reader)  {
            int size = reader.ReadInt32();
            int offset = reader.ReadInt32();
            long oldPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = offset;
            Capacity = size;

            for (int i = 0; i != size; ++i) {
                SerializableHelper.Deserialize(reader, out T val);
                Add(val);
            }
            reader.BaseStream.Position = oldPosition;
        }

        public void Serialize(BinaryWriter writer) {
            throw new System.NotImplementedException();
        }
    }
    
    public class STUField_Info : ISerializable {
        public uint Hash;
        public int Size;

        public void Deserialize(BinaryReader reader) {
            Hash = reader.ReadUInt32();
            Size = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer) {
            throw new System.NotImplementedException();
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUInstance_Info : ISerializable {
        public uint Hash;
        public uint EmbedderFieldHash; // Help me find a better name?
        public int EmbedderInstanceIdx; // Help me find a better name?
        public int Size;

        public void Deserialize(BinaryReader reader) {
            Hash = reader.ReadUInt32();
            EmbedderFieldHash = reader.ReadUInt32();
            EmbedderInstanceIdx = reader.ReadInt32();
            Size = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer) {
            throw new System.NotImplementedException();
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUInlineArray_Info : ISerializable {
        public uint TypeHash;
        public int ArrayCount; // Size of the array that references this type.

        public void Deserialize(BinaryReader reader) {
            TypeHash = reader.ReadUInt32();
            ArrayCount = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer) {
            throw new System.NotImplementedException();
        }
    }
    #endregion

    #region V1
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUHeaderV1 {
        public uint Magic;
        public uint InstanceCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUInstanceRecordV1 {
        public int Offset;
        public uint Flags;
    }
    #endregion
}