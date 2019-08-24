using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TankLib;
using TankLib.STU;
using TACTLib;
using TACTLib.Client;
using TACTLib.Core.Product.Tank;

namespace TankLibHelper.Modes {
    public class FindPrimaryClassTact : IMode {
        public ModeResult Run(string[] args) {
            if (args.Length < 3) {
                Console.Out.WriteLine("Missing required arg: \"overwatch dir\" types...");
                return ModeResult.Fail;
            }
            string gameDir = args[1];
            ushort[] types = args.Skip(2).Select(x => ushort.Parse(x, NumberStyles.HexNumber)).ToArray();
            
            ClientCreateArgs createArgs = new ClientCreateArgs {
                SpeechLanguage = "enUS",
                TextLanguage = "enUS"
            };
            
            TankLib.TACT.LoadHelper.PreLoad();
            ClientHandler client = new ClientHandler(gameDir, createArgs);
            var handler = (ProductHandler_Tank)client.ProductHandler;
            TankLib.TACT.LoadHelper.PostLoad(client);

            foreach (var asset in handler.Assets) {
                if (!types.Contains(teResourceGUID.Type(asset.Key))) continue;
                string filename = teResourceGUID.AsString(asset.Key);
                using (Stream stream = handler.OpenFile(asset.Key)) {
                    try {
                        if (stream == null) throw new Exception();
                        using (var structuredData = new teStructuredData(stream)) {
                            var primary = structuredData.Instances.FirstOrDefault(x => x.Usage == TypeUsage.Root) ?? structuredData.Instances.FirstOrDefault();

                            if (primary == default) throw new Exception();
                        
                            Logger.Info(null, $"{filename}: {primary.GetType().Name}");
                        }
                    } catch {
                        Logger.Warn(null, $"Can't find root instance for {filename}");
                    }
                }
            }
            
            return ModeResult.Success;
        }

        public string Mode => "primary-tact";
    }
}
