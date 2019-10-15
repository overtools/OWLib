using System.Globalization;
using System.IO;
using TankLib;
using TankLib.STU;
using TACTLib.Client;
using TACTLib.Core.Product.Tank;

namespace TankLibHelper.Modes {
    public class TestTypeClasses : IMode {
        public string Mode => "testtypeclasses";

        private ProductHandler_Tank _tankHandler;

        public ModeResult Run(string[] args) {
            string gameDir = args[1];
            ushort type = ushort.Parse(args[2], NumberStyles.HexNumber);
            
            ClientCreateArgs createArgs = new ClientCreateArgs {
               SpeechLanguage = "enUS",
               TextLanguage = "enUS"
            };
            
            TankLib.TACT.LoadHelper.PreLoad();
            ClientHandler client = new ClientHandler(gameDir, createArgs);
            _tankHandler = (ProductHandler_Tank)client.ProductHandler;
            TankLib.TACT.LoadHelper.PostLoad(client);

            foreach (var asset in _tankHandler.Assets) {
                if (teResourceGUID.Type(asset.Key) != type) continue;
                string filename = teResourceGUID.AsString(asset.Key);
                using (Stream stream = _tankHandler.OpenFile(asset.Key)) {
                    if (stream == null) continue;
                    using (var stu = new teStructuredData(stream)) {
                        var inst = stu.GetMainInstance<STUInstance>();
                    }
                }
            }
            
            return ModeResult.Success;
        }

        public teStructuredData GetStructuredData(ulong guid) {
            using (Stream stream = _tankHandler.OpenFile(guid)) {
                if (stream == null) return null;
                return new teStructuredData(stream);
            }
        }

        public string GetString(ulong guid) {
            if (guid == 0) return null;
            using (Stream stream = _tankHandler.OpenFile(guid)) {
                if (stream == null) return null;
                return new teString(stream);
            }
        }
        
        public T GetInst<T>(ulong guid) where T : STUInstance {
            using (var stu = GetStructuredData(guid)) {
                return stu?.GetMainInstance<T>();
            }
        }
    }
}