using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CMFLib;
using LZ4;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    public class ApplicationPackageManifest {
        public static class Types {
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Header22 {
                public ulong Build;
                public ulong Unknown1;
                public uint Unknown2;
                public uint PackageCount;
                public uint Unknown3;
                public uint EntryCount;
                public uint Checksum;

                public Header Upgrade() {
                    return new Header {Build = Build, Checksum = Checksum, EntryCount = EntryCount, PackageCount = PackageCount};
                }
            }
            
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
                public ulong HashA;
                public ulong HashB;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Entry21 { // also v20, shh
                public uint Index;
                public ulong Hash;
                
                public Entry GetEntry() => new Entry { Index = Index, HashA = Hash };
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PackageEntry {
                public ulong PackageGUID; // 077 file
                public ulong Unknown1;
                public uint Unknown2;
                public uint Unknown3;
                public uint Unknown4;
                
                public uint Unknown5;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PackageEntry21 { // also v20, shh
                public ulong EntryPointGUID; // virtual most likely
                public ulong PrimaryGUID; // real
                public ulong SecondaryGUID; // real
                public ulong Key; // encryption
                public ulong PackageGUID; // 077 file
                public ulong Unknown1;
                public uint Unknown2;
                
                public PackageEntry GetPackage() => new PackageEntry { PackageGUID = PackageGUID };
            }

            public enum PackageCompressionMethod : uint {
                Uncompressed = 0,
                Gzip = 1
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
                public ulong BundleGUID; // 09C file
                public ulong Unknown4;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]  // size = 16
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
                public MD5Hash LoadHash;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct CachePackageRecord {
                public int Index;
                public ContentFlags Flags;
                public uint Offset;
                public uint Size;
            }
        }

        public string Name;
        public Types.Header Header;
        public Types.Entry[] Entries;
        public Types.PackageEntry[] PackageEntries;
        public Types.Package[] Packages;
        public Types.PackageRecord[][] Records;
        public ulong[][] PackageSiblings;
        public ConcurrentDictionary<ulong, Types.PackageRecord> FirstOccurence = new ConcurrentDictionary<ulong, Types.PackageRecord>();

        public ContentManifestFile CMF;
        public MD5Hash CMFHash;
        public string CMFName;

        public LocaleFlags Locale;
        
        // ReSharper disable once InconsistentNaming
        private const ulong APM_VERSION = 22;

        public void Load(string name, MD5Hash cmfhash, Stream stream, CASCHandler casc, string cmfname, LocaleFlags locale,
            ProgressReportSlave worker = null) {
            Locale = locale;
            Name = name;
            CMFHash = cmfhash;
            CMFName = Path.GetFileName(cmfname);
            
            //using (Stream file = File.OpenWrite(Path.GetFileName(name))) {
            //    stream.CopyTo(file);
            //    stream.Position = 0;
            //}

            if (!casc.EncodingHandler.GetEntry(cmfhash, out EncodingEntry cmfEncoding)) {
                return;
            }

            if (!casc.Config.LoadContentManifest) return;
            using (Stream cmfStream = casc.OpenFile(cmfEncoding.Key)) {
                CMF = new ContentManifestFile(CMFName, cmfStream, worker);
            }

            FirstOccurence = new ConcurrentDictionary<ulong, Types.PackageRecord>(Environment.ProcessorCount + 2, CMF.Map.Count);

            using (BinaryReader reader = new BinaryReader(stream)) {
                ulong build = reader.ReadUInt64();
                reader.BaseStream.Position = 0;
                
                if (CMFHeaderCommon.IsV22((uint)build)) {
                    Header = reader.Read<Types.Header22>().Upgrade();
                } else {
                    Header = reader.Read<Types.Header>();
                }

                if (CMFHeaderCommon.IsV22((uint)Header.Build)) {
                    Entries = reader.ReadArray<Types.Entry>((int)Header.EntryCount);
                    PackageEntries = reader.ReadArray<Types.PackageEntry>((int)Header.PackageCount);
                } else {
                    Entries = reader.ReadArray<Types.Entry21>((int)Header.EntryCount).Select(x => x.GetEntry()).ToArray();
                    PackageEntries = reader.ReadArray<Types.PackageEntry21>((int)Header.PackageCount).Select(x => x.GetPackage()).ToArray();
                }

                if (CASCHandler.Cache.CacheAPM && CacheFileExists(Header.Build)) {
                    worker?.ReportProgress(0, "Loading cached data...");
                    
                    try {
                        if (LoadCache(Header.Build)) {  // if cache is invalid, we'll regenerate
                            GatherFirstCMF(casc, worker);
                            return;
                        }
                    }
                    catch {
                        TankLib.Helpers.Logger.Error("CASC", $"Failed to load APM Cache {Path.GetFileName(CacheFile(Header.Build))}");
                        File.Delete(CacheFile(Header.Build));
                    }
                }

                Packages = new Types.Package[Header.PackageCount];
                Records = new Types.PackageRecord[Header.PackageCount][];
                PackageSiblings = new ulong[Header.PackageCount][];
                worker?.ReportProgress(0, $"Loading {Name} packages");
                try {
                    LoadPackages(casc, worker);
                } catch (AggregateException e) {
                    // return nice exception to RootHandler
                    if (e.InnerException != null) {
                        throw e.InnerException;
                    }
                }
            }
            if (!Console.IsOutputRedirected) {
                Console.Write(new string(' ', Console.WindowWidth-1)+"\r");
            }

            if (CASCHandler.Cache.CacheAPM) {
                TankLib.Helpers.Logger.Debug("CASC", $"Caching APM {name}");
                worker?.ReportProgress(0, "Caching data...");
                SaveCache(Header.Build);
            }

            GatherFirstCMF(casc, worker);
        }

        private void LoadPackages(CASCHandler casc, ProgressReportSlave worker) {
            int c = 0;
                Parallel.For(0, Header.PackageCount, new ParallelOptions {
                    MaxDegreeOfParallelism = CASCConfig.MaxThreads
                }, i => {
                    c++;
                    if (c % 1000 == 0) {
                        if (!Console.IsOutputRedirected) {
                            Console.Out.Write($"Loading packages: {System.Math.Floor(c / (float)Header.PackageCount * 10000) / 100:F0}% ({c}/{Header.PackageCount})\r");
                        }
                        
                        worker?.ReportProgress((int)((float)c / Header.PackageCount * 100));
                    }
                    Types.PackageEntry entry = PackageEntries[i];
                    if (!CMF.Map.ContainsKey(entry.PackageGUID)) {
                        return; // lol?
                    }

                    if (!casc.EncodingHandler.GetEntry(CMF.Map[entry.PackageGUID].HashKey, out EncodingEntry packageEncoding))
                        return;
                    using (Stream packageStream = casc.OpenFile(packageEncoding.Key))
                    using (BinaryReader packageReader = new BinaryReader(packageStream)) {
                        Packages[i] = packageReader.Read<Types.Package>();

                        if (CMFHeaderCommon.IsV22((uint)Header.Build)) {  // todo: hack
                            //Packages[i].SiblingCount *= 2;
                            Packages[i].SiblingCount = 0;
                        }
                        if (Packages[i].SiblingCount > 0) {
                            packageStream.Position = Packages[i].OffsetSiblings;
                            PackageSiblings[i] = packageReader.ReadArray<ulong>((int)Packages[i].SiblingCount);
                        } else {
                            PackageSiblings[i] = new ulong[0];
                        }

                        packageStream.Position = Packages[i].OffsetRecords;
                        Types.PackageRecordRaw[] recordsRaw;
                        using (GZipStream recordGunzipped = new GZipStream(packageStream, CompressionMode.Decompress))
                        using (BinaryReader recordReader = new BinaryReader(recordGunzipped)) {
                            recordsRaw = recordReader.ReadArray<Types.PackageRecordRaw>((int)Packages[i].RecordCount);
                            Records[i] = new Types.PackageRecord[Packages[i].RecordCount];
                        }

                        for (uint j = 0; j < Packages[i].RecordCount; ++j) {
                            Types.PackageRecordRaw rawRecord = recordsRaw[j];

                            ContentManifestFile.HashData recordCMF = CMF.Map[rawRecord.GUID];
                            Types.PackageRecord record = new Types.PackageRecord {
                                GUID = rawRecord.GUID,
                                Flags = rawRecord.Flags,
                                Offset = rawRecord.Offset
                            };
                            if (record.Flags.HasFlag(ContentFlags.Bundle)) {
                                record.LoadHash = CMF.Map[Packages[i].BundleGUID].HashKey;
                            } else {
                                if (CMF.Map.ContainsKey(record.GUID)) {
                                    record.LoadHash = recordCMF.HashKey;
                                }
                            }
                            record.Size = recordCMF.Size;
                            if (!FirstOccurence.ContainsKey(record.GUID)) {
                                FirstOccurence[record.GUID] = record;
                            }
                            Records[i][j] = record;
                        }
                    }
                });
        }

        private bool CacheFileExists(ulong build) => File.Exists(CacheFile(build));

        private string CacheFile(ulong build) =>
            Path.Combine(CASCHandler.Cache.APMCachePath, $"{build}_{Path.GetFileNameWithoutExtension(Name)}.apmcached");

        private void SaveCache(ulong build) {
            if (CacheFileExists(build)) {
                return;
            }

            using (Stream file = File.OpenWrite(CacheFile(build)))
            using (LZ4Stream lz4Stream = new LZ4Stream(file, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression))
            using (BinaryWriter writer = new BinaryWriter(lz4Stream)) {
                writer.Write(APM_VERSION);
                writer.WriteStructArray(Packages);
                for (int i = 0; i < PackageEntries.Length; ++i) {
                    writer.Write(Records[i].Length);
                    
                    Types.CachePackageRecord[] cacheRecords = new Types.CachePackageRecord[Records[i].Length];
                    for (int j = 0; j < Records[i].Length; j++) {
                        Types.PackageRecord record = Records[i][j];
                        cacheRecords[j] = new Types.CachePackageRecord {
                            Index = CMF.IndexMap[record.GUID],
                            Flags = record.Flags,
                            Offset = record.Offset,
                            Size = record.Size
                        };
                    }
                    writer.WriteStructArray(cacheRecords);
                    
                    writer.Write(PackageSiblings[i].Length);
                    writer.WriteStructArray(PackageSiblings[i]);
                }
            }
        }

        private bool LoadCache(ulong build) {
            if (!CacheFileExists(build)) {
                return false;
            }

            using (Stream file = File.OpenRead(CacheFile(build)))
            using (LZ4Stream lz4Stream = new LZ4Stream(file, LZ4StreamMode.Decompress))
            using (BinaryReader reader = new BinaryReader(lz4Stream)) {
                if(reader.ReadUInt64() != APM_VERSION) {
                    return false;
                }

                int packageEntryCount = PackageEntries.Length;
                Packages = reader.ReadArray<Types.Package>(packageEntryCount);

                Records = new Types.PackageRecord[packageEntryCount][];
                PackageSiblings = new ulong[packageEntryCount][];
                
                Types.CachePackageRecord[][] cacheRecords = new Types.CachePackageRecord[packageEntryCount][];

                for (int i = 0; i < packageEntryCount; i++) {
                    int recordCount = reader.ReadInt32();
                    cacheRecords[i] = reader.ReadArray<Types.CachePackageRecord>(recordCount);
                    int siblingCount = reader.ReadInt32();
                    PackageSiblings[i] = reader.ReadArray<ulong>(siblingCount);
                }

                Parallel.For(0, packageEntryCount, new ParallelOptions { MaxDegreeOfParallelism = CASCConfig.MaxThreads }, i =>
                {
                    Types.CachePackageRecord[] cache = cacheRecords[i];
                    Records[i] = new Types.PackageRecord[cache.Length];

                    Types.Package package = Packages[i];
                    MD5Hash bundleLoadHash;
                    if (package.BundleGUID != 0) {
                        bundleLoadHash = CMF.Map[package.BundleGUID].HashKey;
                    }

                    for (int j = 0; j < cache.Length; j++) {
                        Types.CachePackageRecord cacheRecord = cache[j];
                        ContentManifestFile.HashData cmfRecord = CMF.HashList[cacheRecord.Index];
                        Types.PackageRecord record = new Types.PackageRecord {
                            GUID = cmfRecord.GUID,
                            Size = cacheRecord.Size,
                            Flags = cacheRecord.Flags,
                            Offset = cacheRecord.Offset
                        };
                        if ((record.Flags & ContentFlags.Bundle) != 0) {
                            record.LoadHash = bundleLoadHash;
                        } else {
                            record.LoadHash = cmfRecord.HashKey;
                        }
                        if (!FirstOccurence.ContainsKey(record.GUID)) {
                            FirstOccurence.TryAdd(record.GUID, record);
                        }
                        Records[i][j] = record;
                    }
                });
            }

            return true;
        }

        public static bool SaneChecking = true;

        private void GatherFirstCMF(CASCHandler casc, ProgressReportSlave worker = null) {
            worker?.ReportProgress(0, "Rebuilding occurence list...");
            int c = 0;
            ContentManifestFile.HashData[] data = CMF.Map.Values.ToArray();
            Parallel.For(0, data.Length, new ParallelOptions {
                MaxDegreeOfParallelism = CASCConfig.MaxThreads
            }, i => {
                c++;
                if (worker != null && c % 500 == 0) {
                    worker.ReportProgress((int)((float)c / CMF.Map.Count * 100));
                }
                if (FirstOccurence.ContainsKey(data[i].GUID)) return;
                if (SaneChecking && !casc.EncodingHandler.HasEntry(data[i].HashKey)) return;
                FirstOccurence.TryAdd(data[i].GUID, new Types.PackageRecord {
                    GUID = data[i].GUID,
                    LoadHash = data[i].HashKey,
                    Size = data[i].Size,
                    Offset = 0,
                    Flags = 0
                });
            });
        }
    }
}
