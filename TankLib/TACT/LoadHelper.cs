using TACTLib.Client;

namespace TankLib.TACT {
    public static class LoadHelper {
        private static bool _loggerInitialized;
        
        public static void PreLoad() {
            if (_loggerInitialized) return;
            _loggerInitialized = true;
            
            TACTLib.Logger.OnInfo += (category, message) => Helpers.Logger.Info(category, message);
            TACTLib.Logger.OnDebug += (category, message) => Helpers.Logger.Debug(category, message);
            TACTLib.Logger.OnWarn += (category, message) => Helpers.Logger.Warn(category, message);
            TACTLib.Logger.OnError += (category, message) => Helpers.Logger.Error(category, message);
        }
        
        public static void PostLoad(ClientHandler clientHandler) {
        }
    }
}
