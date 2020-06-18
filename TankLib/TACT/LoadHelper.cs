using System.IO;
using System.Reflection;
using TACTLib.Client;
using TACTLib.Core.Product.Tank;

namespace TankLib.TACT {
    public static class LoadHelper {
        public static void PreLoad() {
            TACTLib.Logger.OnInfo += (category, message) => Helpers.Logger.Info(category, message);
            TACTLib.Logger.OnDebug += (category, message) => Helpers.Logger.Debug(category, message);
            TACTLib.Logger.OnWarn += (category, message) => Helpers.Logger.Warn(category, message);
            TACTLib.Logger.OnError += (category, message) => Helpers.Logger.Error(category, message);
        }
        
        public static void PostLoad(ClientHandler clientHandler) {
        }
    }
}
