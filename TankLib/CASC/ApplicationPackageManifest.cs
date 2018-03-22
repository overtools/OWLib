using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMEntry {
        public uint Index;
        public uint hashA;
        public uint hashB;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFEntry {
        public uint Index;
        public uint hashA;
        public uint hashB;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHashData {
        public ulong id;
        public uint flags;
        public MD5Hash HashKey;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndex {
        public long recordsOffset; // Offset to GZIP compressed records chunk, read (recordsSize + numRecords) bytes here

        public long unkOffset_0;
        public long unkOffset_1;
        public long depsOffset; // Offset to dependencies chunk, read numDeps * uint here
        public ulong unk_0;
        public uint numUnk_0;
        public uint numRecords;
        public uint numUnk_1;
        public uint recordsSize;
        public uint unk_1;
        public uint numDeps;
        public ulong unk_2;
        public teResourceGUID bundleKey; // Requires some sorcery, see Key
        public ulong unk_3;
        public MD5Hash bundleContentKey;

        public PackageIndex(PackageIndexItem package) {
            recordsOffset = package.recordsOffset;
            unkOffset_0 = package.unkOffset_0;
            unkOffset_1 = package.unkOffset_1;
            depsOffset = package.depsOffset;
            unk_0 = package.unk_0;
            numRecords = package.numRecords;
            numUnk_0 = package.numUnk_0;
            numUnk_1 = package.numUnk_1;
            recordsSize = package.recordsSize;
            unk_1 = package.unk_1;
            numDeps = package.numDeps;
            unk_2 = package.unk_2;
            bundleKey = package.bundleKey;
            unk_3 = package.unk_3;
        }

        public static explicit operator PackageIndex(PackageIndexItem package) {
            PackageIndex a = new PackageIndex(package);
            return a;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndexItem {
        public long recordsOffset;
        public long unkOffset_0;
        public long unkOffset_1;
        public long depsOffset;
        public ulong unk_0;
        public uint numUnk_0;
        public uint numRecords;
        public uint numUnk_1;
        public uint recordsSize;
        public uint unk_1;
        public uint numDeps;
        public ulong unk_2;
        public teResourceGUID bundleKey;
        public ulong unk_3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndexRecord {
        public teResourceGUID Key; // Requires some sorcery, see Key
        public uint Flags; // Flags. Has 0x40000000 when in bundle, otherwise in encoding
        public uint Offset; // Offset into bundle
        public int Size;
        public MD5Hash ContentKey; // If it doesn't have the above flag (0x40000000) look it up in encoding

        public PackageIndexRecord(PackageIndexRecordItem record) {
            Key = record.Key;
            Flags = record.Flags;
            Offset = record.Offset;
            Size = 0;
        }

        public static explicit operator PackageIndexRecord(PackageIndexRecordItem record) {
            PackageIndexRecord a = new PackageIndexRecord(record);
            return a;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndexRecordItem {
        public teResourceGUID Key; // Requires some sorcery, see Key
        public uint Flags; // Flags. Has 0x40000000 when in bundle, otherwise in encoding
        public uint Offset; // Offset into bundle
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class APMPackage {
        public teResourceGUID localKey;
        public teResourceGUID primaryKey;
        public teResourceGUID externalKey;
        public ulong encryptionKeyHash;
        public teResourceGUID packageKey;
        public uint unk_0;
        public uint unk_1; // size?
        public uint unk_2;
        public MD5Hash indexContentKey;

        public APMPackage(APMPackageItem package) {
            localKey = package.localKey;
            primaryKey = package.primaryKey;
            externalKey = package.externalKey;
            encryptionKeyHash = package.encryptionKeyHash;
            packageKey = package.packageKey;
            unk_0 = package.unk_0;
            unk_1 = package.unk_1;
            unk_2 = package.unk_2;
        }

        public static explicit operator APMPackage(APMPackageItem package) {
            APMPackage a = new APMPackage(package);
            return a;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMPackageItem {
        public teResourceGUID localKey;
        public teResourceGUID primaryKey;
        public teResourceGUID externalKey;
        public ulong encryptionKeyHash;
        public teResourceGUID packageKey;
        public uint unk_0;
        public uint unk_1; // size?
        public uint unk_2;
    }
    
    /// <summary>APM file</summary>
    public class ApplicationPackageManifest {
        public interface IAPMHeader {
            uint GetPackageCount();
            void SetPackageCount(uint value);
            uint GetEntryCount();
            ulong GetBuildVersion();
            uint GetBuildNumber();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct APMHeader39028 : IAPMHeader {
            public ulong BuildVersion;
            public ulong Unknown1; // zero
            public uint BuildNumber;
            public uint PackageCount; // zero;
            public uint EntryCount;
            public uint Checksum;

            public void SetPackageCount(uint value) {
                PackageCount = value;
            }

            public uint GetPackageCount() => PackageCount;
            public uint GetEntryCount() => EntryCount;
            public ulong GetBuildVersion() => BuildVersion;
            public uint GetBuildNumber() => BuildNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct APMHeader17 : IAPMHeader {
            public ulong BuildVersion;
            public uint BuildNumber;
            public uint PackageCount; // zero;
            public uint EntryCount;
            public uint Checksum;

            public void SetPackageCount(uint value) {
                PackageCount = value;
            }

            public uint GetPackageCount() => PackageCount;
            public uint GetEntryCount() => EntryCount;
            public ulong GetBuildVersion() => BuildVersion;
            public uint GetBuildNumber() => BuildNumber;
        }

        public readonly IAPMHeader Header;
        public readonly APMPackage[] Packages;
        public readonly APMEntry[] Entries;
        public readonly PackageIndex[] Indexes;
        public readonly PackageIndexRecord[][] Records;
        public readonly ContentManifestFile CMF;

        public LocaleFlags Locale;
        public string Name;
        
        public ApplicationPackageManifest(string name, MD5Hash cmfhash, Stream stream, CASCHandler casc,
            LocaleFlags locale, string cmfname, BackgroundWorkerEx worker = null) {
            Locale = locale;
            Name = name;
            
            using (BinaryReader reader = new BinaryReader(stream)) {
                //using (Stream file = File.OpenWrite(Path.GetFileName(name))) {
                //    stream.CopyTo(file);
                //    stream.Position = 0;
                //}
                
                ulong version = reader.ReadUInt64();
                stream.Position -= 8;
                if (version >= 39028) {
                    Header = reader.Read<APMHeader39028>();
                }
                else {
                    Header = reader.Read<APMHeader17>();
                }

                Entries = new APMEntry[Header.GetEntryCount()];
                for (int j = 0; j < Entries.Length; j++) {
                    Entries[j] = reader.Read<APMEntry>();
                }
                Header.SetPackageCount((uint) ((stream.Length - stream.Position) / Marshal.SizeOf(typeof(APMPackage))));

                if (!casc.EncodingHandler.GetEntry(cmfhash, out EncodingEntry cmfEnc)) {
                    return;
                }

                if (casc.Config.LoadContentManifest) {
                    using (Stream cmfStream = casc.OpenFile(cmfEnc.Key)) {
                        CMF = new ContentManifestFile(cmfname, cmfStream, worker);
                    }  
                }

                uint[][] dependencies;
                //if (!RootHandler.LoadPackages) {
                //    Packages = new APMPackage[0];
                //    Indexes = new PackageIndex[0];
                //    Records = new PackageIndexRecord[0][];
                //    //dependencies = new uint[0][];
                //    return;
                //}

                Packages = new APMPackage[Header.GetPackageCount()];
                Indexes = new PackageIndex[Header.GetPackageCount()];
                Records = new PackageIndexRecord[Header.GetPackageCount()][];
                dependencies = new uint[Header.GetPackageCount()][];
                
                return;

                for (int j = 0; j < Packages.Length; j++) {
                    Packages[j] = new APMPackage(reader.Read<APMPackageItem>());
                    Packages[j].indexContentKey = CMF.Map[(ulong)Packages[j].packageKey].HashKey;

                    if (!casc.EncodingHandler.GetEntry(Packages[j].indexContentKey, out EncodingEntry pkgIndexEnc)) {
                        continue;
                    }

                    using (Stream pkgIndexStream = casc.OpenFile(pkgIndexEnc.Key))
                    using (BinaryReader pkgIndexReader = new BinaryReader(pkgIndexStream)) {
#if OUTPUT_PKG // Write out Package Index files
                        string pkgfilename =
string.Format("./Packages/{0}/{1:X}.pkgindx", name, packages[j].packageKey);
                        string pkgPathname = pkgfilename.Substring(0, pkgfilename.LastIndexOf('/'));
                        Directory.CreateDirectory(pkgPathname);
                        Stream pkgWriter = File.Create(pkgfilename);
                        pkgIndexStream.CopyTo(pkgWriter);
                        pkgWriter.Close();
                        pkgIndexStream.Seek(0, SeekOrigin.Begin);
#endif

                        Indexes[j] = new PackageIndex(pkgIndexReader.Read<PackageIndexItem>());
                        try {
                            Indexes[j].bundleContentKey = CMF.Map[(ulong)Indexes[j].bundleKey].HashKey;
                        }
                        catch {
                            // ignored
                        }
                        pkgIndexStream.Position = Indexes[j].recordsOffset;

                        using (GZipStream recordsStream =
                            new GZipStream(pkgIndexStream, CompressionMode.Decompress, true)) {
                            using (BinaryReader recordsReader = new BinaryReader(recordsStream)) {
                                PackageIndexRecord[] recs = new PackageIndexRecord[Indexes[j].numRecords];

                                for (int k = 0; k < recs.Length; k++) {
                                    recs[k] = new PackageIndexRecord(recordsReader.Read<PackageIndexRecordItem>());
                                    bool shouldntEnc = (recs[k].Flags & 0x40000000) != 0;
                                    recs[k].ContentKey = CMF.Map[(ulong)recs[k].Key].HashKey;
                                    if (casc.EncodingHandler.GetEntry(recs[k].ContentKey, out EncodingEntry encInfo)) {
                                        recs[k].Size =
                                            encInfo
                                                .Size; // WHY DOES THIS WORK? ARE BUNDLED FILES NOT ACTUALLY IN A BUNDLE ANYMORE?
                                        if (shouldntEnc) {
                                            
                                        }
                                    }
                                }
                                Records[j] = recs;
                            }
                        }

                        pkgIndexStream.Position = Indexes[j].depsOffset;

                        uint[] deps = new uint[Indexes[j].numDeps];

                        for (int k = 0; k < deps.Length; k++)
                            deps[k] = pkgIndexReader.ReadUInt32();

                        dependencies[j] = deps;
                    }
                }
            }
        }
    }
}