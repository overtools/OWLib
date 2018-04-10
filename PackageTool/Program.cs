using DataTool;
using DataTool.Flag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CASCLib.ApplicationPackageManifest.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using System.Reflection;
using OWLib;
using CASCLib;
using System.Globalization;
using System.IO;
using DataTool.ToolLogic.Extract;
using DataTool.ConvertLogic;

namespace PackageTool
{
    class Program
    {
        static void Main(string[] args)
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
            Log("Set language to {0}", Flags.Language);
            CDNIndexHandler.Cache.Enabled = Flags.UseCache;
            CDNIndexHandler.Cache.CacheData = Flags.CacheData;
            CDNIndexHandler.Cache.Validate = Flags.ValidateCache;
            // ngdp:us:pro
            // http:us:pro:us.patch.battle.net:1119
            if (Flags.OverwatchDirectory.ToLowerInvariant().Substring(0, 5) == "ngdp:")
            {
                string cdn = Flags.OverwatchDirectory.Substring(5, 4);
                string[] parts = Flags.OverwatchDirectory.Substring(5).Split(':');
                string region = "us";
                string product = "pro";
                if (parts.Length > 1)
                {
                    region = parts[1];
                }
                if (parts.Length > 2)
                {
                    product = parts[2];
                }
                if (cdn == "bnet")
                {
                    Config = CASCConfig.LoadOnlineStorageConfig(product, region);
                }
                else
                {
                    if (cdn == "http")
                    {
                        string host = string.Join(":", parts.Skip(3));
                        Config = CASCConfig.LoadOnlineStorageConfig(host, product, region, true, true, true);
                    }
                }
            }
            else
            {
                Config = CASCConfig.LoadLocalStorageConfig(Flags.OverwatchDirectory, !Flags.SkipKeys, false);
            }
            Config.Languages = new HashSet<string>(new[] { Flags.Language });
            #endregion

            BuildVersion = uint.Parse(Config.BuildName.Split('.').Last());

            if (Flags.SkipKeys)
            {
                Log("Disabling Key auto-detection...");
            }

            Log("Using Overwatch Version {0}", Config.BuildName);
            CASC = CASCHandler.OpenStorage(Config);
            Root = CASC.Root as OwRootHandler;
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
                default:
                    Console.Out.WriteLine("Available modes: extract, search, search-type, info");
                    break;
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
            Dictionary<ulong, ulong[]> siblings = new Dictionary<ulong, ulong[]>();
            Dictionary<ulong, Package> packages = new Dictionary<ulong, Package>();

            foreach (ApplicationPackageManifest apm in (CASC.Root as OwRootHandler).APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (guids.Contains(GUID.LongKey(entry.PackageGUID)) || guids.Contains(GUID.Index(entry.PackageGUID)))
                    {
                        packages[entry.PackageGUID] = apm.Packages[i];
                        siblings[entry.PackageGUID] = apm.PackageSiblings[i];
                        records[entry.PackageGUID] = apm.Records[i];
                    }
                    totalRecords[entry.PackageGUID] = apm.Records[i];
                }
            }

            foreach (ulong key in records.Keys)
            {
                Save(output, key, records[key]);
                foreach (ulong sibling in siblings[key])
                {
                    if (totalRecords.ContainsKey(sibling))
                    {
                        Save(output, key, sibling, totalRecords[sibling]);
                    }
                }
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

            foreach (ApplicationPackageManifest apm in (CASC.Root as OwRootHandler).APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (guids.Contains(GUID.LongKey(entry.PackageGUID)) || guids.Contains(GUID.Index(entry.PackageGUID)))
                    {
                        packages[entry.PackageGUID] = apm.Packages[i];
                        records[entry.PackageGUID] = apm.Records[i];
                    }
                }
            }

            ICLIFlags flags = FlagParser.Parse<ExtractFlags>();
            MapCMF();
            LoadGUIDTable();
            Sound.WwiseBank.GetReady();
            DataTool.FindLogic.Combo.ComboInfo info = new DataTool.FindLogic.Combo.ComboInfo();
            foreach (ulong key in records.Keys)
            {
                string dest = Path.Combine(output, GUID.AsString(key));
                foreach (PackageRecord record in records[key])
                {
                    DataTool.FindLogic.Combo.Find(info, record.GUID);
                }
                DataTool.SaveLogic.Combo.Save(flags, dest, info);
            }
        }

        private static void Save(string output, ulong key, PackageRecord[] value) => Save(output, key, key, value);

        private static void Save(string output, ulong parentKey, ulong myKey, PackageRecord[] records)
        {
            string dest = Path.Combine(output, GUID.AsString(parentKey));
            if (myKey != parentKey)
            {
                dest = Path.Combine(dest, "sib", GUID.AsString(myKey));
            }

            foreach (PackageRecord record in records)
            {
                using (Stream file = OpenFile(record))
                {
                    string tmp = Path.Combine(dest, $"{GUID.Type(record.GUID):X3}");
                    if(!Directory.Exists(tmp))
                    {
                        Directory.CreateDirectory(tmp);
                    }
                    tmp = Path.Combine(tmp, GUID.AsString(record.GUID));
                    InfoLog("Saved {0}", tmp);
                    WriteFile(file, tmp);
                }
            }
        }

        private static void Search(string[] args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in (CASC.Root as OwRootHandler).APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    PackageRecord[] records = apm.Records[i];

                    foreach (PackageRecord record in records.Where(x => guids.Contains(x.GUID) || guids.Contains(GUID.Type(x.GUID)) || guids.Contains(GUID.Index(x.GUID))))
                    {
                        Log("Found {0} in package {1:X12}", GUID.AsString(record.GUID), GUID.LongKey(entry.PackageGUID));
                    }
                }
            }
        }

        private static void SearchType(string[] args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in (CASC.Root as OwRootHandler).APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    PackageRecord[] records = apm.Records[i];

                    foreach (PackageRecord record in records.Where(x => guids.Contains(GUID.Type(x.GUID))))
                    {
                        Log("Found {0} in package {1:X12}", GUID.AsString(record.GUID), GUID.LongKey(entry.PackageGUID));
                    }
                }
            }
        }

        private static void Info(string[] args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            foreach (ApplicationPackageManifest apm in (CASC.Root as OwRootHandler).APMFiles)
            {
                for (int i = 0; i < apm.PackageEntries.Length; ++i)
                {
                    PackageEntry entry = apm.PackageEntries[i];
                    if (guids.Contains(GUID.LongKey(entry.PackageGUID)) || guids.Contains(GUID.Index(entry.PackageGUID)))
                    {
                        Log("Package {0:X12}:", GUID.LongKey(entry.PackageGUID));
                        Log("\tUnknowns: {0}, {1}", entry.Unknown1, entry.Unknown2);
                        Log("\t{0} records", apm.Records[i].Length);
                        Log("\t{0} siblings", apm.PackageSiblings[i].Length);
                        foreach (ulong sibling in apm.PackageSiblings[i])
                        {
                            Log("\t\t{0}", GUID.AsString(sibling));
                        }
                    }
                }
            }
        }
    }
}
