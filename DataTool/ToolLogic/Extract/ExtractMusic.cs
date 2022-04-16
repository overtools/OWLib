using System;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using DataTool.SaveLogic;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using System.Collections.Generic;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-music", Description = "Extracts sound files which are identified as music.", CustomFlags = typeof(ExtractFlags))]
    public class ExtractMusic : ITool {
        Dictionary<UInt32, string> music_types = new Dictionary<uint, string> {
            { 0xE590A66D, "LoadingScreen" },
            { 0xB5655E34, "Retribution" },
            { 0x37C6AA44, "Uprising" },
            { 0x735E53FB, "StormRising" },
            { 0xB6F579B6, "StormRising" },
            { 0x4CC4D335, "Junkenstein" },
            { 0xBDFF9DE3, "PvP" },
            { 0xEDF036D6, "Stinger" },
            { 0x17B3A0BB, "PvE" },
            { 0xAEAA8714, "MainMenuTheme" },
            { 0xDBB66679, "RoundNearEnd" },
            { 0xA367CA4E, "PostGameFlow" }
        };

        public void Parse(ICLIFlags toolFlags) {
            ExtractType(toolFlags);
        }

        public void ExtractType(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = Path.Combine(flags.OutputPath, "Music");
            } else {
                throw new Exception("no output path");
            }

            foreach (ulong @ulong in TrackedFiles[0x2C]) {
                STUSound music = GetInstance<STUSound>(@ulong);
                var s_class = music.m_C32C2195.m_wwiseBankID;
                if (music_types.ContainsKey(s_class)) {
                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                    var context = new Combo.SaveContext(info);
                    FindLogic.Combo.Find(info, @ulong);
                    SaveLogic.Combo.SaveAllSoundFiles(flags, Path.Combine(basePath, music_types[s_class]), context);
                }
            }
        }
    }
}