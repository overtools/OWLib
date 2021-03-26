using System;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using DataTool.SaveLogic;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using System.Collections.Generic;

namespace DataTool.ToolLogic.Extract.Debug
{
    [Tool("extract-music", Description = "Extracts sound files which are identified as music.", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]

    public class ExtractMusic : ITool
    {

        Dictionary<UInt32, string> music_types = new Dictionary<uint, string>
        {   { 3851462253, "LoadingScreen" },
            { 3043319348, "Retribution" },
            { 935766596, "Uprising"},
            { 1935561723, "StormRising1" },
            { 3069540790, "StormRising2"},
            { 1287967541, "Junkenstein"},
            { 3187645923, "PvP" },
            { 3991942870, "Stinger" },
            { 397648059,  "PvE"},
            { 2930411284, "MainMenuTheme"},
            { 251430280, "RoundNearEnd"}
        };

        public void Parse(ICLIFlags toolFlags)
        {
            ExtractType(toolFlags);
        }

        public void ExtractType(ICLIFlags toolFlags)
        {
            string basePath;
            if (toolFlags is ExtractFlags flags)
            {
                basePath = flags.OutputPath + "\\Music";
            }
            else
            {
                throw new Exception("no output path");
            }

            foreach (ulong @ulong in TrackedFiles[0x2C])
            {
                STUSound music = GetInstance<STUSound>(@ulong);
                var s_class = music.m_C32C2195.m_soundClass;
                if (music_types.ContainsKey(s_class))
                {     
                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                    var context = new Combo.SaveContext(info);
                    FindLogic.Combo.Find(info, @ulong);
                    SaveLogic.Combo.SaveAllSoundFiles(flags, Path.Combine(basePath, music_types[s_class]), context);
                    context.Wait();
                }
            }
        }
    }
}
