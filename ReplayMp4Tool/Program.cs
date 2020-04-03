using System;
using System.Collections.Generic;
using System.IO;
using DataTool;

namespace ReplayMp4Tool {
    internal class Program {
        public static void Main(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: Mp4Tool {owverwatch dir} {file}");
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
            
            const string locale = "enUS";

            DataTool.Program.Flags = new ToolFlags {
                OverwatchDirectory = gameDir,
                Language = locale,
                SpeechLanguage = locale,
                UseCache = true,
                CacheCDNData = true,
                Quiet = true
            };

            DataTool.Program.InitStorage(false);

            ReplayThing.ParseReplays(files);
        }
    }
}