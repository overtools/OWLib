using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.STU;
using TankLib.STU.Types;

namespace TankLibHelper.Modes {
    public class TestTypeClasses : IMode {
        public string Mode => "testtypeclasses";

        public Dictionary<ulong, ApplicationPackageManifest.Types.PackageRecord> Files;
        public Dictionary<ushort, HashSet<ulong>> Types;
        public CASCHandler CASC;

        public ModeResult Run(string[] args) {
            string gameDir = args[1];
            ushort type = ushort.Parse(args[2], NumberStyles.HexNumber);
            
            CASCConfig config = CASCConfig.LoadLocalStorageConfig(gameDir, true, false);
            CASC = CASCHandler.Open(config);
            MapCMF("enUS"); // heck

            foreach (ulong file in Types[type]) {
                string filename = teResourceGUID.AsString(file);
                using (Stream stream = OpenFile(file)) {
                    if (stream == null) continue;
                    teStructuredData structuredData = new teStructuredData(stream);

                    STUSkinTheme skinTheme = structuredData.GetMainInstance<STUSkinTheme>();
                    //if (skinTheme.m_50FDDF83 != null) {
                    //    continue;
                    //}

                    //STUGameRuleset ruleset = structuredData.GetMainInstance<STUGameRuleset>();
                    //if (ruleset.m_gamemode.m_gamemode == 157625986957967397) {
                    //    foreach (STUGameRulesetTeam team in ruleset.m_gamemode.m_teams) {
                    //        if (team.m_availableHeroes == null) continue;
                    //        if (team.m_availableHeroes is STU_C45DE560 heroSkinOverrides) {
                    //            foreach (teStructuredDataAssetRef<ulong> heroSkinOverride in heroSkinOverrides.m_62E537BD) {
                    //                STU_42270D59 skin = GetInst<STU_42270D59>(heroSkinOverride);
                    //                foreach (KeyValuePair<ulong,STU_3E88143F> rep in skin.m_258A7D5C) {
                    //                    Console.Out.WriteLine($"{teResourceGUID.AsString(rep.Key)} : {teResourceGUID.AsString(rep.Value.m_3D884507)}");
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }
            }
            
            return ModeResult.Success;
        }

        public teStructuredData GetStructuredData(ulong guid) {
            using (Stream stream = OpenFile(guid)) {
                if (stream == null) return null;
                return new teStructuredData(stream);
            }
        }
        
        public T GetInst<T>(ulong guid) where T : STUInstance {
            return GetStructuredData(guid)?.GetMainInstance<T>();
        }
        
        public Stream OpenFile(ulong guid) {
            return OpenFile(Files[guid]);
        }

        public Stream OpenFile(ApplicationPackageManifest.Types.PackageRecord record) {
            long offset = 0;
            EncodingEntry enc;
            if (record.Flags.HasFlag(ContentFlags.Bundle)) offset = record.Offset;
            if (!CASC.EncodingHandler.GetEntry(record.LoadHash, out enc)) return null;

            MemoryStream ms = new MemoryStream((int) record.Size);
            try {
                Stream fstream = CASC.OpenFile(enc.Key);
                fstream.Position = offset;
                fstream.CopyBytes(ms, (int) record.Size);
                ms.Position = 0;
            } catch (Exception e) {
                if (e is BLTEKeyException exception) {
                    Debugger.Log(0, "DataTool", $"[DataTool:CASC]: Missing key: {exception.MissingKey:X16}\r\n");
                }

                return null;
            }

            return ms;
        }
        
        public void MapCMF(string locale) {
            Files = new Dictionary<ulong, ApplicationPackageManifest.Types.PackageRecord>();
            Types = new Dictionary<ushort, HashSet<ulong>>();
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                const string searchString = "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + locale.ToLowerInvariant())) {
                    continue;
                }

                foreach (KeyValuePair<ulong, ApplicationPackageManifest.Types.PackageRecord> pair in apm.FirstOccurence) {
                    ushort type = teResourceGUID.Type(pair.Key);
                    if (!Types.ContainsKey(type)) {
                        Types[type] = new HashSet<ulong>();
                    }
                    
                    Types[type].Add(pair.Key);
                    Files[pair.Value.GUID] = pair.Value;
                }
            }
        }
    }
}