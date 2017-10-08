using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using OWReplayLib.Serializer;

namespace OWReplayLib {
    public class Checksum : ReadableData {
        [Serializer.Types.Skip]
        private static readonly byte[] Key = {
            0x0C, 0x1A, 0xAB, 0xE8, 0xCC, 0xBF, 0x85, 0xBB, 0x77, 0x7B, 0xE2, 0xD0, 0xCB, 0x68, 0xD7, 0x35,
            0x75, 0x7C, 0x2F, 0x3A, 0x32, 0x96, 0xA4, 0x98, 0x57, 0x0F, 0xB3, 0x54, 0x56, 0x2F, 0xD5, 0x1C
        };

        [Serializer.Types.FixedSizeArray(typeof(byte), 32)]
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Data;

        public Checksum(string data) {
            Debug.Assert(data.Length == 64);
            Data = new byte[32];
            for (int i = 0; i < 32; ++i) {
                Data[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
            }
        }

        public Checksum(byte[] data) {
            Data = data;
        }
        
        public Checksum() {}

        public static Checksum Compute(byte[] input) {
            using (HMACSHA256 sha256 = new HMACSHA256(Key)) {
                return new Checksum(sha256.ComputeHash(input));
            }
        }

        public override string ToString() {
            return string.Join("", Data.Select(a => a.ToString("x2")));
        }

        public static implicit operator byte[](Checksum thisChecksum) {
            return thisChecksum.Data;
        }
    }
}