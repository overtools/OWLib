using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.SaveLogic;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using Model = DataTool.FindLogic.Model;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-ent", Description = "Extract entities (debug)", TrackTypes = new ushort[] {0x3}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugEntities : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ResearchEntities(toolFlags);
        }

        public void ResearchEntities(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            foreach (ulong key in TrackedFiles[0x3]) {
                STUEntityDefinition entity = GetInstance<STUEntityDefinition>(key);
                if (entity == null) continue;

                HashSet<ModelInfo> models = Model.FindModels(null, new Common.STUGUID(key));
                foreach (ModelInfo model in models) {
                    CreateDirectoryFromFile(Path.Combine(basePath, GetFileName(key), "jeffK"));
                    // SaveLogic.Model.Save(toolFlags, Path.Combine(basePath, GetFileName(key), "Models"), model, $"Entity {GUID.Index(key):X} Model {GUID.Index(model.GUID):X}");
                    // Entity.Save(flags, Path.Combine(basePath, GetFileName(key), "Entities"), model.Entities.Values);
                }
            }
        }
    }
}