using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.ConvertLogic.WEM;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using Spectre.Console;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-music-new", Description = "Extracts every sound file classified as music", CustomFlags = typeof(ExtractFlags))]
    public class ExtractMusicNew : ITool {
        private const string Container = "MusicNew";

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();
            var outputPath = Path.Combine(flags.OutputPath, Container);

            AnsiConsole.Progress().Start(context => ExtractMusic(context, flags, outputPath));
        }

        private static void ExtractMusic(ProgressContext context, ExtractFlags flags, string outputPath) {
            var allBankGUIDs = Program.TrackedFiles[0x43];
            var allSoundGUIDs = Program.TrackedFiles[0x2C];
            var banksTask = context.AddTask("Scanning SoundBanks", true, allBankGUIDs.Count);
            var soundsTask = context.AddTaskAfter("Scanning Sounds", banksTask, true, allSoundGUIDs.Count);
            var extractingTask = context.AddTaskAfter("Extracting", soundsTask);

            // many sounds share each bank.
            // it's not good enough to say "this bank contains some music"
            // we need to identify exactly which tracks are music
            var musicIDs = new HashSet<uint>();
            foreach (ulong bankGUID in allBankGUIDs) {
                banksTask.Increment(1);

                using var stream = IO.OpenFile(bankGUID);
                if (stream == null) continue;

                WwiseBank bank;
                try {
                    bank = new WwiseBank(stream);
                } catch (Exception e) {
                    Console.Out.WriteLine($"todo err: {e}");
                    continue;
                }

                foreach (var musicTrack in bank.ObjectsOfType<BankObjectMusicTrack>()) {
                    foreach (var musicSource in musicTrack.Sources) {
                        musicIDs.Add(musicSource.Media.SourceID);
                    }
                }
            }
            banksTask.StopTask();

            var findInfo = new Combo.ComboInfo();
            foreach (var soundGUID in allSoundGUIDs) {
                soundsTask.Increment(1);

                var sound = STUHelper.GetInstance<STUSound>(soundGUID);
                if (sound == null) continue;

                var soundIsMusic =
                    sound.m_C32C2195.m_wwiseWEMFileIDs != null && musicIDs.Overlaps(sound.m_C32C2195.m_wwiseWEMFileIDs) ||
                    sound.m_C32C2195.m_wwiseWEMStreamIDs != null && musicIDs.Overlaps(sound.m_C32C2195.m_wwiseWEMStreamIDs);
                if (!soundIsMusic) {
                    continue;
                }

                Combo.Find(findInfo, soundGUID);
            }
            soundsTask.StopTask();

            extractingTask.MaxValue = findInfo.m_soundFiles.Count;
            var saveContext = new SaveLogic.Combo.SaveContext(findInfo);
            foreach (var soundFileGUID in findInfo.m_soundFiles.Keys) {
                SaveLogic.Combo.SaveSoundFile(flags, outputPath, saveContext, soundFileGUID, false);
                extractingTask.Increment(1);
            }
            extractingTask.StopTask();
        }
    }
}