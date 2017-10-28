using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-lootbox", Description = "Extract lootbox models", TrackTypes = new ushort[] {0xCF}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractLootbox : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetLootboxes(toolFlags);
        }

        public void GetLootboxes(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            
            foreach (ulong key in TrackedFiles[0xCF]) {
                STULootbox lootbox = GetInstance<STULootbox>(key);
                if (lootbox == null) continue;
                
                string name = GetValidFilename(lootbox.Event.ToString()) ?? $"Unknown{GUID.Index(key):X}";
                
                HashSet<ModelInfo> models = new HashSet<ModelInfo>();
                models = Model.FindModels(models, lootbox.StateScriptComponent);
                models = Model.FindModels(models, lootbox.Effect1);
                models = Model.FindModels(models, lootbox.Effect2);
                models = Model.FindModels(models, lootbox.Effect3);
                models = Model.FindModels(models, lootbox.Effect4);
                models = Model.FindModels(models, lootbox.Material);
                models = Model.FindModels(models, lootbox.Material2);

                foreach (ModelInfo model in models) {
                    SaveLogic.Model.Save(flags, Path.Combine(basePath, name), model, $"Lootbox {lootbox.Event}_{GUID.Index(model.GUID):X}");
                }
            }
        }
    }
}