using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DataTool;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using TACTLib.Client;
using TACTLib.Core.Product.Tank;
using TankLib;
using TankLib.Chunks;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace Mp4Tool {
    internal class Program {
        public static void Main(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: Mp4Tool {owverwatch dir} {file}");
                return;
            }
            
            string gameDir = args[0];
            string filePath = args[1];

            if (!filePath.EndsWith(".mp4")) {
                Console.Out.WriteLine("Only MP4s are supported");
                return;
            }

            ReplayThing.ParseReplay(gameDir, filePath);
        }
    }

    public static class ReplayThing {
        public static void ParseReplay(string gameDir, string filePath) {
            var searchString = "/EncodingSettings";
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            Seek(fs, searchString);
            while (fs.ReadByte() != (int) ':') {
                // we're just winding forward lol
            }
	
            var ogPosition = fs.Position;
            var endDotThingy = Seek(fs, ":");
            fs.Position = ogPosition;
            var end = endDotThingy - fs.Position - 1; // theres some crap at the end
            var buffer = new byte[end];
            fs.Read(buffer, 0, (int)(end));
	
            var b64Str = System.Text.Encoding.BigEndianUnicode.GetString(buffer);
	
            byte[] bytes = Convert.FromBase64String(b64Str);
            // string hex = BitConverter.ToString(bytes);
            
            var replayInfo = new Mp4Replay();
            replayInfo.Parse(bytes);

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
            
            var heroStu = GetInstance<STUHero>(replayInfo.Header.HeroGuid); 
            var hero = new Hero(heroStu);
            var unlocks = new ProgressionUnlocks(heroStu);
            var skins = unlocks.GetUnlocksOfType("Skin");
            var skinTheme = skins.FirstOrDefault(skin => ((STUUnlock_SkinTheme) skin.STU)?.m_skinTheme == replayInfo.Header.SkinGuid);
            
            ulong mapHeaderGuid = (replayInfo.Header.MapGuid & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
            var mapData = new MapHeader(mapHeaderGuid);

            Console.Out.WriteLine("\nReplay Info\n");
            Console.Out.WriteLine($"Hero: {hero.Name}");
            Console.Out.WriteLine($"Map: {mapData.Name}");
            Console.Out.WriteLine($"Skin: {skinTheme?.Name ?? "Unknown"}");
        }
        
        public class Mp4Replay {
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct Structure {
                public teResourceGUID MapGuid;
                public teResourceGUID HeroGuid;
                public teResourceGUID SkinGuid;
            }

            public Structure Header;

            public void Parse(byte[] bytes) {
                Header = FastStruct<Structure>.ArrayToStructure(bytes);
            }
        }
        
        public static long Seek(FileStream fs, string searchString) {
            char[] search = searchString.ToCharArray();
            long result = -1, position = 0, stored = -1,
                 begin = fs.Position;
            int c;

            while ((c = fs.ReadByte()) != -1) {
                if ((char)c == search[position]) {
                    if (stored == -1 && position > 0
                                     && (char)c == search[0]) {
                        stored = fs.Position;
                    }

                    if (position + 1 == search.Length) {
                        result = fs.Position - search.Length;
                        fs.Position = result;
                        break;
                    }

                    position++;
                }
                else if (stored > -1) {
                        fs.Position = stored + 1;
                        position = 1;
                    }
                    else {
                        position = 0;
                    }
            }
	
            if (result == -1) {
                fs.Position = begin;
            }

            return result;
        }
    }
}