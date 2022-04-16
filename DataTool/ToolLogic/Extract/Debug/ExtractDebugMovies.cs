using System;
using System.IO;
using System.Runtime.InteropServices;
using DataTool.Flag;
using DataTool.SaveLogic;
using TankLib;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-movies", Description = "Extract movies (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMovies : ITool {
        public void Parse(ICLIFlags toolFlags) {
            ExtractMOVI(toolFlags);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOVI {
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
            if (string.IsNullOrWhiteSpace(basePath)) {
                throw new Exception("no output path");
            }

            const string container = "DebugMovies";

            foreach (ulong key in Program.TrackedFiles[0xB6]) {
                SaveVideoFile(flags, key, Path.Combine(basePath, container, teResourceGUID.LongKey(key).ToString("X12")));
            }
        }

        public static void SaveVideoFile(ICLIFlags flags, ulong guid, string directory) {
            using (Stream videoStream = OpenFile(guid)) {
                if (videoStream == null) return;

                using (BinaryReader reader = new BinaryReader(videoStream)) {
                    MOVI movi = reader.Read<MOVI>();
                    videoStream.Position = 128;

                    string videoFile = Path.Combine(directory, $"{teResourceGUID.LongKey(guid):X12}.bk2");
                    WriteFile(videoStream, videoFile);

                    FindLogic.Combo.ComboInfo audioInfo = new FindLogic.Combo.ComboInfo();
                    FindLogic.Combo.Find(audioInfo, movi.MasterAudio);
                    FindLogic.Combo.Find(audioInfo, movi.ExtraAudio);

                    var audioContext = new Combo.SaveContext(audioInfo);
                    Combo.SaveAllSoundFiles(flags, directory, audioContext);
                }
            }
        }
    }
}
