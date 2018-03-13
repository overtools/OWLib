using System.Diagnostics.CodeAnalysis;

namespace TankLib {
    public class teEnums {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum SDAM : long {
            NONE,
            MUTABLE,
            IMMUTABLE
        }
    }
}