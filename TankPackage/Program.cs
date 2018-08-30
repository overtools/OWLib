using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using DataTool;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using DataTool.ConvertLogic;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using static TankLib.CASC.ApplicationPackageManifest.Types;
using TankLib;
using TankLib.CASC;
using System.Threading.Tasks;

namespace TankPackage
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Files = new Dictionary<ulong, PackageRecord>();
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();


            Flags = FlagParser.Parse<ToolFlags>();
            if (Flags == null)
            {
                return;
            }

            #region Initialize CASC
            Log("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Util.GetVersion());
            Log("Initializing CASC...");
            if (Flags.Language != null)
            {
                Log("Set language to {0}", Flags.Language);
            }
            if (Flags.SpeechLanguage != null)
            {
                Log("Set speech language to {0}", Flags.SpeechLanguage);
            }
            CASCHandler.Cache.CacheAPM = Flags.UseCache;
            CASCHandler.Cache.CacheCDN = Flags.UseCache;
            CASCHandler.Cache.CacheCDNData = Flags.CacheCDNData;
            Config = CASCConfig.LoadFromString(Flags.OverwatchDirectory, Flags.SkipKeys);
            Config.SpeechLanguage = Flags.SpeechLanguage ?? Flags.Language ?? Config.SpeechLanguage;
            Config.TextLanguage = Flags.Language ?? Config.TextLanguage;
            #endregion

            BuildVersion = uint.Parse(Config.BuildVersion.Split('.').Last());

            if (Flags.SkipKeys)
            {
                Log("Disabling Key auto-detection...");
            }

            Log("Using Overwatch Version {0}", Config.BuildVersion);
            CASC = CASCHandler.Open(Config);
            Root = CASC.RootHandler;
            if (Root == null)
            {
                ErrorLog("Not a valid overwatch installation");
                return;
            }

            if (!Root.APMFiles.Any())
            {
                ErrorLog("Could not find the files for language {0}. Please confirm that you have that language installed, and are using the names from the target language.", Flags.Language);
                if (!Flags.GracefulExit)
                {
                    return;
                }
            }

            string[] modeArgs = Flags.Positionals.Skip(2).ToArray();

            switch (Flags.Mode.ToLower())
            {
                case "extract":
                    Extract(modeArgs);
                    break;
                case "extract-type":
                    ExtractType(modeArgs);
                    break;
                case "search":
                    Search(modeArgs);
                    break;
                case "search-type":
                    SearchType(modeArgs);
                    break;
                case "info":
                    Info(modeArgs);
                    break;
                case "convert":
                    Convert(modeArgs);
                    break;
                case "types":
                    Types(modeArgs);
                    break;
                default:
                    Console.Out.WriteLine("Available modes: extract, search, search-type, info");
                    break;
            }
        }

        private static void Types(string[] args)
        {
            IOrderedEnumerable<ulong> unique = new HashSet<ulong>(Root.APMFiles.SelectMany(x => x.FirstOccurence.Keys).Select(x => teResourceGUID.Attribute(x, teResourceGUID.AttributeEnum.Type))).OrderBy(x => x >> 48);

            foreach (ulong key in unique)
            {
                ushort sh = (ushort)(key >> 48);
                ushort shBE = (ushort)(((sh & 0xFF) << 8) | sh >> 8);
                Console.Out.WriteLine($"{shBE:X4} : {sh:X4} : {teResourceGUID.Type(key):X3}");
            }
        }

        private static void ExtractType(string[] args)
        {
            string output = args.FirstOrDefault();
            ulong[] guids = args.Skip(1).Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            foreach (ApplicationPackageManifest apm in Root.APMFiles) {
                Save(output, apm.Header.Checksum, apm.FirstOccurence.Where(x => guids.Length == 0 || guids.Contains(teResourceGUID.Type(x.Key))).Select(x => x.Value));
            }
        }

        private static void Extract(string[] args)
        {
            string output = args.FirstOrDefault();
            ulong[] guids = args.Skip(1).Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            Dictionary<ulong, PackageRecord[]> records = new Dictionary<ulong, PackageRecord[]>();
            Dictionary<ulong, PackageRecord[]> totalRecords = new Dictionary<ulong, PackageRecord[]>();
            Dictionary<ulong, Package> packages = new Dictionary<ulong, Package>();

            foreach (ApplicationPackageManifest apm in Root.APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (guids.Contains(teResourceGUID.LongKey(entry.PackageGUID)) || guids.Contains(teResourceGUID.Index(entry.PackageGUID)))
                    {
                        packages[entry.PackageGUID] = apm.Packages[i];
                        records[entry.PackageGUID] = apm.Records[i];
                    }
                    totalRecords[entry.PackageGUID] = apm.Records[i];
                }
            }

            foreach (ulong key in records.Keys)
            {
                Save(output, key, records[key]);
            }
        }

        private static void Convert(string[] args)
        {
            string output = args.FirstOrDefault();
            ulong[] guids = args.Skip(1).Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            Dictionary<ulong, PackageRecord[]> records = new Dictionary<ulong, PackageRecord[]>();
            Dictionary<ulong, Package> packages = new Dictionary<ulong, Package>();

            foreach (ApplicationPackageManifest apm in Root.APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (!guids.Contains(teResourceGUID.LongKey(entry.PackageGUID)) && !guids.Contains(teResourceGUID.Index(entry.PackageGUID))) continue;
                    packages[entry.PackageGUID] = apm.Packages[i];
                    records[entry.PackageGUID] = apm.Records[i];
                }
            }

            ICLIFlags flags = FlagParser.Parse<ExtractFlags>();
            MapCMF();
            LoadGUIDTable();
            Sound.WwiseBank.GetReady();

            void Body(ulong key) {
                DataTool.FindLogic.Combo.ComboInfo info = new DataTool.FindLogic.Combo.ComboInfo();
                string dest = Path.Combine(output, teResourceGUID.AsString(key));
                foreach (PackageRecord record in records[key]) {
                    DataTool.FindLogic.Combo.Find(info, record.GUID);
                }

                DataTool.SaveLogic.Combo.Save(flags, dest, info);
            }

            Parallel.ForEach(records.Keys, Body);
        }

        private static void Save(string output, ulong key, IEnumerable<PackageRecord> value) => Save(output, key, key, value);

        private static void Save(string output, ulong parentKey, ulong myKey, IEnumerable<PackageRecord> records)
        {
            string dest = Path.Combine(output, teResourceGUID.AsString(parentKey));
            if (myKey != parentKey)
            {
                dest = Path.Combine(dest, "sib", teResourceGUID.AsString(myKey));
            }

            void Body(PackageRecord record) {
                using (Stream file = OpenFile(record)) {
                    string tmp = Path.Combine(dest, $"{teResourceGUID.Type(record.GUID):X3}");
                    if (!Directory.Exists(tmp)) {
                        Directory.CreateDirectory(tmp);
                    }

                    tmp = Path.Combine(tmp, teResourceGUID.AsString(record.GUID));
                    InfoLog("Saved {0}", tmp);
                    WriteFile(file, tmp);
                }
            }

            Parallel.ForEach(records, Body);
        }

        private static void Search(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in Root.APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    PackageRecord[] records = apm.Records[i];

                    foreach (PackageRecord record in records.Where(x => guids.Contains(x.GUID) || guids.Contains(teResourceGUID.Type(x.GUID)) || guids.Contains(teResourceGUID.Index(x.GUID)) || guids.Contains(teResourceGUID.LongKey(x.GUID))))
                    {
                        Log("Found {0} in package {1:X12}", teResourceGUID.AsString(record.GUID), teResourceGUID.LongKey(entry.PackageGUID));
                    }
                }
            }
        }

        private static void SearchType(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in Root.APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    PackageRecord[] records = apm.Records[i];

                    foreach (PackageRecord record in records.Where(x => guids.Contains(teResourceGUID.Type(x.GUID))))
                    {
                        Log("Found {0} in package {1:X12}", teResourceGUID.AsString(record.GUID), teResourceGUID.LongKey(entry.PackageGUID));
                    }
                }
            }
        }

        private static void Info(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in Root.APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (!guids.Contains(teResourceGUID.LongKey(entry.PackageGUID)) && !guids.Contains(teResourceGUID.Index(entry.PackageGUID))) continue;
                    Log("Package {0:X12}:", teResourceGUID.LongKey(entry.PackageGUID));
                    Log("\tUnknowns: {0}, {1}", entry.Unknown1, entry.Unknown2);
                    Log("\t{0} records", apm.Records[i].Length);
                    Log("\t{0} siblings", apm.PackageSiblings[i].Length);
                    foreach (ulong sibling in apm.PackageSiblings[i])
                    {
                        Log("\t\t{0}", teResourceGUID.AsString(sibling));
                    }
                }
            }
        }
    }
}
