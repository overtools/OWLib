using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    public class ApplicationPackageManifest {
        public static class Types {
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Header {
                public ulong Build;
                public ulong Unknown1;
                public uint PackageCount;
                public uint Unknown2;
                public uint EntryCount;
                public uint Checksum;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Entry {
                public uint Index;
                public ulong Hash;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PackageEntry {
                public ulong EntryPointGUID; // virtual most likely
                public ulong PrimaryGUID; // real
                public ulong SecondaryGUID; // real
                public ulong Key; // encryption
                public ulong PackageGUID; // 077 file
                public ulong Unknown1;
                public uint Unknown2;
            }

            public enum PackageCompressionMethod : uint {
                Uncompressed = 0,
                Gzip = 1,
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Package {
                public long OffsetRecords;
                public long OffsetSiblings;
                public long OffsetUnknown;
                public long OffsetSiblings2;
                public ulong Unknown1;
                public uint Unknown2;
                public uint RecordCount;
                public PackageCompressionMethod CompressionMethod;
                public ulong SizeRecords;
                public uint SiblingCount;
                public uint Checksum;
                public uint Unknown3;
                public ulong BundleGUID; // 095 file
                public ulong Unknown4;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PackageRecordRaw {
                public ulong GUID;
                public ContentFlags Flags;
                public uint Offset;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PackageRecord {
                public ulong GUID;
                public ContentFlags Flags;
                public uint Offset;
                public uint Size;
                public MD5Hash Hash;
            }
        }

        public string Name { get; }

        public Types.Header Header;
        public Types.Entry[] Entries;
        public Types.PackageEntry[] PackageEntries;
        public Types.Package[] Packages;
        public Types.PackageRecord[][] Records;
        public ulong[][] PackageSiblings;
        public Dictionary<ulong, Types.PackageRecord> FirstOccurence = new Dictionary<ulong, Types.PackageRecord>();

        public ContentManifestFile CMF;

        public LocaleFlags Locale;

        public ApplicationPackageManifest(string name, MD5Hash cmfhash, Stream stream, CASCHandler casc, string cmfname,
            BackgroundWorkerEx worker = null) {
            Name = name;

            EncodingEntry cmfEncoding;
            if (!casc.EncodingHandler.GetEntry(cmfhash, out cmfEncoding)) {
                return;
            }

            using (Stream cmfStream = casc.OpenFile(cmfEncoding.Key)) {
                CMF = new ContentManifestFile(cmfname, cmfStream, worker);

                using (BinaryReader reader = new BinaryReader(stream)) {
                    Header = reader.Read<Types.Header>();

                    if (CASCHandler.Cache.CacheAPM && CacheFileExists(Header.Build)) {
                        LoadCache(Header.Build);
                        GatherFirstCMF(casc);
                        return;
                    }

                    Entries = reader.ReadArray<Types.Entry>((int) Header.EntryCount);
                    PackageEntries = reader.ReadArray<Types.PackageEntry>((int) Header.PackageCount);

                    Packages = new Types.Package[Header.PackageCount];
                    Records = new Types.PackageRecord[Header.PackageCount][];
                    PackageSiblings = new ulong[Header.PackageCount][];

                    for (uint i = 0; i < Header.PackageCount; ++i) {
                        Types.PackageEntry entry = PackageEntries[i];
                        if (!CMF.Map.ContainsKey(entry.PackageGUID)) {
                            continue; // lol?
                        }

                        EncodingEntry packageEncoding;
                        if (!casc.EncodingHandler.GetEntry(CMF.Map[entry.PackageGUID].HashKey, out packageEncoding))
                            continue;
                        using (Stream packageStream = casc.OpenFile(packageEncoding.Key))
                        using (BinaryReader packageReader = new BinaryReader(packageStream)) {
                            Packages[i] = packageReader.Read<Types.Package>();

                            if (Packages[i].SiblingCount > 0) {
                                packageStream.Position = Packages[i].OffsetSiblings;
                                PackageSiblings[i] = packageReader.ReadArray<ulong>((int) Packages[i].SiblingCount);
                            } else {
                                PackageSiblings[i] = new ulong[0];
                            }

                            packageStream.Position = Packages[i].OffsetRecords;
                            Types.PackageRecordRaw[] recordsRaw;
                            using (GZipStream recordGunzipped = new GZipStream(packageStream, CompressionMode.Decompress))
                            using (BinaryReader recordReader = new BinaryReader(recordGunzipped)) {
                                recordsRaw = recordReader.ReadArray<Types.PackageRecordRaw>((int) Packages[i].RecordCount);
                                Records[i] = new Types.PackageRecord[Packages[i].RecordCount];
                            }

                            for (uint j = 0; j < Packages[i].RecordCount; ++j) {
                                Types.PackageRecordRaw rawRecord = recordsRaw[j];
                                Types.PackageRecord record = new Types.PackageRecord {
                                    GUID = rawRecord.GUID,
                                    Flags = rawRecord.Flags,
                                    Offset = rawRecord.Offset,
                                    Hash = default(MD5Hash)
                                };
                                ContentManifestFile.HashData recordCMF = CMF.Map[record.GUID];
                                if (record.Flags.HasFlag(ContentFlags.Bundle)) {
                                    record.Hash = CMF.Map[Packages[i].BundleGUID].HashKey;
                                } else {
                                    if (CMF.Map.ContainsKey(record.GUID)) {
                                        record.Hash = recordCMF.HashKey;
                                    }
                                }

                                record.Size = recordCMF.Size;
                                Records[i][j] = record;

                                if (!FirstOccurence.ContainsKey(record.GUID)) {
                                    FirstOccurence[record.GUID] = record;
                                }
                            }
                        }
                    }
                }

                if (CASCHandler.Cache.CacheAPM) {
                    SaveCache(Header.Build);
                }

                GatherFirstCMF(casc);
            }
        }

        private bool CacheFileExists(ulong build) => File.Exists(CacheFile(build));

        private string CacheFile(ulong build) =>
            Path.Combine(CASCHandler.Cache.APMCachePath, $"{build}_{Path.GetFileNameWithoutExtension(Name)}.apmcached");

        private void SaveCache(ulong build) {
            if (CacheFileExists(build)) {
                return;
            }

            using (Stream file = File.OpenWrite(CacheFile(build)))
            using (BinaryWriter writer = new BinaryWriter(file)) {
                writer.Write(1UL);
                writer.Write(Entries.Length);
                writer.WriteStructArray(Entries);
                writer.Write(PackageEntries.Length);
                writer.WriteStructArray(PackageEntries);
                writer.WriteStructArray(Packages);
                for (int i = 0; i < PackageEntries.Length; ++i) {
                    writer.Write(Records[i].Length);
                    writer.WriteStructArray(Records[i]);
                    writer.Write(PackageSiblings[i].Length);
                    writer.WriteStructArray(PackageSiblings[i]);
                }
            }
        }

        private void LoadCache(ulong build) {
            if (!CacheFileExists(build)) {
                return;
            }

            using (Stream file = File.OpenRead(CacheFile(build)))
            using (BinaryReader reader = new BinaryReader(file)) {
                if (reader.ReadUInt64() > 1) {
                    return;
                }

                int entryCount = reader.ReadInt32();
                Entries = reader.ReadArray<Types.Entry>(entryCount);
                int packageEntryCount = reader.ReadInt32();
                PackageEntries = reader.ReadArray<Types.PackageEntry>(packageEntryCount);
                Packages = reader.ReadArray<Types.Package>(packageEntryCount);

                Records = new Types.PackageRecord[packageEntryCount][];
                PackageSiblings = new ulong[packageEntryCount][];

                for (int i = 0; i < packageEntryCount; ++i) {
                    int recordCount = reader.ReadInt32();
                    Records[i] = reader.ReadArray<Types.PackageRecord>(recordCount);
                    int siblingCount = reader.ReadInt32();
                    PackageSiblings[i] = reader.ReadArray<ulong>(siblingCount);

                    foreach (Types.PackageRecord record in Records[i]) {
                        if (!FirstOccurence.ContainsKey(record.GUID)) {
                            FirstOccurence[record.GUID] = record;
                        }
                    }
                }
            }
        }

        private void GatherFirstCMF(CASCHandler casc) {
            foreach (KeyValuePair<ulong, ContentManifestFile.HashData> pair in CMF.Map) {
                if (FirstOccurence.ContainsKey(pair.Key)) continue;
                EncodingEntry info = new EncodingEntry();
                if (casc.EncodingHandler.GetEntry(pair.Value.HashKey, out info)) {
                    FirstOccurence[pair.Key] = new Types.PackageRecord {
                        Flags = 0,
                        GUID = pair.Key,
                        Hash = pair.Value.HashKey,
                        Offset = 0,
                        Size = (uint) info.Size
                    };
                }
            }
        }
    }
}