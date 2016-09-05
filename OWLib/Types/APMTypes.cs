using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMHeader {
        public ulong buildVersion;
        public uint buildNumber;
        public uint packageCount;
        public uint entryCount;
        public uint unk1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMEntry {
        public uint Index;
        public uint hashA;
        public uint hashB;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMPackage {
        public ulong localKey;
        public ulong primaryKey;
        public ulong externalKey;
        public ulong encryptionKeyHash;
        public ulong packageKey;
        public uint unk_0;
        public uint unk_1;
        public uint unk_2;
        public uint unk_3;
        public MD5Hash indexContentKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndex {
        public long recordsOffset;
        public ulong unkOffset_0;
        public long depsOffset;
        public ulong unkOffset_1;
        public uint unk_0;
        public uint numRecords;
        public int recordsSize;
        public uint unk_1;
        public uint numDeps;
        public uint totalSize;
        public ulong bundleKey;
        public uint bundleSize;
        public ulong unk_2;
        public MD5Hash bundleContentKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndexRecord {
        public ulong Key;
        public int Size;
        public uint Flags;
        public uint Offset;
        public MD5Hash ContentKey;
    }
}
