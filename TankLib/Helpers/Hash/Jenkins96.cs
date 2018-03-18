using System;
using System.Security.Cryptography;
using System.Text;

namespace TankLib.Helpers.Hash {
    // Implementation of Bob Jenkins' hash function in C# (96 bit internal state)
    public class Jenkins96 : HashAlgorithm {
        private ulong _hashValue;
        private readonly byte[] _hashBytes = new byte[0];

        private static uint Rot(uint x, int k) {
            return (x << k) | (x >> (32 - k));
        }

        public ulong ComputeHash(string str, bool fix = true) {
            string tempstr = fix ? str.Replace('/', '\\').ToUpperInvariant() : str;
            byte[] data = Encoding.ASCII.GetBytes(tempstr);
            ComputeHash(data);
            return _hashValue;
        }

        public override void Initialize() {}

        protected override unsafe void HashCore(byte[] array, int ibStart, int cbSize) {
            uint length = (uint)array.Length;
            uint a = 0xdeadbeef + length;
            uint b = a;
            uint c = a;

            if (length == 0) {
                _hashValue = ((ulong)c << 32) | b;
                return;
            }

            uint newLen = (length + (12 - length % 12) % 12);

            if (length != newLen) {
                Array.Resize(ref array, (int)newLen);
                length = newLen;
            }

            fixed (byte* bb = array) {
                uint* u = (uint*)bb;

                for (int j = 0; j < length - 12; j += 12) {
                    a += *(u + j / 4);
                    b += *(u + j / 4 + 1);
                    c += *(u + j / 4 + 2);

                    a -= c; a ^= Rot(c, 4); c += b;
                    b -= a; b ^= Rot(a, 6); a += c;
                    c -= b; c ^= Rot(b, 8); b += a;
                    a -= c; a ^= Rot(c, 16); c += b;
                    b -= a; b ^= Rot(a, 19); a += c;
                    c -= b; c ^= Rot(b, 4); b += a;
                }

                uint i = length - 12;
                a += *(u + i / 4);
                b += *(u + i / 4 + 1);
                c += *(u + i / 4 + 2);

                c ^= b; c -= Rot(b, 14);
                a ^= c; a -= Rot(c, 11);
                b ^= a; b -= Rot(a, 25);
                c ^= b; c -= Rot(b, 16);
                a ^= c; a -= Rot(c, 4);
                b ^= a; b -= Rot(a, 14);
                c ^= b; c -= Rot(b, 24);

                _hashValue = ((ulong)c << 32) | b;
            }
        }

        protected override byte[] HashFinal() {
            return _hashBytes;
        }
    }
}