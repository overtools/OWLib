using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CASCExplorer {
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
  public struct APMPackageItem {
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
  public struct CMFHashData {
    public ulong id;
    public uint flags;
    public MD5Hash HashKey;
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
    public ulong bundleKey;
    public ulong unk_3;
    //PackageIndexRecord[numRecords] records;   // See recordsOffset and PackageIndexRecord
    //u32[numDeps] dependencies;                // See depsOffset
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct PackageIndex {
    public long recordsOffset;                  // Offset to GZIP compressed records chunk, read (recordsSize + numRecords) bytes here
    public long unkOffset_0;
    public long unkOffset_1;
    public long depsOffset;                    // Offset to dependencies chunk, read numDeps * uint here
    public ulong unk_0;
    public uint numUnk_0;
    public uint numRecords;
    public uint numUnk_1;
    public uint recordsSize;
    public uint unk_1;
    public uint numDeps;
    public ulong unk_2;
    public ulong bundleKey;                     // Requires some sorcery, see Key
    public ulong unk_3;
    public MD5Hash bundleContentKey;
    //PackageIndexRecord[numRecords] records;   // See recordsOffset and PackageIndexRecord
    //u32[numDeps] dependencies;                // See depsOffset

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
  public struct PackageIndexRecord {
    public ulong Key;               // Requires some sorcery, see Key
    public uint Flags;              // Flags. Has 0x40000000 when in bundle, otherwise in encoding
    public uint Offset;             // Offset into bundle
    public int Size;
    public MD5Hash ContentKey;      // If it doesn't have the above flag (0x40000000) look it up in encoding

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
    public ulong Key;               // Requires some sorcery, see Key
    public uint Flags;              // Flags. Has 0x40000000 when in bundle, otherwise in encoding
    public uint Offset;             // Offset into bundle
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct OWRootEntry {
    public RootEntry baseEntry;
    public PackageIndex pkgIndex;
    public PackageIndexRecord pkgIndexRec;
  }

  public class OwRootHandler : RootHandlerBase {
    private readonly Dictionary<ulong, OWRootEntry> _rootData = new Dictionary<ulong, OWRootEntry>();

    private readonly List<APMFile> apmFiles = new List<APMFile>();

    public override int Count => _rootData.Count;
    public IReadOnlyList<APMFile> APMFiles => apmFiles;

    public string[] APMList;
    public static string LanguageScan = "enUS";

    public OwRootHandler(BinaryReader stream, BackgroundWorkerEx worker, CASCHandler casc) {
      worker?.ReportProgress(0, "Loading APM data...");

      string str = Encoding.ASCII.GetString(stream.ReadBytes((int)stream.BaseStream.Length));

      string[] array = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

      List<string> components = array[0].Substring(1).ToUpper().Split('|').ToList();
      components = components.Select(c => c.Split('!')[0]).ToList();
      int nameComponentIDX = components.IndexOf("FILENAME");
      if(nameComponentIDX == -1) {
        nameComponentIDX = 0;
      }
      int md5ComponentIDX = components.IndexOf("MD5");
      if(md5ComponentIDX == -1) {
        md5ComponentIDX = 1;
      }
      components.Clear();

      Dictionary<string, MD5Hash> CMFHashes = new Dictionary<string, MD5Hash>();
      List<string> APMNames = new List<string>();
      for(int i = 1; i < array.Length; i++) {
        string[] filedata = array[i].Split('|');
        string name = filedata[nameComponentIDX];

        if(Path.GetExtension(name) == ".cmf" && name.Contains("RDEV")) {
          MD5Hash cmfMD5 = filedata[md5ComponentIDX].ToByteArray().ToMD5();
          EncodingEntry apmEnc;
          if(LanguageScan != null && !name.Contains("L" + LanguageScan)) {
            continue;
          }

          if(!casc.Encoding.GetEntry(cmfMD5, out apmEnc)) {
            //Console.Out.WriteLine("Failed to GetEntry: {0}", cmfMD5.ToHexString());
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

      for(int i = 1; i < array.Length; i++) {
        string[] filedata = array[i].Split('|');
        //Console.Out.WriteLine("Array[{0}]: {1}", i, array[i]);
        string name = filedata[nameComponentIDX];

        if(Path.GetExtension(name) == ".apm") {
          APMNames.Add(Path.GetFileNameWithoutExtension(name));
          if(!name.Contains("RDEV")) {
            continue;
          }
          if(LanguageScan != null && !name.Contains("L" + LanguageScan)) {
            continue;
          }
          // add apm file for dev purposes
          ulong apmNameHash = Hasher.ComputeHash(name);
          MD5Hash apmMD5 = filedata[md5ComponentIDX].ToByteArray().ToMD5();
          // Console.Out.WriteLine("apmNameHash: {0}, apmMD5: {1}", apmNameHash, apmMD5.ToHexString());
          _rootData[apmNameHash] = new OWRootEntry() {
            baseEntry = new RootEntry() { MD5 = apmMD5, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None }
          };

          CASCFile.FileNames[apmNameHash] = name;
          // Console.Out.WriteLine("name: {0}", name);

          EncodingEntry apmEnc;

          if(!casc.Encoding.GetEntry(apmMD5, out apmEnc)) {
            //Console.Out.WriteLine("Failed to GetEntry: {0}", apmMD5.ToHexString());
            continue;
          }

          MD5Hash cmf;
          string cmfname = string.Format("{0}/{1}.cmf", Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name));
          ulong cmfNameHash = Hasher.ComputeHash(cmfname);
          // Console.Out.WriteLine("CMF File Name: {0}", cmfname);
          if(CMFHashes.ContainsKey(cmfname) == true) {
            CMFHashes.TryGetValue(cmfname, out cmf);
            // Console.Out.WriteLine("CMF Hash Value: {0:X}", cmf.ToHexString());
          }
          _rootData[cmfNameHash] = new OWRootEntry() {
            baseEntry = new RootEntry() { MD5 = cmf, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None }
          };
          CASCFile.FileNames[cmfNameHash] = cmfname;

          //  Console.Out.WriteLine("Sucessfully Got Entry.\napmEnc.key: {0}", apmEnc.Key.ToHexString());
          using(Stream apmStream = casc.OpenFile(apmEnc.Key)) {
            apmFiles.Add(new APMFile(name, cmf, apmStream, casc));
          }
        }

        worker?.ReportProgress((int)(i / (array.Length / 100f)));
      }
      APMList = APMNames.ToArray();
      APMNames.Clear();
    }

    static ulong keyToTypeID(ulong key) {
      var num = (key >> 48);
      num = (((num >> 1) & 0x55555555) | ((num & 0x55555555) << 1));
      num = (((num >> 2) & 0x33333333) | ((num & 0x33333333) << 2));
      num = (((num >> 4) & 0x0F0F0F0F) | ((num & 0x0F0F0F0F) << 4));
      num = (((num >> 8) & 0x00FF00FF) | ((num & 0x00FF00FF) << 8));
      num = ((num >> 16) | (num << 16));
      num >>= 20;
      return num + 1;
    }

    public override IEnumerable<KeyValuePair<ulong, RootEntry>> GetAllEntries() {
      foreach(var entry in _rootData)
        yield return new KeyValuePair<ulong, RootEntry>(entry.Key, entry.Value.baseEntry);
    }

    public override IEnumerable<RootEntry> GetAllEntries(ulong hash) {
      OWRootEntry entry;

      if(_rootData.TryGetValue(hash, out entry))
        yield return entry.baseEntry;
    }

    // Returns only entries that match current locale and content flags
    public override IEnumerable<RootEntry> GetEntries(ulong hash) {
      return GetAllEntries(hash);
    }

    public bool GetEntry(ulong hash, out OWRootEntry entry) {
      return _rootData.TryGetValue(hash, out entry);
    }

    public override void LoadListFile(string path, BackgroundWorkerEx worker = null) {
      Logger.WriteLine("OWRootHandler: loading file names...");
      foreach(APMFile apm in apmFiles) {
        Logger.WriteLine($"OWRootHandler: processing {apm.Name}...");
        worker?.ReportProgress(0, $"Loading APM {apm.Name}...");

        float one = apm.Packages.Length / 100f;
        int count = 0;
        for(int i = 0; i < apm.Packages.Length; i++) {
          APMPackage package = apm.Packages[i];
          PackageIndex index = apm.Indexes[i];
          MD5Hash packageHash = apm.CMFMap[package.packageKey].HashKey;
          PackageIndexRecord[] records = apm.Records[i];

          for(int j = 0; j < records.Length; j++) {
            string recordName = string.Format("files/{0:X3}/{1:X12}.{0:X3}", keyToTypeID(records[j].Key), records[j].Key & 0xFFFFFFFFFFFF);

            ulong recordHash = Hasher.ComputeHash(recordName);
            if(_rootData.ContainsKey(recordHash)) {
              continue;
            }
            _rootData[recordHash] = new OWRootEntry() {
              baseEntry = new RootEntry() { MD5 = records[j].ContentKey, LocaleFlags = LocaleFlags.All, ContentFlags = (ContentFlags)records[j].Flags },
              pkgIndex = index,
              pkgIndexRec = records[j]
            };

            CASCFile.FileNames[recordHash] = recordName;
          }

          worker?.ReportProgress((int)(++count / one / 2));
        }

        count = 0;
        one = apm.CMFMap.Count / 100f;

        worker?.ReportProgress(50, $"Loading CMF...");
        foreach(KeyValuePair<ulong, CMFHashData> pair in apm.CMFMap) {
          string recordName = string.Format("files/{0:X3}/{1:X12}.{0:X3}", keyToTypeID(pair.Key), pair.Key & 0xFFFFFFFFFFFF);

          ulong recordHash = Hasher.ComputeHash(recordName);
          if(_rootData.ContainsKey(recordHash)) {
            continue;
          }

          _rootData[recordHash] = new OWRootEntry() {
            baseEntry = new RootEntry() { MD5 = pair.Value.HashKey, LocaleFlags = LocaleFlags.All, ContentFlags = ContentFlags.None },
            pkgIndex = new PackageIndex(new PackageIndexItem()),
            pkgIndexRec = new PackageIndexRecord(new PackageIndexRecordItem())
          };

          CASCFile.FileNames[recordHash] = recordName;

          worker?.ReportProgress(50 + (int)(++count / one / 2));
        }
      }

      Logger.WriteLine("OWRootHandler: loaded {0} file names", _rootData.Count);
    }

    protected override CASCFolder CreateStorageTree() {
      var root = new CASCFolder("root");

      CountSelect = 0;
      CountUnknown = 0;

      foreach(var rootEntry in _rootData) {
        if((rootEntry.Value.baseEntry.LocaleFlags & Locale) == 0)
          continue;

        CreateSubTree(root, rootEntry.Key, CASCFile.FileNames[rootEntry.Key]);
        CountSelect++;
      }

      Logger.WriteLine("OwRootHandler: {0} file names missing for locale {1}", CountUnknown, Locale);

      return root;
    }

    public override void Clear() {
      _rootData.Clear();
      Root.Entries.Clear();
      CASCFile.FileNames.Clear();
    }

    public override void Dump() {

    }
  }

  public class APMFile {
    private APMPackage[] packages;
    private APMEntry[] entries;
    private PackageIndex[] indexes;
    private PackageIndexRecord[][] records;
    private uint[][] dependencies;
    private List<CMFHashData> cmfHashList;
    private List<CMFEntry> cmfEntries;
    private Dictionary<ulong, CMFHashData> cmfMap;

    public APMPackage[] Packages => packages;
    public APMEntry[] Entries => entries;
    public PackageIndex[] Indexes => indexes;
    public PackageIndexRecord[][] Records => records;
    public List<CMFHashData> CMFHashList => cmfHashList;
    public List<CMFEntry> CMFEntries => cmfEntries;
    public Dictionary<ulong, CMFHashData> CMFMap => cmfMap;

    public string Name
    {
      get;
    }
    public string cmfHash
    {
      get;
    }

    internal void ParseCMF(BinaryReader cmfreader, uint cmfentryCount, uint cmfhashCount) {
      cmfEntries = new List<CMFEntry>((int)cmfentryCount);
      for(uint i = 0; i < cmfentryCount; i++) {
        CMFEntry a = cmfreader.Read<CMFEntry>();
        cmfEntries.Add(a);
      }

      cmfHashList = new List<CMFHashData>((int)cmfhashCount);
      cmfMap = new Dictionary<ulong, CMFHashData>((int)cmfhashCount);
      for(uint i = 0; i < cmfhashCount; i++) {
        CMFHashData a = cmfreader.Read<CMFHashData>();
        cmfHashList.Add(a);
        cmfMap[a.id] = a;
      }
    }



    private static byte[] GenerateKey(uint secret, int size) {
      var buffer = new byte[size];
      uint kidx = KeyTable[secret & 0x1FF];
      for(var i = 0; i != size; ++i) {
        buffer[i] = KeyTable[kidx % 512];
        kidx += 3;
      }
      return buffer;
    }

    private static byte[] GenerateIV(uint secret, int size, string name) {
      var buffer = new byte[size];
      var hasher = SHA1.Create();
      var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(name));
      var unk = size * secret;
      var increase = unk % 61;
      for(var i = 0; i != size; ++i) {
        buffer[i] = KeyTable[unk % 512];
        unk += increase;
        buffer[i] ^= hash[(unk - i) % 20];
      }
      return buffer;
    }

    private BinaryReader DecryptCMF(BinaryReader cmfreader, string name, uint keySecret /* HashDataCount */, uint ivSecret /* BuildVersion */) {
      var key = GenerateKey(keySecret, 32);
      var iv = GenerateIV(ivSecret, 16, Path.GetFileName(name));
      using(var rijndael = new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC }) {
        var cryptostream = new CryptoStream(cmfreader.BaseStream, rijndael.CreateDecryptor(), CryptoStreamMode.Read);
        return new BinaryReader(cryptostream);
      }
    }

    private static byte[] KeyTable = {
      0xAB, 0xD1, 0x7A, 0xF5, 0xD8, 0x95, 0x03, 0x95, 0x07, 0xBF, 0xD0, 0x51, 0xA3, 0x8D,
      0x54, 0xD2, 0xB7, 0xB9, 0xB3, 0xC7, 0xF3, 0x95, 0x77, 0xB5, 0x86, 0xAB, 0xE0, 0x83, 0xF9, 0x69, 0xD5, 0x05, 0xEF,
      0xB6, 0x39, 0xED, 0x3A, 0xEB, 0x35, 0x99, 0x28, 0x59, 0xFE, 0xA9, 0xB4, 0xC2, 0x3A, 0x32, 0x35, 0xB4, 0xC3, 0xC2,
      0xAE, 0x19, 0xFC, 0xB7, 0xDE, 0x94, 0x41, 0xEC, 0xD8, 0xDC, 0x21, 0xC8, 0x5F, 0x13, 0x26, 0xDE, 0x5D, 0xCF, 0x10,
      0xF6, 0x69, 0x0C, 0x6B, 0xD6, 0x7A, 0xB9, 0x38, 0x0C, 0x33, 0x03, 0x6A, 0x29, 0xFC, 0xCB, 0x44, 0xBE, 0x23, 0xD6,
      0xBB, 0xB7, 0x54, 0x70, 0x3D, 0xE5, 0x9F, 0xF2, 0xC7, 0xC3, 0x21, 0x8B, 0x68, 0x21, 0x9B, 0x0F, 0xC2, 0x80, 0xDD,
      0x19, 0x5B, 0xEB, 0x2A, 0x55, 0x75, 0x81, 0x0D, 0xA0, 0x28, 0xBB, 0x52, 0x0B, 0xA7, 0x34, 0x84, 0x46, 0x99, 0xEB,
      0x7A, 0xA1, 0xD1, 0xBD, 0x27, 0xC3, 0x20, 0xC6, 0x5D, 0x8C, 0xA3, 0xD4, 0x87, 0x9F, 0x90, 0x61, 0xDC, 0x9D, 0x2D,
      0x3E, 0xF3, 0x37, 0xB0, 0xA9, 0x13, 0x97, 0xDD, 0xF6, 0x79, 0x27, 0xFA, 0xC7, 0xF3, 0x6F, 0xC5, 0xF0, 0xFA, 0x52,
      0xF1, 0x82, 0xE0, 0xEF, 0x99, 0x84, 0x18, 0xBB, 0xAD, 0x26, 0x9F, 0xA4, 0xE4, 0xA8, 0xE0, 0x73, 0xA0, 0x13, 0x3B,
      0x14, 0xA5, 0xBA, 0x64, 0xC3, 0xC5, 0x68, 0xF5, 0xBF, 0x4F, 0x94, 0xA0, 0xAC, 0xC8, 0x29, 0xB8, 0xC3, 0x42, 0x18,
      0x09, 0xCF, 0x48, 0x57, 0x69, 0xED, 0x78, 0x84, 0xB9, 0x4A, 0x5B, 0x1F, 0xE8, 0x08, 0x55, 0x55, 0x95, 0x40, 0xBC,
      0x71, 0x4F, 0x22, 0x90, 0xD4, 0x19, 0x61, 0x1F, 0x67, 0x36, 0x7F, 0xBD, 0xF1, 0x76, 0x75, 0xE1, 0x99, 0xB0, 0x9F,
      0x54, 0x71, 0x65, 0x8E, 0xCB, 0x61, 0xC0, 0xF7, 0xE3, 0xA9, 0xAB, 0xE8, 0xD2, 0x3E, 0x48, 0xD4, 0x2D, 0xF8, 0x56,
      0x32, 0xCF, 0xFC, 0x77, 0x77, 0x20, 0x3E, 0x92, 0x78, 0xF0, 0x4F, 0xB7, 0x90, 0x74, 0xF0, 0x41, 0xF9, 0x38, 0x26,
      0xD3, 0x37, 0xD6, 0x10, 0x75, 0x1A, 0x75, 0x12, 0xD9, 0x39, 0x64, 0x1C, 0x37, 0x6D, 0x4B, 0x1C, 0xA8, 0xFF, 0x29,
      0x5C, 0x9F, 0x6E, 0x68, 0x26, 0xCA, 0x3E, 0x06, 0xA6, 0xCB, 0xE6, 0x27, 0x13, 0xD2, 0x12, 0xB2, 0xFF, 0xF7, 0x4A,
      0x4A, 0x19, 0x68, 0x02, 0x85, 0xBE, 0xE2, 0x10, 0xFA, 0x97, 0xFE, 0x1B, 0x4A, 0x1F, 0xA8, 0xDD, 0x7F, 0x69, 0x22,
      0x53, 0xC0, 0x3B, 0xFA, 0x80, 0x77, 0xC2, 0x10, 0x49, 0x59, 0x08, 0x62, 0x3B, 0x62, 0x5F, 0xE5, 0x4F, 0xEF, 0xDC,
      0xC3, 0x56, 0x4A, 0xCB, 0x62, 0x9C, 0x27, 0x91, 0xEA, 0xD9, 0x47, 0x83, 0x55, 0xBD, 0x80, 0x2A, 0x67, 0x0A, 0x4F,
      0x13, 0x1F, 0xE5, 0x6A, 0x06, 0x70, 0x60, 0x3C, 0xC2, 0x4A, 0x03, 0x2A, 0x3B, 0xC2, 0x80, 0xCB, 0xC9, 0x2E, 0x9F,
      0x7F, 0x87, 0x1B, 0xD1, 0x8B, 0xA7, 0xEE, 0xD8, 0x6A, 0x61, 0x85, 0x81, 0x75, 0x40, 0x02, 0x89, 0x23, 0xD6, 0xBC,
      0xC3, 0xB3, 0x38, 0x61, 0x9A, 0x50, 0x41, 0xF4, 0xD5, 0xB0, 0x84, 0xD4, 0xCF, 0x13, 0x87, 0xF7, 0x8F, 0x1C, 0x1F,
      0xAB, 0xA1, 0xCC, 0x17, 0x83, 0x30, 0x8A, 0x6E, 0xDE, 0x86, 0x5F, 0xC4, 0xCC, 0x55, 0x85, 0xF6, 0x8A, 0x60, 0xA3,
      0x4B, 0xF9, 0xC1, 0xA6, 0x8E, 0x31, 0x75, 0xC2, 0xAD, 0xCF, 0x52, 0xCE, 0x29, 0x1D, 0xBE, 0x07, 0x50, 0xD1, 0x20,
      0xA5, 0xB2, 0x4E, 0x79, 0x1C, 0xBC, 0xEA, 0xB4, 0x01, 0xBB, 0x3F, 0x55, 0x75, 0x47, 0xAA, 0xE7, 0x73, 0xF7, 0x78,
      0x3B, 0xC1, 0x63, 0x3D, 0xD5, 0x09, 0xB4, 0x6A, 0x45, 0x01, 0xD7, 0x5C, 0xDA, 0x8E, 0x93, 0xF8, 0x45, 0xDC, 0x5B,
      0x91, 0xDF, 0x8B, 0x41
    };

    public APMFile(string name, MD5Hash cmfhash, Stream stream, CASCHandler casc) {
      Name = name;

      using(BinaryReader reader = new BinaryReader(stream)) {
        // Save out APM files for hex viewing
#if OUTPUT_APM
        string Filename = string.Format("./APMFiles/{0}", name);
        string Pathname = Filename.Substring(0,Filename.LastIndexOf('/'));
        Directory.CreateDirectory(Pathname);
        Stream APMWriter = File.Create(Filename);
        stream.CopyTo(APMWriter);
        APMWriter.Close();
        stream.Seek(0, SeekOrigin.Begin);
#endif
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
        for(int j = 0; j < entryCount; j++) {
          entries[j] = reader.Read<APMEntry>();
          //Console.Out.WriteLine("Entry[{0}]: Index: {1}, hashA: {2}, hashB: {3}", j, entries[j].Index, entries[j].hashA, entries[j].hashB);
        }
        packageCount = (uint)((stream.Length - stream.Position) / Marshal.SizeOf(typeof(APMPackage)));
        //Console.Out.WriteLine("Math PackageCount: {0}", packageCount);

        EncodingEntry cmfEnc;

        if(!casc.Encoding.GetEntry(cmfhash, out cmfEnc)) {
          // Console.Out.WriteLine("Failed to GetEntry for CMF: {0}", cmfhash.ToHexString());
          return;
        }
        using(Stream cmfStream = casc.OpenFile(cmfEnc.Key)) {
#if OUTPUT_CMF
          string cmfFilename = string.Format("./CMF/{0}.cmf", Path.GetFileNameWithoutExtension(name));
          string cmfPathname = cmfFilename.Substring(0,cmfFilename.LastIndexOf('/'));
          Directory.CreateDirectory(cmfPathname);
          Stream CMFWriter = File.Create(cmfFilename);
          cmfStream.CopyTo(CMFWriter);
          CMFWriter.Close();
          cmfStream.Seek(0, SeekOrigin.Begin);
#endif
          using(BinaryReader cmfreader = new BinaryReader(cmfStream)) {
            cmfStream.Seek(0, SeekOrigin.Begin);


            uint cmfbuildVersion = cmfreader.ReadUInt32();
            uint cmfunk0 = cmfreader.ReadUInt32(); // 0
            uint cmfhashCount = cmfreader.ReadUInt32();
            uint cmfunk1 = cmfreader.ReadUInt32(); // 0
            uint cmfentryCount = cmfreader.ReadUInt32();
            uint cmfmagic = cmfreader.ReadUInt32();

            if(cmfmagic == 0x14666D63) {
              ParseCMF(cmfreader, cmfentryCount, cmfhashCount);
            } else if(cmfmagic == 0x636D6614) {
              using(var decryptedReader = DecryptCMF(cmfreader, name, cmfhashCount, cmfbuildVersion)) {
                ParseCMF(decryptedReader, cmfentryCount, cmfhashCount);
              }
            }
          }
        }

        packages = new APMPackage[packageCount];
        indexes = new PackageIndex[packageCount];
        records = new PackageIndexRecord[packageCount][];
        dependencies = new uint[packageCount][];
        //Console.Out.WriteLine("Package Length: {0}", packages.Length);
        //Logger.WriteLine("Package Length: {0}", packageCount);

        for(int j = 0; j < packages.Length; j++) {
          packages[j] = new APMPackage(reader.Read<APMPackageItem>());
          packages[j].indexContentKey = cmfMap[packages[j].packageKey].HashKey;
          //Console.Out.WriteLine("package[{0}]:\n\tlocalKey: {1:X}, \n\tprimaryKey: {2:X}, \n\texternalKey: {3:X}, \n\tencryptionKeyHash: {4:X}, \n\tpackageKey: {5:X}, \n\tunk_0: {6:X}, \n\tunk_1: {7:X}, \n\tunk_2: {8:X}", j, packages[j].localKey, packages[j].primaryKey, packages[j].externalKey, packages[j].encryptionKeyHash, packages[j].packageKey, packages[j].unk_0, packages[j].unk_1, packages[j].unk_2);

          EncodingEntry pkgIndexEnc;

          //Console.Out.WriteLine("indexContentKey: {0}", packages[j].indexContentKey.ToHexString());
          if(!casc.Encoding.GetEntry(packages[j].indexContentKey, out pkgIndexEnc)) {
            //Console.Out.WriteLine("Couldn't find indexContentKey: {0}", packages[j].indexContentKey.ToHexString());
            //continue;
            throw new Exception("pkgIndexEnc missing");
          }

          using(Stream pkgIndexStream = casc.OpenFile(pkgIndexEnc.Key))
          using(BinaryReader pkgIndexReader = new BinaryReader(pkgIndexStream)) {
            // Write out Package Index files
#if OUTPUT_PKG
            string pkgfilename = string.Format("./Packages/{0}/{1:X}.pkgindx", name, packages[j].packageKey);
            string pkgPathname = pkgfilename.Substring(0, pkgfilename.LastIndexOf('/'));
            Directory.CreateDirectory(pkgPathname);
            Stream pkgWriter = File.Create(pkgfilename);
            pkgIndexStream.CopyTo(pkgWriter);
            pkgWriter.Close();
            pkgIndexStream.Seek(0, SeekOrigin.Begin);
#endif

            indexes[j] = new PackageIndex(pkgIndexReader.Read<PackageIndexItem>());
            try {
              indexes[j].bundleContentKey = cmfMap[indexes[j].bundleKey].HashKey;
            } catch { }
            // Console.Out.WriteLine("index[{0}]:\n\trecordsOffset: {1}|{1:X}\n\tunkOffset_0: {2}|{2:X}\n\tunk_1300_0: {3}|{3:X}\n\tdepsOffset: {4}|{4:X}\n\tunkOffset_1: {5}|{5:X}\n\tunk_0: {6}|{6:X}", j, indexes[j].recordsOffset, indexes[j].unkOffset_0, indexes[j].unk_1300_0, indexes[j].depsOffset, indexes[j].unkOffset_1, indexes[j].unk_0);
            // Logger.WriteLine("index[{0}]: {1} {2}", j, indexes[j].numRecords, indexes[j].numUnk_0);
            pkgIndexStream.Position = indexes[j].recordsOffset;

            using(GZipStream recordsStream = new GZipStream(pkgIndexStream, CompressionMode.Decompress, true)) {
              using(BinaryReader recordsReader = new BinaryReader(recordsStream)) {
                PackageIndexRecord[] recs = new PackageIndexRecord[indexes[j].numRecords];

                for(int k = 0; k < recs.Length; k++) {
                  recs[k] = new PackageIndexRecord(recordsReader.Read<PackageIndexRecordItem>());
                  recs[k].ContentKey = cmfMap[recs[k].Key].HashKey;
                  EncodingEntry encInfo;
                  if(casc.Encoding.GetEntry(recs[k].ContentKey, out encInfo)) {
                    recs[k].Size = encInfo.Size; // WHY DOES THIS WORK? ARE BUNDLED FILES NOT ACTUALLY IN A BUNDLE ANYMORE?
                  }
                }
                records[j] = recs;
              }
            }

            pkgIndexStream.Position = indexes[j].depsOffset;

            uint[] deps = new uint[indexes[j].numDeps];

            for(int k = 0; k < deps.Length; k++)
              deps[k] = pkgIndexReader.ReadUInt32();

            dependencies[j] = deps;
          }
        }
      }
    }

    public APMPackage GetPackage(int index) {
      return packages[index];
    }

    public APMEntry GetEntry(int index) {
      return entries[index];
    }
  }
}
