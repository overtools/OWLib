using System.Collections.Generic;

namespace TankLib.CASC {
    /// <summary>MD5 Hash</summary>
    public unsafe struct MD5Hash {
        public fixed byte Value[16];
        
        public string ToHexString() {
            byte[] array = new byte[16];

            fixed (byte* aptr = array) {
                *(MD5Hash*)aptr = this;
            }

            return array.ToHexString();
        }
    }

    public class MD5HashComparer : IEqualityComparer<MD5Hash> {
        private const uint FnvPrime32 = 16777619;
        private const uint FnvOffset32 = 2166136261;

        public unsafe bool Equals(MD5Hash x, MD5Hash y) {
            for (int i = 0; i < 16; ++i)
                if (x.Value[i] != y.Value[i])
                    return false;

            return true;
        }

        public int GetHashCode(MD5Hash obj) {
            return To32BitFnv1aHash(obj);
        }

        private static unsafe int To32BitFnv1aHash(MD5Hash toHash) {
            uint hash = FnvOffset32;

            uint* ptr = (uint*) &toHash;

            for (int i = 0; i < 4; i++) {
                hash ^= ptr[i];
                hash *= FnvPrime32;
            }

            return unchecked((int) hash);
        }
    }
}