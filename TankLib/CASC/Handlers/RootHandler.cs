using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TankLib.CASC.Helpers;
using TankLib.Helpers.Hash;

namespace TankLib.CASC.Handlers {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct RootEntry {
        public MD5Hash MD5;
        public ContentFlags ContentFlags;
        public LocaleFlags LocaleFlags;
        public PackageIndex pkgIndex;
        public PackageIndexRecord pkgIndexRec;
    }
    
    public class RootHandler {
        public static bool LoadPackages = false;
        
        protected readonly Jenkins96 Hasher = new Jenkins96();
        private readonly Dictionary<ulong, RootEntry> _rootData = new Dictionary<ulong, RootEntry>();
        public readonly List<ApplicationPackageManifest> APMFiles = new List<ApplicationPackageManifest>();
        
        public int Count => _rootData.Count;
        public string[] APMList;
        public LocaleFlags Locale;
        
        public RootHandler(BinaryReader stream, BackgroundWorkerEx worker, CASCHandler casc) {
            worker?.ReportProgress(0, "Loading APM data...");

            string str = Encoding.ASCII.GetString(stream.ReadBytes((int)stream.BaseStream.Length));

            string[] array = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> components = array[0].Substring(1).ToUpper().Split('|').ToList();
            components = components.Select(c => c.Split('!')[0]).ToList();
            int nameComponentIdx = components.IndexOf("FILENAME");
            if (nameComponentIdx == -1) {
                nameComponentIdx = 0;
            }
            int md5ComponentIdx = components.IndexOf("MD5");
            if (md5ComponentIdx == -1) {
                md5ComponentIdx = 1;
            }
            components.Clear();

            Dictionary<string, MD5Hash> cmfHashes = new Dictionary<string, MD5Hash>();
            List<string> apmNames = new List<string>();
            for (int i = 1; i < array.Length; i++) {
                string[] filedata = array[i].Split('|');
                string name = filedata[nameComponentIdx];

                if (Path.GetExtension(name) != ".cmf" || !name.Contains("RDEV")) continue;
                MD5Hash cmfMD5 = filedata[md5ComponentIdx].ToByteArray().ToMD5();
                EncodingEntry apmEnc;
                if (casc.Config.Languages != null) {
                    bool @break = true;
                    foreach (string lang in casc.Config.Languages) {
                        if (name.Contains("L" + lang)) {
                            @break = false;
                        }
                    }
                    if (@break) {
                        continue;
                    }
                }

                if (!casc.EncodingHandler.GetEntry(cmfMD5, out apmEnc)) {
                    continue;
                }
                cmfHashes.Add(name, cmfMD5);
            }

            for (int i = 1; i < array.Length; i++) {
                string[] filedata = array[i].Split('|');
                string name = filedata[nameComponentIdx];

                if (Path.GetExtension(name) == ".apm") {
                    apmNames.Add(Path.GetFileNameWithoutExtension(name));
                    // add apm file for dev purposes
                    ulong apmNameHash = Hasher.ComputeHash(name);
                    MD5Hash apmMD5 = filedata[md5ComponentIdx].ToByteArray().ToMD5();
                    LocaleFlags apmLang = HeurFigureLangFromName(Path.GetFileNameWithoutExtension(name));
                    _rootData[apmNameHash] = new RootEntry {MD5 = apmMD5, LocaleFlags = apmLang, ContentFlags = ContentFlags.None };

                    //CASCFile.Files[apmNameHash] = new CASCFile(apmNameHash, name);
                    if (!name.Contains("RDEV")) {
                        continue;
                    }
                    if (casc.Config.Languages != null) {
                        bool @break = true;
                        foreach (string lang in casc.Config.Languages) {
                            if (name.Contains("L" + lang)) {
                                @break = false;
                                break;
                            }
                        }
                        if (@break) {
                            continue;
                        }
                    }

                    if (!casc.EncodingHandler.GetEntry(apmMD5, out EncodingEntry apmEnc)) {
                        continue;
                    }

                    MD5Hash cmf;
                    string cmfname = $"{Path.GetDirectoryName(name)}/{Path.GetFileNameWithoutExtension(name)}.cmf";
                    ulong cmfNameHash = Hasher.ComputeHash(cmfname);
                    // Console.Out.WriteLine("CMF File Name: {0}", cmfname);
                    if (cmfHashes.ContainsKey(cmfname)) {
                        cmfHashes.TryGetValue(cmfname, out cmf);
                        // Console.Out.WriteLine("CMF Hash Value: {0:X}", cmf.ToHexString());
                    }
                    _rootData[cmfNameHash] = new RootEntry {MD5 = cmf, LocaleFlags = _rootData[apmNameHash].LocaleFlags, ContentFlags = ContentFlags.None};
                    //CASCFile.Files[cmfNameHash] = new CASCFile(cmfNameHash, cmfname);

                    //  Console.Out.WriteLine("Sucessfully Got Entry.\napmEnc.key: {0}", apmEnc.Key.ToHexString());
                    using (Stream apmStream = casc.OpenFile(apmEnc.Key)) {
                        try {
                            Console.Out.WriteLine("Loading APM {0}", name);
                            worker?.ReportProgress(0, $"Loading APM {name}...");
                            ApplicationPackageManifest apm = new ApplicationPackageManifest(name, cmf, apmStream, casc, _rootData[apmNameHash].LocaleFlags, worker);
                            APMFiles.Add(apm);
                        } catch(CryptographicException) {
                            worker?.ReportProgress(0, "CMF decryption failed");
                            Console.Error.WriteLine("CMF Procedure is outdated, cannot parse {0}\r\nPlease update CMFLib", name);
                            Debugger.Log(0, "CASC", $"RootHandler: CMF decryption procedure outdated, unable to parse {name}\r\n");

                            Environment.Exit(0x636D6614);
                            //Logger.GracefulExit(0x636D6614);
                        }
                    }
                }

                worker?.ReportProgress((int)(i / (array.Length / 100f)));
            }
            APMList = apmNames.ToArray();
            apmNames.Clear();
        }
        
