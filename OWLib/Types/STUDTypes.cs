using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUDHeader {
        public uint magic;
        public uint version;
        public ulong instanceTableOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUDInstanceRecord {
        public uint offset;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUDInstanceInfo {
        public uint localId;
        public uint nextInstance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUDArrayInfo {
        public ulong count;
        public ulong offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct STUDReferenceArrayInfo {
        public ulong count;
        public ulong indiceOffset;
        public ulong referenceOffset;
    }

    public interface ISTUDInstance {
        string Name
        {
            get;
        }

        uint Id
        {
            get;
        }

        ulong Key
        {
            get;
        }

        void Read(Stream input);
    }
}
