using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using DataTool.SaveLogic;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
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
            { 0xA367CA4E, "PostGameFlow" },
            // ow2 musics, unclassified
            { 0x2B058DFD, "2B058DFD" },
            { 0x4C1C986B, "4C1C986B" },
            { 0x8C529270, "8C529270" },
            { 0x65E10B23, "65E10B23" },
            { 0x85F1FDB8, "85F1FDB8" },
            { 0x730BC8EB, "730BC8EB" },
            { 0x981B9AB5, "981B9AB5" },
            { 0x1845EE31, "1845EE31" },
            { 0x56573649, "56573649" },
            { 0xA45CBFCA, "A45CBFCA" },
            { 0xC21B9447, "C21B9447" },
            { 0xEBB2F2DD, "EBB2F2DD" },
            { 0xEBB2F2DE, "EBB2F2DE" },
            { 0xF322E374, "F322E374" }
        };

        public void Parse(ICLIFlags toolFlags) {
            ExtractType(toolFlags);
        }

        public void ExtractType(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = Path.Combine(flags.OutputPath, "Music");
                flags.EnableSound = true;
            } else {
                throw new Exception("no output path");
            }

            foreach (ulong @ulong in TrackedFiles[0x2C]) {
                STUSound music = GetInstance<STUSound>(@ulong);
                if (music?.m_C32C2195 == null) {
                    continue;
                }

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