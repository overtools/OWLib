using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using TankLib;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-newents", Description = "Extract new entities (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugNewEntities : ITool {
        public void Parse(ICLIFlags toolFlags) {
            ExtractNewEntities(toolFlags);
        }

        public void AddNewByGUID(Combo.ComboInfo info, HashSet<ulong> lastVerGuids, params ushort[] types) {
            foreach (ushort type in types) {
                foreach (ulong key in TrackedFiles[type]) {
                    if (lastVerGuids.Contains(key)) continue;
                    Combo.Find(info, key);
                }
            }
        }

        public void AddNewByContentHash(Combo.ComboInfo info, HashSet<CKey> contentHashes, params ushort[] types) {
            foreach (KeyValuePair<ulong, ProductHandler_Tank.Asset> asset in TankHandler.m_assets) {
                TankHandler.UnpackAsset(asset.Value, out var package, out var record);

                ushort fileType = teResourceGUID.Type(asset.Key);
                if (fileType == 0x9C) continue; // bundle
                if (fileType == 0x77) continue; // package

                if (!types.Contains(fileType)) continue;

                var cmf = TankHandler.GetContentManifestForAsset(asset.Key);
                if (!cmf.TryGet(record.m_GUID, out var cmfData)) {
                    //throw new FileNotFoundException();
                    // todo: wtf
                    continue;
                }

                if (contentHashes.Contains(cmfData.ContentKey)) continue;

                if (fileType == 0x4) {
                    var locale = teResourceGUID.Locale(asset.Key);
                    if (locale == 0xF) continue; // ? 
                    if (locale == 0x1F) continue; // ? 
                    if (locale == 0x2F) continue; // ? 
                    if (locale == 0x3F) continue; // ? 
                    if (locale == 0x4F) continue; // ? 
                    if (locale == 0x5F) continue; // ? 
                    if (teResourceGUID.Platform(asset.Key) == 0x8) continue; // effect images
                }

                Combo.Find(info, asset.Key);
            }
        }

        public static HashSet<CKey> GetContentHashes(string path) {
            using (var stream = File.OpenRead(path))
                return Diff.ReadCKeys(stream);
        }

        public static HashSet<ulong> GetGUIDs(string path) {
            using (var stream = File.OpenRead(path))
                return Diff.ReadGUIDs(stream);
        }

        public void ExtractNewEntities(ICLIFlags toolFlags) {
            string basePath;

            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            if (flags.Positionals.Length != 4) {
                throw new Exception("incorrect params provided");
            }

            var file = flags.Positionals[3];
            const string container = "DebugNewEntities2";
            Combo.ComboInfo info = new Combo.ComboInfo();
            var types = new ushort[] {0x4, 0x7C, 0x3F, 0xB2};

            if (file.EndsWith("guids")) {
                var guids = GetGUIDs(file);
                AddNewByGUID(info, guids, types);
            } else if (file.EndsWith("cmfhashes")) {
                // does this even still work?
                var contentHashes = GetContentHashes(file);
                AddNewByContentHash(info, contentHashes, types);
            } else {
                throw new Exception("unknown file type");
            }

            var context = new SaveLogic.Combo.SaveContext(info);
            SaveLogic.Combo.Save(flags, Path.Combine(basePath, container), context);
            SaveLogic.Combo.SaveAllSoundFiles(flags, Path.Combine(basePath, container, "Sounds"), context);
            SaveLogic.Combo.SaveAllVoiceSoundFiles(flags, Path.Combine(basePath, container, "VoiceSounds"), context);
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, container, "LooseTex"), context);
            SaveLogic.Combo.SaveAllStrings(flags, Path.Combine(basePath, container, "Strings"), info);
        }
    }
}
