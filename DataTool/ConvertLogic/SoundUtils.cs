using System.Collections.Generic;
using System.Linq;

namespace DataTool.ConvertLogic;

internal static class SoundUtils {
    internal static bool ArraysEqual(IReadOnlyCollection<byte> a1, IReadOnlyList<byte> a2) {
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Count != a2.Count)
            return false;

        EqualityComparer<byte> comparer = EqualityComparer<byte>.Default;
        return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
    }

    internal static uint SwapBytes(uint x) {
        // swap adjacent 16-bit blocks
        x = (x >> 16) | (x << 16);
        // swap adjacent 8-bit blocks
        return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
    }

    internal static ushort SwapBytes(ushort x) => (ushort) SwapBytes((uint) x);
}