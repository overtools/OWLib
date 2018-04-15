using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using DataTool.Flag;
using DataTool.Helper;
using TankLib.CASC;
using VersionManager.Modes;

namespace VersionManager {
    internal class Program {
        public static ToolFlags Flags;
        
        public static void Main(string[] args) {
            Flags = FlagParser.Parse<ToolFlags>();
            if (Flags == null) {
                return;
            }

            #region Initialize CASC
            Log("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, TankLib.Util.GetVersion(typeof(Program).Assembly));
            Log("Initializing CASC...");
            if (Flags.Language != null) {
                Log("Set language to {0}", Flags.Language);
            }
            if (Flags.SpeechLanguage != null) {
                Log("Set speech language to {0}", Flags.SpeechLanguage);
            }
            // ngdp:us:pro
            // http:us:pro:us.patch.battle.net:1119
            if (Flags.OverwatchDirectory.ToLowerInvariant().Substring(0, 5) == "ngdp:") {
                string cdn = Flags.OverwatchDirectory.Substring(5, 4);
                string[] parts = Flags.OverwatchDirectory.Substring(5).Split(':');
                string region = "us";
                string product = "pro";
                if (parts.Length > 1)
                {
                    region = parts[1];
                }
                if (parts.Length > 2)
                {
                    product = parts[2];
                }
                //if (cdn == "bnet") {
                //    Config = CASCConfig.LoadOnlineStorageConfig(product, region);
                //} else {
                //    if (cdn == "http") {
                //        string host = string.Join(":", parts.Skip(3));
                //        Config = CASCConfig.LoadOnlineStorageConfig(host, product, region, true, true, true);
                //    }
                //}
            } else {
                DataTool.Program.Config = CASCConfig.LoadLocalStorageConfig(Flags.OverwatchDirectory, true, false);
            }
            DataTool.Program.Config.SpeechLanguage = Flags.SpeechLanguage ?? Flags.Language ?? DataTool.Program.Config.SpeechLanguage;
            DataTool.Program.Config.TextLanguage = Flags.Language ?? DataTool.Program.Config.TextLanguage;
            #endregion

            DataTool.Program.BuildVersion = uint.Parse(DataTool.Program.Config.BuildName.Split('.').Last());

            Log("Using Overwatch Version {0}", DataTool.Program.Config.BuildName);
            DataTool.Program.CASC = CASCHandler.Open(DataTool.Program.Config);
            DataTool.Program.Root = DataTool.Program.CASC.RootHandler;
            
            IO.MapCMF();

            if (Flags.Mode == "create") { // create data
                CreateFlags createFlags = FlagParser.Parse<CreateFlags>();
                Modes.Create create = new Create();
                create.Run(createFlags);
            } else {
                
            }
        }
        public static void Log(string message, params object[] args) {
            Console.Out.WriteLine(message, args);
        }
    }
}