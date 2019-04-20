using System;
using System.IO;
using System.Runtime.InteropServices;
using DataTool.Flag;
using TankLib;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-movies", Description = "Extract movies (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMovies : ITool {
        public void Parse(ICLIFlags toolFlags) {
            ExtractMOVI(toolFlags);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOVI
        {
            public uint Magic;
            public uint Version;
            public ushort Unknown1;
            public ushort Flags;
            public uint Width;
            public uint Height;
            public uint Depth;
            public ulong MasterAudio;
            public ulong ExtraAudio;
        }

        private static string FFMPEG = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party", "ffmpeg.exe"));
        private static bool HAS_FFMPEG = File.Exists(FFMPEG);

        public void ExtractMOVI(ICLIFlags toolFlags) {
            string basePath;
            ExtractFlags flags = toolFlags as ExtractFlags;
            basePath = flags?.OutputPath;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new Exception("no output path");
            }

            const string container = "DebugMovies";
            
            foreach (ulong key in Program.TrackedFiles[0xB6]) {
                using (Stream videoStream = OpenFile(key)) {
                    if (videoStream != null) {
                        using (BinaryReader reader = new BinaryReader(videoStream))
                        {
                            MOVI movi = reader.Read<MOVI>();
                            videoStream.Position = 128;  // wrapped in "MOVI" for some reason
                            string videoFile = Path.Combine(basePath, container, teResourceGUID.LongKey(key).ToString("X12"), $"{teResourceGUID.LongKey(key):X12}.bk2");
                            WriteFile(videoStream, videoFile);
                            FindLogic.Combo.ComboInfo audioInfo = new FindLogic.Combo.ComboInfo
                            {
                                SoundFiles = new System.Collections.Generic.Dictionary<ulong, FindLogic.Combo.SoundFileInfo>
                                {
                                    { movi.MasterAudio, new FindLogic.Combo.SoundFileInfo(movi.MasterAudio) }
                                }
                            };
                            SaveLogic.Combo.SaveSoundFile(flags, Path.Combine(basePath, container, teResourceGUID.LongKey(key).ToString("X12")), audioInfo, movi.MasterAudio, false);
                        }
                    }
                }
            }
        }
    }
}