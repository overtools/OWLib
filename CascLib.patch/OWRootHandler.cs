using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CASCExplorer
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMEntry
    {
        public uint Index;
        public uint hashA;
        public uint hashB;
    }

    public struct CMFEntry {
        public uint Index;
        public uint hashA;
        public uint hashB;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct APMPackageItem
    {
        public ulong localKey;
        public ulong primaryKey;
        public ulong externalKey;
        public ulong encryptionKeyHash;
        public ulong packageKey;
        public uint unk_0;
        public uint unk_1; // size?
        public uint unk_2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class APMPackage {
        public ulong localKey;
        public ulong primaryKey;
        public ulong externalKey;
        public ulong encryptionKeyHash;
        public ulong packageKey;
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
    public struct CMFHashData
    {
        public ulong id;
        public uint flags;
        public MD5Hash HashKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndex
    {
        public long recordsOffset;                  // Offset to GZIP compressed records chunk, read (recordsSize + numRecords) bytes here
        public ulong unkOffset_0;
		public ulong unk_1300_0;
        public long depsOffset;                     // Offset to dependencies chunk, read numDeps * uint here
        public ulong unkOffset_1;
        public uint unk_0;
        public uint numRecords;
		public uint unk_1300_2;
        public int recordsSize;
        public uint unk_1;
        public uint numDeps;
        public uint totalSize;
		public uint unk_1300_3;
        public ulong bundleKey;                     // Requires some sorcery, see Key
        public uint bundleSize;
        public ulong unk_2;
        public MD5Hash bundleContentKey;            // Look this up in encoding
        //PackageIndexRecord[numRecords] records;   // See recordsOffset and PackageIndexRecord
        //u32[numDeps] dependencies;                // See depsOffset
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageIndexRecord
    {
        public ulong Key;               // Requires some sorcery, see Key
        public int Size;                // Size of asset
        public uint Flags;              // Flags. Has 0x40000000 when in bundle, otherwise in encoding
        public uint Offset;             // Offset into bundle
        public MD5Hash ContentKey;      // If it doesn't have the above flag (0x40000000) look it up in encoding
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OWRootEntry
    {
        public RootEntry baseEntry;
        public PackageIndex pkgIndex;
        public PackageIndexRecord pkgIndexRec;
    }

    public class OwRootHandler : RootHandlerBase
    {
        private readonly Dictionary<ulong, OWRootEntry> _rootData = new Dictionary<ulong, OWRootEntry>();

        private readonly List<APMFile> apmFiles = new List<APMFile>();

        public override int Count => _rootData.Count;
        public IReadOnlyList<APMFile> APMFiles => apmFiles;

        public string[] APMList;
        public static string LanguageScan = "enUS";

        public OwRootHandler(BinaryReader stream, BackgroundWorkerEx worker, CASCHandler casc)
        {
            worker?.ReportProgress(0, "Loading \"root\"...");

            string str = Encoding.ASCII.GetString(stream.ReadBytes((int)stream.BaseStream.Length));

            string[] array = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, MD5Hash> CMFHashes = new Dictionary<string, MD5Hash>();
            List<string> APMNames = new List<string>();
            for (int i = 1; i < array.Length; i++) {
                string[] filedata = array[i].Split('|');
                string name = filedata[4];

                if (Path.GetExtension(name) == ".cmf" && name.Contains("RDEV")) {
                    MD5Hash cmfMD5 = filedata[0].ToByteArray().ToMD5();
                    EncodingEntry apmEnc;
                    if (!name.Contains("L" + LanguageScan)) {
                        continue;
                    }

                    if (!casc.Encoding.GetEntry(cmfMD5, out apmEnc)) {
                        Console.Out.WriteLine("Failed to GetEntry: {0}", cmfMD5.ToHexString());
                        continue;
                    }
					// Export CMF files for hex viewing
					/*
                    using (Stream apmStream = casc.OpenFile(apmEnc.Key)) {
                        long start = apmStream.Position;
                        string Filename = string.Format("./APMFiles/{0}", name);
                        string Pathname = Filename.Substring(0, Filename.LastIndexOf('/'));
                        Directory.CreateDirectory(Pathname);
                        Stream APMWriter = File.Create(Filename);
                        apmStream.CopyTo(APMWriter);
                        APMWriter.Close();
                    }
					*/
                    CMFHashes.Add(name, cmfMD5);
                    // Console.Out.WriteLine("Adding {0} | {1:X} to CMFHashes", name, cmfMD5.ToHexString());
                }
            }
            
            for (int i = 1; i < array.Length; i++)
            {
                string[] filedata = array[i].Split('|');
                Console.Out.WriteLine("Array[{0}]: {1}", i, array[i]);
                string name = filedata[4];

                if (Path.GetExtension(name) == ".apm" && name.Contains("RDEV")) {
                    APMNames.Add(Path.GetFileNameWithoutExtension(name));
                    if (!name.Contains("L" + LanguageScan)) {
                        continue;
                    }
                    // add apm file for dev purposes
                    ulong apmNameHash = Hasher.ComputeHash(name);
                    MD5Hash apmMD5 = filedata[0].ToByteArray().ToMD5();
                    // Console.Out.WriteLine("apmNameHash: {0}, apmMD5: {1}", apmNameHash, apmMD5.ToHexString());
                    _rootData[apmNameHash] = new OWRootEntry() {
                        baseEntry = new RootEntry() { MD5 = apmMD5, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None }
                    };

                    CASCFile.FileNames[apmNameHash] = name;
                    // Console.Out.WriteLine("name: {0}", name);

                    EncodingEntry apmEnc;

                    if (!casc.Encoding.GetEntry(apmMD5, out apmEnc)) {
                        Console.Out.WriteLine("Failed to GetEntry: {0}", apmMD5.ToHexString());
                        continue;
                    }

                    MD5Hash cmf;
                    string cmfname = string.Format("{0}/{1}.cmf", Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name));
                    // Console.Out.WriteLine("CMF File Name: {0}", cmfname);
                    if (CMFHashes.ContainsKey(cmfname)==true)
                    {
                        CMFHashes.TryGetValue(cmfname, out cmf);
                        // Console.Out.WriteLine("CMF Hash Value: {0:X}", cmf.ToHexString());
                    }

                   //  Console.Out.WriteLine("Sucessfully Got Entry.\napmEnc.key: {0}", apmEnc.Key.ToHexString());
                    using (Stream apmStream = casc.OpenFile(apmEnc.Key))
                    {
                        apmFiles.Add(new APMFile(name, cmf, apmStream, casc));
                    }
                }

                worker?.ReportProgress((int)(i / (array.Length / 100f)));
            }
            APMList = APMNames.ToArray();
            APMNames.Clear();
        }

        static ulong keyToTypeID(ulong key)
        {
            var num = (key >> 48);
            num = (((num >> 1) & 0x55555555) | ((num & 0x55555555) << 1));
            num = (((num >> 2) & 0x33333333) | ((num & 0x33333333) << 2));
            num = (((num >> 4) & 0x0F0F0F0F) | ((num & 0x0F0F0F0F) << 4));
            num = (((num >> 8) & 0x00FF00FF) | ((num & 0x00FF00FF) << 8));
            num = ((num >> 16) | (num << 16));
            num >>= 20;
            return num + 1;
        }

        public override IEnumerable<KeyValuePair<ulong, RootEntry>> GetAllEntries()
        {
            foreach (var entry in _rootData)
                yield return new KeyValuePair<ulong, RootEntry>(entry.Key, entry.Value.baseEntry);
        }

        public override IEnumerable<RootEntry> GetAllEntries(ulong hash)
        {
            OWRootEntry entry;

            if (_rootData.TryGetValue(hash, out entry))
                yield return entry.baseEntry;
        }

        // Returns only entries that match current locale and content flags
        public override IEnumerable<RootEntry> GetEntries(ulong hash)
        {
            return GetAllEntries(hash);
        }

        public bool GetEntry(ulong hash, out OWRootEntry entry)
        {
            return _rootData.TryGetValue(hash, out entry);
        }

        public override void LoadListFile(string path, BackgroundWorkerEx worker = null)
        {
            worker?.ReportProgress(0, "Loading \"listfile\"...");

            Logger.WriteLine("OWRootHandler: loading file names...");

            float pkgOnePct = apmFiles.Sum(a => a.Packages.Length) / 100f;

            int pkgCount = 0;

            foreach (var apm in apmFiles)
            {
                for (int i = 0; i < apm.Packages.Length; i++)
                {
                    APMPackage package = apm.Packages[i];

                    MD5Hash pkgIndexMD5 = package.indexContentKey;

                    string apmName = Path.GetFileNameWithoutExtension(apm.Name);
                    string pkgName = string.Format("{0}/package_{1:X4}_{2:X16}", apmName, i, package.packageKey);
                    string fakeName = string.Format("{0}_index", pkgName);

                    ulong fileHash = Hasher.ComputeHash(fakeName);
                    Logger.WriteLine("Adding package: {0:X16} {1}", fileHash, package.indexContentKey.ToHexString());
                    if (_rootData.ContainsKey(fileHash))
                    {
                        if (!_rootData[fileHash].baseEntry.MD5.EqualsTo(package.indexContentKey))
                            Logger.WriteLine("Weird duplicate package: {0:X16} {1}", fileHash, package.indexContentKey.ToHexString());
                        else
                            Logger.WriteLine("Duplicate package: {0:X16} {1}", fileHash, package.indexContentKey.ToHexString());
                        continue;
                    }
                    _rootData[fileHash] = new OWRootEntry()
                    {
                        baseEntry = new RootEntry() { MD5 = pkgIndexMD5, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None }
                    };

                    CASCFile.FileNames[fileHash] = fakeName;

                    PackageIndex pkgIndex = apm.Indexes[i];

                    fakeName = string.Format("{0}_bundle_{1:X16}", pkgName, pkgIndex.bundleKey);

                    fileHash = Hasher.ComputeHash(fakeName);
                    Logger.WriteLine("Adding bundle: {0:X16} {1}", fileHash, pkgIndex.bundleContentKey.ToHexString());
                    if (_rootData.ContainsKey(fileHash))
                    {
                        if (!_rootData[fileHash].baseEntry.MD5.EqualsTo(pkgIndex.bundleContentKey))
                            Logger.WriteLine("Weird duplicate bundle: {0:X16} {1}", fileHash, pkgIndex.bundleContentKey.ToHexString());
                        else
                            Logger.WriteLine("Duplicate bundle: {0:X16} {1}", fileHash, pkgIndex.bundleContentKey.ToHexString());
                        continue;
                    }
                    _rootData[fileHash] = new OWRootEntry()
                    {
                        baseEntry = new RootEntry() { MD5 = pkgIndex.bundleContentKey, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None },
                        pkgIndex = pkgIndex
                    };

                    CASCFile.FileNames[fileHash] = fakeName;

                    PackageIndexRecord[] records = apm.Records[i];

                    for (int k = 0; k < records.Length; k++)
                    {
                        fakeName = string.Format("files/{0:X3}/{1:X12}.{0:X3}", keyToTypeID(records[k].Key), records[k].Key & 0xFFFFFFFFFFFF);

                        fileHash = Hasher.ComputeHash(fakeName);
                        //Logger.WriteLine("Adding package record: key {0:X16} hash {1} flags {2:X8}", fileHash, records[k].contentKey.ToHexString(), records[k].flags);
                        if (_rootData.ContainsKey(fileHash))
                        {
                            if (!_rootData[fileHash].baseEntry.MD5.EqualsTo(records[k].ContentKey))
                                Logger.WriteLine("Weird duplicate package record: {0:X16} {1}", fileHash, records[k].ContentKey.ToHexString());
                            //else
                            //    Logger.WriteLine("Duplicate package record: {0:X16} {1}", fileHash, records[k].contentKey.ToHexString());
                            continue;
                        }
                        _rootData[fileHash] = new OWRootEntry()
                        {
                            baseEntry = new RootEntry() { MD5 = records[k].ContentKey, LocaleFlags = LocaleFlags.All, ContentFlags = (ContentFlags)records[k].Flags },
                            pkgIndex = pkgIndex,
                            pkgIndexRec = records[k]
                        };

                        CASCFile.FileNames[fileHash] = fakeName;
                    }

                    worker?.ReportProgress((int)(++pkgCount / pkgOnePct));
                }
            }

            Logger.WriteLine("OWRootHandler: loaded {0} file names", _rootData.Count);
        }

        protected override CASCFolder CreateStorageTree()
        {
            var root = new CASCFolder("root");

            CountSelect = 0;
            CountUnknown = 0;

            foreach (var rootEntry in _rootData)
            {
                if ((rootEntry.Value.baseEntry.LocaleFlags & Locale) == 0)
                    continue;

                CreateSubTree(root, rootEntry.Key, CASCFile.FileNames[rootEntry.Key]);
                CountSelect++;
            }

            Logger.WriteLine("OwRootHandler: {0} file names missing for locale {1}", CountUnknown, Locale);

            return root;
        }

        public override void Clear()
        {
            _rootData.Clear();
            Root.Entries.Clear();
            CASCFile.FileNames.Clear();
        }

        public override void Dump()
        {

        }
    }

    public class APMFile
    {
        private APMPackage[] packages;
        private APMEntry[] entries;
        private PackageIndex[] indexes;
        private PackageIndexRecord[][] records;
        private uint[][] dependencies;

        public APMPackage[] Packages => packages;
        public APMEntry[] Entries => entries;
        public PackageIndex[] Indexes => indexes;
        public PackageIndexRecord[][] Records => records;

        public string Name { get; }
        public string cmfHash { get; }

        public APMFile(string name, MD5Hash cmfhash, Stream stream, CASCHandler casc)
        {
            Name = name;

            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Save out APM files for hex viewing
                /*
                string Filename = string.Format("./APMFiles/{0}", name);
                string Pathname = Filename.Substring(0,Filename.LastIndexOf('/'));
                Directory.CreateDirectory(Pathname);
                Stream APMWriter = File.Create(Filename);
                stream.CopyTo(APMWriter);
                APMWriter.Close();
                stream.Seek(0, SeekOrigin.Begin);
                */
                ulong buildVersion = reader.ReadUInt64();
                uint buildNumber = reader.ReadUInt32();
                uint packageCount = reader.ReadUInt32();   // always 0 as of 1.7.0.0
                uint entryCount = reader.ReadUInt32();
                uint unk = reader.ReadUInt32();

                /*
                Console.Out.WriteLine("\nAPM Name: {0}", name);
                Console.Out.WriteLine("APM buildVersion: {0}", buildVersion);
                Console.Out.WriteLine("APM buildNumber: {0}", buildNumber);
                Console.Out.WriteLine("APM PackageCount: {0}", packageCount);
                Console.Out.WriteLine("APM EntryCount: {0}", entryCount);
                Console.Out.WriteLine("APM unk: {0:X}", unk);
                */
                entries = new APMEntry[entryCount];
                for (int j = 0; j < entryCount; j++)
                {
                    entries[j] = reader.Read<APMEntry>();
                    //Console.Out.WriteLine("Entry[{0}]: Index: {1}, hashA: {2}, hashB: {3}", j, entries[j].Index, entries[j].hashA, entries[j].hashB);
                }
                packageCount = (uint)((stream.Length - stream.Position) / Marshal.SizeOf(typeof(APMPackage)));
                //Console.Out.WriteLine("Math PackageCount: {0}", packageCount);

                EncodingEntry cmfEnc;
                List<CMFHashData> cmfHashList = new List<CMFHashData>();

                if (!casc.Encoding.GetEntry(cmfhash, out cmfEnc)) {
                    // Console.Out.WriteLine("Failed to GetEntry for CMF: {0}", cmfhash.ToHexString());
                    return;
                }
                using (Stream cmfStream = casc.OpenFile(cmfEnc.Key)) {
                    using (BinaryReader cmfreader = new BinaryReader(cmfStream)) {
                        cmfStream.Seek(0, SeekOrigin.Begin);

                        ulong cmfbuildVersion = cmfreader.ReadUInt64();
                        ulong cmfbuildNumber = cmfreader.ReadUInt64();
                        uint cmfentryCount = cmfreader.ReadUInt32();
                        uint cmfunk_0 = cmfreader.ReadUInt32();

                        //Console.Out.WriteLine("CMF EntryCount: {0}", cmfentryCount);
                        for (uint i = 0; i < cmfentryCount; i++) {
                            CMFEntry a = cmfreader.Read<CMFEntry>();
                        }

                        uint HashCount = (uint)((cmfStream.Length - cmfStream.Position) / Marshal.SizeOf(typeof(CMFHashData)));
                        //Console.Out.WriteLine("CMF HashCount: {0}", HashCount);
                        for (uint i = 0; i < HashCount; i++) {
                            CMFHashData a = cmfreader.Read<CMFHashData>();
                            // Console.Out.WriteLine("Hash #{0}:\n\tID: {1:X},\n\tFlags: {2:X},\n\tKey: {3:X}", i, a.id, a.flags, a.HashKey.ToHexString());
                            cmfHashList.Add(a);
                        }
                    }
                }

                packages = new APMPackage[packageCount];
                indexes = new PackageIndex[packageCount];
                records = new PackageIndexRecord[packageCount][];
                dependencies = new uint[packageCount][];
                //Console.Out.WriteLine("Package Length: {0}", packages.Length);
                for (int j = 0; j < packages.Length; j++) {
                    packages[j] = new APMPackage(reader.Read<APMPackageItem>());
                    packages[j].indexContentKey = cmfHashList[j].HashKey;
                    //Console.Out.WriteLine("package[{0}]:\n\tlocalKey: {1:X}, \n\tprimaryKey: {2:X}, \n\texternalKey: {3:X}, \n\tencryptionKeyHash: {4:X}, \n\tpackageKey: {5:X}, \n\tunk_0: {6:X}, \n\tunk_1: {7:X}, \n\tunk_2: {8:X}", j, packages[j].localKey, packages[j].primaryKey, packages[j].externalKey, packages[j].encryptionKeyHash, packages[j].packageKey, packages[j].unk_0, packages[j].unk_1, packages[j].unk_2);

                    EncodingEntry pkgIndexEnc;

                    //Console.Out.WriteLine("indexContentKey: {0}", packages[j].indexContentKey.ToHexString());
                    if (!casc.Encoding.GetEntry(packages[j].indexContentKey, out pkgIndexEnc)) {
                        //Console.Out.WriteLine("Couldn't find indexContentKey: {0}", packages[j].indexContentKey.ToHexString());
                        //continue;
                        throw new Exception("pkgIndexEnc missing");
                    }

                    using (Stream pkgIndexStream = casc.OpenFile(pkgIndexEnc.Key))
                    using (BinaryReader pkgIndexReader = new BinaryReader(pkgIndexStream))
                    {
                        // Write out Package Index files
                        /*
                        string pkgfilename = string.Format("./Packages/{0}/{1:X}.pkgindx", name, packages[j].packageKey);
                        string pkgPathname = pkgfilename.Substring(0, pkgfilename.LastIndexOf('/'));
                        Directory.CreateDirectory(pkgPathname);
                        Stream pkgWriter = File.Create(pkgfilename);
                        pkgIndexStream.CopyTo(pkgWriter);
                        pkgWriter.Close();
                        pkgIndexStream.Seek(0, SeekOrigin.Begin);
                        */

                        indexes[j] = pkgIndexReader.Read<PackageIndex>();
                        // Console.Out.WriteLine("index[{0}]:\n\trecordsOffset: {1}|{1:X}\n\tunkOffset_0: {2}|{2:X}\n\tunk_1300_0: {3}|{3:X}\n\tdepsOffset: {4}|{4:X}\n\tunkOffset_1: {5}|{5:X}\n\tunk_0: {6}|{6:X}", j, indexes[j].recordsOffset, indexes[j].unkOffset_0, indexes[j].unk_1300_0, indexes[j].depsOffset, indexes[j].unkOffset_1, indexes[j].unk_0);

                        pkgIndexStream.Position = indexes[j].recordsOffset;

                        using (GZipStream recordsStream = new GZipStream(pkgIndexStream, CompressionMode.Decompress, true))
                        using (BinaryReader recordsReader = new BinaryReader(recordsStream))
                        {
                            PackageIndexRecord[] recs = new PackageIndexRecord[indexes[j].numRecords];

                            for (int k = 0; k < recs.Length; k++)
                                recs[k] = recordsReader.Read<PackageIndexRecord>();

                            records[j] = recs;
                        }

                        pkgIndexStream.Position = indexes[j].depsOffset;

                        uint[] deps = new uint[indexes[j].numDeps];

                        for (int k = 0; k < deps.Length; k++)
                            deps[k] = pkgIndexReader.ReadUInt32();

                        dependencies[j] = deps;
                    }
                }
            }
        }

        public APMPackage GetPackage(int index)
        {
            return packages[index];
        }

        public APMEntry GetEntry(int index)
        {
            return entries[index];
        }
    }
}
