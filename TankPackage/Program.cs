using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DataTool;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using System.Threading.Tasks;
using TankLib;
using TACTLib.Core.Product.Tank;
using static TACTLib.Core.Product.Tank.ApplicationPackageManifest;

namespace TankPackage
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Flags = FlagParser.Parse<ToolFlags>();
            if (Flags == null)
            {
                return;
            }
            
            InitStorage();
            InitMisc();
            InitKeys();
            
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
            IOrderedEnumerable<ulong> unique = new HashSet<ulong>(TankHandler.Assets.Keys.Select(x => teResourceGUID.Attribute(x, teResourceGUID.AttributeEnum.Type))).OrderBy(x => x >> 48);

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

            ApplicationPackageManifest apm = TankHandler.PackageManifest;
            foreach (ContentManifestFile contentManifest in new [] {TankHandler.MainContentManifest, TankHandler.SpeechContentManifest}) {
                var ids = contentManifest.IndexMap.Where(x => guids.Length == 0 || guids.Contains(teResourceGUID.Type(x.Key))).Select(x => x.Key);
                Save(output, apm.Header.Checksum, ids, contentManifest);
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

            ApplicationPackageManifest apm = TankHandler.PackageManifest;
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

            var apm = TankHandler.PackageManifest;
            for (int i = 0; i < apm.PackageEntries.Length; ++i)
            {
                PackageEntry entry = apm.PackageEntries[i];
                if (!guids.Contains(teResourceGUID.LongKey(entry.PackageGUID)) && !guids.Contains(teResourceGUID.Index(entry.PackageGUID)) && !guids.Contains(entry.PackageGUID)) continue;
                packages[entry.PackageGUID] = apm.Packages[i];
                records[entry.PackageGUID] = apm.Records[i];
            }

            ICLIFlags flags = FlagParser.Parse<ExtractFlags>();

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
                using (Stream file = OpenFile(record.GUID)) {
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
        private static void Save(string output, ulong myKey, IEnumerable<ulong> records, ContentManifestFile cmf)
        {
            string dest = Path.Combine(output, teResourceGUID.AsString(myKey));

            void Body(ulong guid) {
                using (Stream file = cmf.OpenFile(Client, guid)) {
                    string tmp = Path.Combine(dest, $"{teResourceGUID.Type(guid):X3}");
                    if (!Directory.Exists(tmp)) {
                        Directory.CreateDirectory(tmp);
                    }

                    tmp = Path.Combine(tmp, teResourceGUID.AsString(guid));
                    InfoLog("Saved {0}", tmp);
                    WriteFile(file, tmp);
                }
            }

            Parallel.ForEach(records, Body);
        }

        private static void Search(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            ApplicationPackageManifest apm = TankHandler.PackageManifest;
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

        private static void SearchType(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            ApplicationPackageManifest apm = TankHandler.PackageManifest;
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

        private static void Info(IEnumerable<string> args)
        {
            ulong[] guids = args.Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();

            ApplicationPackageManifest apm = TankHandler.PackageManifest;
            for (int i = 0; i < apm.PackageEntries.Length; ++i)
            {
                PackageEntry entry = apm.PackageEntries[i];
                if (!guids.Contains(teResourceGUID.LongKey(entry.PackageGUID)) && !guids.Contains(teResourceGUID.Index(entry.PackageGUID))) continue;
                Log("Package {0:X12}:", teResourceGUID.LongKey(entry.PackageGUID));
                Log("\tUnknowns: {0}, {1}", entry.Unknown1, entry.Unknown2);
                Log("\t{0} records", apm.Records[i].Length);
            }
        }
    }
}
