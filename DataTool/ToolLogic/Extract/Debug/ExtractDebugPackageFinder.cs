/*using System;
using System.Collections.Generic;
using DataTool.Flag;
using TankLib;
using TankLib.CASC;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-packagefinder", Description = "Test package loading (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugPackageFinder : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractVoiceSets(toolFlags);
        }

        public void FindStuff(HashSet<teResourceGUID> toLoad, ulong guid) {
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                for (int i = 0; i < apm.Header.PackageCount; i++) {
                    ApplicationPackageManifest.Types.Package package = apm.Packages[i];

                    bool shouldLoad = false;
                    foreach (ApplicationPackageManifest.Types.PackageRecord record in apm.Records[i]) {
                        if (record.GUID == guid) {
                            shouldLoad = true;
                        }
                    }
                    if (!shouldLoad) continue;
                    foreach (ApplicationPackageManifest.Types.PackageRecord record in apm.Records[i]) {
                        toLoad.Add((teResourceGUID) record.GUID);
                    }

                    //foreach (ulong sibling in apm.PackageSiblings[i]) {
                    //    teResourceGUID siblingResource = (teResourceGUID) sibling;
                    //    if (toLoad.Add(siblingResource)) {
                    //        FindStuff(toLoad, siblingResource);
                    //    }
                    //}
                }
            }
        }

        public void ExtractVoiceSets(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            //const string container = "DebugVoiceSet";

            ulong loadGuid = 0x400000000000001;
            HashSet<teResourceGUID> loadedAssets = new HashSet<teResourceGUID>();
            
            FindStuff(loadedAssets, loadGuid);
            
            //foreach (ulong key in Program.TrackedFiles[0x5F]) {
            //    if (teResourceGUID.Index(key) != 0x19F) continue;
            //    STUVoiceSet voiceSet = STUHelper.GetInstance<STUVoiceSet>(key);
            //    string voiceMaterDir = Path.Combine(basePath, container, IO.GetFileName(key));
            //    Combo.ComboInfo info = new Combo.ComboInfo();
            //    Combo.Find(info, key);
            //    SaveLogic.Combo.SaveVoiceSet(flags, voiceMaterDir, info, key);
            //    // foreach (STUVoiceLineInstance voiceLineInstance in voiceSet.VoiceLineInstances) {
            //    //     if (voiceLineInstance?.SoundDataContainer == null) continue;
            //    //     
            //    //     Combo.ComboInfo info = new Combo.ComboInfo();
            //    //
            //    //     Combo.Find(info, voiceLineInstance.SoundDataContainer.SoundbankMasterResource);
            //    //
            //    //     foreach (ulong soundInfoNew in info.Sounds.Keys) {
            //    //         SaveLogic.Combo.SaveSound(flags, voiceMaterDir, info, soundInfoNew);
            //    //     }
            //    // }
            //}
        }
    }
}*/