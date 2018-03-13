using System;

namespace CMFLib {
    public static class Extensions {
        public static byte[] ToByteArray(this string str) {
            str = str.Replace(" ", string.Empty);

            byte[] res = new byte[str.Length / 2];
            for (int i = 0; i < res.Length; ++i) res[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);

            return res;
        }
    }
}