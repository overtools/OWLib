using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Helper;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;


namespace ReplayMp4Tool {
    public static class ReplayThing {
        private static string ProcessAtomsButDumber(Memory<byte> buffer) {
            var cursor = 0;
            while (cursor < buffer.Length) {
                var atom = new MP4Atom(buffer.Span.Slice(cursor));
                cursor += Math.Max(4, atom.Size);
                if (atom.Name == "ilst" || atom.Name == "?nam") {
                    return ProcessAtomsButDumber(atom.Buffer);
                }

                if (atom.Name == "data") {
                    return Encoding.UTF8.GetString(atom.Buffer.Slice(8).ToArray());
                }
            }

            return "";
        }
        

        private static IEnumerable<(string, string[])> ProcessAtoms(Memory<byte> buffer) {
            var cursor = 0;
            var filename = default(string);
            while (cursor < buffer.Length) {
                var atom = new MP4Atom(buffer.Span.Slice(cursor));
                cursor += atom.Size;
                if (atom.Name == "moov" || atom.Name == "udta") {
                    foreach (var (fn, str) in ProcessAtoms(atom.Buffer)) {
                        yield return (fn, str);
                    }

                    continue;
                }

                if (atom.Name == "meta") filename = ProcessAtomsButDumber(atom.Buffer);

                if (atom.Name != "Xtra") continue; // moov -> udta -> Xtra

                if (atom.Buffer.Length < 0x1F) {
                    Console.Out.WriteLine("\nReplay is fucked\n");
                    continue;
                }

                var localCursor = 0;
                var blockSize = BinaryPrimitives.ReadInt32BigEndian(atom.Buffer.Span);
                if (blockSize != atom.Buffer.Length) {
                    Console.Out.WriteLine("\nReplay has a lot of data?\n");
                }

                localCursor += 4;
                var blockNameLength = BinaryPrimitives.ReadInt32BigEndian(atom.Buffer.Span.Slice(localCursor));
                if (blockNameLength == 0) continue;
                localCursor += 4;
                var name = Encoding.ASCII.GetString(atom.Buffer.Span.Slice(localCursor, blockNameLength).ToArray());
                localCursor += blockNameLength;
                if (name != "WM/EncodingSettings") {
                    Console.Out.WriteLine("\nReplay is fucked\n");
                    continue;
                }

                var settingCount = BinaryPrimitives.ReadInt32BigEndian(atom.Buffer.Span.Slice(localCursor));
                localCursor += 4;
                for (var i = 0; i < settingCount; ++i) {
                    var encodedSettingLength = BinaryPrimitives.ReadInt32BigEndian(atom.Buffer.Span.Slice(localCursor));
                    if (encodedSettingLength == 0) continue;
                    var type = BinaryPrimitives.ReadInt16BigEndian(atom.Buffer.Span.Slice(localCursor + 4));
                    if (type != 8) {
                        Console.Out.WriteLine("\nNot Type 8?\n");
                    }

                    var data = atom.Buffer.Span.Slice(localCursor + 6, encodedSettingLength - 6);
                    var b64Str = Encoding.Unicode.GetString(data.ToArray());
                    localCursor += encodedSettingLength;

                    yield return (filename, b64Str.Split(':'));
                }
            }
        }

        public static List<Replay> ParseReplays(IEnumerable<string> files) {
            List<Replay> replays = new List<Replay>();
            foreach (var filePath in files) {
                replays.AddRange(ParseReplay(filePath));
            }
            return replays;
        }
        
        public static List<Replay> ParseReplay(string filePath) {
            Console.Out.WriteLine($"Processing file: {Path.GetFileName(filePath)}");
            List<Replay> replays = new List<Replay>();

            var buffer = (Memory<byte>) File.ReadAllBytes(filePath);
            if (buffer.Length == 0) return replays;

            foreach (var (filename, b64Str) in ProcessAtoms(buffer)) { // hash, payload, settinghash?
                byte[] bytes = Convert.FromBase64String(b64Str[1]);
                // string hex = BitConverter.ToString(bytes);

                var replayInfo = new Mp4Replay();
                replayInfo.Parse(bytes);

                var heroStu = STUHelper.GetInstance<STUHero>(replayInfo.Header.HeroGuid);
                var hero = new Hero(heroStu);
                var unlocks = new ProgressionUnlocks(heroStu);
                var skins = unlocks.GetUnlocksOfType(UnlockType.Skin);
                var skinTheme = skins.FirstOrDefault(skin => ((STUUnlock_SkinTheme) skin.STU)?.m_skinTheme == replayInfo.Header.SkinGuid);

                ulong mapHeaderGuid = (replayInfo.Header.MapGuid & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
                var mapData = new MapHeader(mapHeaderGuid);

                var replay = new Replay {
                    Title = filename,
                    Hero = hero.Name,
                    Map = mapData.Name,
                    Skin = skinTheme?.Name ?? "Unknown",
                    RecordedAt = $"{DateTimeOffset.FromUnixTimeSeconds(replayInfo.Header.Timestamp).ToLocalTime()}",
                    HighlightType = $"{replayInfo.Header.Type:G}",
                    Quality = $"{replayInfo.Header.QualityPct}% ({(ReplayQuality)replayInfo.Header.QualityPct})",
                    FilePath = filePath
            };

                replays.Add(replay);
            }
            return replays;
        }
        public struct Replay{
            public string Title, Hero, Map, Skin, RecordedAt, HighlightType, Quality, FilePath;
        }

        public class Mp4Replay
        {
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Structure {
                public teResourceGUID MapGuid;
                public teResourceGUID HeroGuid;
                public teResourceGUID SkinGuid;
                public long Timestamp;
                public ulong UserId;
                public ReplayType Type;
                public int QualityPct;
            }

            public Structure Header;

            public void Parse(byte[] bytes) {
                Header = FastStruct<Structure>.ArrayToStructure(bytes);
            }
        }

        public enum ReplayType {
            Highlight = 0,
            PlayOfTheGame = 2,
            ManualHighlight = 8
        }
        
        public enum ReplayQuality
        {
            Low = 30,
            Medium = 50,
            High = 80,
            Ultra = 100
        }
    }
}
