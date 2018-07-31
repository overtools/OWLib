using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace TankLib.Replay
{
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 32)]
    public unsafe struct ReplayChecksum
    {
        private static readonly byte[] Key = {
            0x0C, 0x1A, 0xAB, 0xE8, 0xCC, 0xBF, 0x85, 0xBB, 0x77, 0x7B, 0xE2, 0xD0, 0xCB, 0x68, 0xD7, 0x35,
            0x75, 0x7C, 0x2F, 0x3A, 0x32, 0x96, 0xA4, 0x98, 0x57, 0x0F, 0xB3, 0x54, 0x56, 0x2F, 0xD5, 0x1C
        };

        [FieldOffset(0)]
        public fixed byte Data[32];

        public ReplayChecksum(string data)
        {
            fixed (byte* ptr = Data)
            {
                for (int i = 0; i < 32; ++i)
                {
                    ptr[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
                }
            }
        }

        public ReplayChecksum(byte[] data)
        {
            if (data.Length != 32)
            {
                return;
            }

            fixed (byte* ptr = Data)
            {
                Marshal.Copy(data, 0, (IntPtr)ptr, 32);
            }
        }

        public static ReplayChecksum Compute(byte[] input)
        {
            using (HMACSHA256 sha256 = new HMACSHA256(Key))
            {
                return new ReplayChecksum(sha256.ComputeHash(input));
            }
        }

        public override string ToString()
        {
            StringBuilder bob = new StringBuilder(64);
            fixed (byte* ptr = Data)
            {
                for (int i = 0; i < 32; ++i)
                {
                    bob.Append(ptr[i].ToString("X2"));
                }
            }
            return bob.ToString();
        }

        public static implicit operator byte[] (ReplayChecksum that)
        {
            byte[] b = new byte[32];
            Marshal.Copy((IntPtr)that.Data, b, 0, 32);
            return b;
        }
    }
}