        private LocaleFlags HeurFigureLangFromName(string name) {
            string tag = name.Split('_').Reverse().Single(v => v[0] == 'L' && v.Length == 5);
            if (tag == null) {
                return LocaleFlags.All;
            }
            Enum.TryParse(tag.Substring(1), out LocaleFlags flags);
            if (flags == 0) {
                flags = LocaleFlags.All;
            }
            return flags;
        }
        
        public IEnumerable<RootEntry> GetEntries(ulong hash) {
            return GetEntriesForSelectedLocale(hash);
        }
        
        protected IEnumerable<RootEntry> GetEntriesForSelectedLocale(ulong hash) {
            IEnumerable<RootEntry> rootInfos = GetAllEntries(hash);

            IEnumerable<RootEntry> rootEntries = rootInfos as RootEntry[] ?? rootInfos.ToArray();
            if (!rootEntries.Any())
                yield break;

            IEnumerable<RootEntry> rootInfosLocale = rootEntries.Where(re => (re.LocaleFlags & Locale) != 0);

            foreach (RootEntry entry in rootInfosLocale)
                yield return entry;
        }
        
        public IEnumerable<KeyValuePair<ulong, RootEntry>> GetAllEntries() {
            foreach (KeyValuePair<ulong, RootEntry> entry in _rootData)
                yield return new KeyValuePair<ulong, RootEntry>(entry.Key, entry.Value);
        }
        
        public IEnumerable<RootEntry> GetAllEntries(ulong hash) {
            if (_rootData.TryGetValue(hash, out RootEntry entry))
                yield return entry;
        }
        
        public bool GetEntry(ulong hash, out RootEntry entry) {
            return _rootData.TryGetValue(hash, out entry);
        }
    }
}