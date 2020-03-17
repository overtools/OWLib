using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using Combo = DataTool.FindLogic.Combo;

namespace DataTool.ToolLogic.Dump {
    [Tool("extract-voice-sets", Description = "Extract voice sets", CustomFlags = typeof(ExtractFlags))]
    public class ExtractVoiceSets : JSONTool, ITool {
        private const string Container = "VoiceSets";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            Log("Saving all voice sets. This will take some time.");
            foreach (var key in TrackedFiles[0x5F]) {
                var stu = STUHelper.GetInstance<STUVoiceSet>(key);
                
                var guidClean = teResourceGUID.AsString(key);
                Log($"Saving VoiceSet: {guidClean}");
                
                var comboInfo = new Combo.ComboInfo();
                Combo.Find(comboInfo, key);
                SaveLogic.Combo.SaveVoiceSet(toolFlags, Path.Combine(basePath, Container), comboInfo, key);      
            }
        }
    }
}