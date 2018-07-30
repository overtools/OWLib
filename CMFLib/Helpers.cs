namespace CMFLib {
    public static class Helpers {
        // ReSharper disable once InconsistentNaming
        internal const uint SHA1_DIGESTSIZE = 20;
        
        internal static uint Constrain(long value) {
            return (uint)(value % uint.MaxValue);
        }
        
        internal static long SignedMod(long a, long b) {
            return a % b < 0 ? a % b + b : a % b;
        }
    }
}