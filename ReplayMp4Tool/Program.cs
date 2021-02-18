using System;
using System.Collections.Generic;
using System.IO;
using DataTool;
using DataTool.Flag;
using DataTool.ToolLogic.List;
using DataTool.JSON;

namespace ReplayMp4Tool {
    internal class Program {
        public static void Main(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: Mp4Tool {overwatch dir} {file} [--json] [--out=outfile.json]");
                return;
            }

            string gameDir = args[0];
            string filePath = args[1];

            var files = new List<string>();
            var fileAttributes = File.GetAttributes(filePath);

            if (fileAttributes.HasFlag(FileAttributes.Directory)) {
                files.AddRange(Directory.GetFiles(filePath, "*.mp4", SearchOption.TopDirectoryOnly));

                if (files.Count == 0) {
                    Console.Out.WriteLine("Found no valid mp4 files.");
                    return;
                }
            } else {
                if (!filePath.EndsWith(".mp4")) {
                    Console.Out.WriteLine("Only MP4s are supported");
                    return;
                }

                files.Add(filePath);
            }

            var flags = FlagParser.Parse<ListFlags>();
            var toolFlags = FlagParser.Parse<ToolFlags>();

            DataTool.Program.Flags = new ToolFlags {
                OverwatchDirectory = gameDir,
                Language = toolFlags.Language ?? "enUS",
                SpeechLanguage = toolFlags.SpeechLanguage ?? "enUS",
                Quiet = true,
                Online = false
            };

            DataTool.Program.InitStorage(false);
            var replays = ReplayThing.ParseReplays(files);

            if (flags.JSON){
                new JSONTool().OutputJSON(replays, flags);
            } else {
                foreach (ReplayThing.Replay replay in replays){
                    Console.Out.WriteLine("Replay Info:");
                    Console.Out.WriteLine($" - Title: {replay.Title}");
                    Console.Out.WriteLine($" - Hero: {replay.Hero}");
                    Console.Out.WriteLine($" - Map: {replay.Map}");
                    Console.Out.WriteLine($" - Skin: {replay.Skin}");
                    Console.Out.WriteLine($" - Recorded At: {replay.RecordedAt}");
                    Console.Out.WriteLine($" - Type: {replay.HighlightType}");
                    Console.Out.WriteLine($" - Quality: {replay.Quality})");
                    Console.Out.WriteLine("\n");
                }
            }
        }
    }
}