using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CRCReverse {
    public class Crc32 {
        public readonly uint[] Table;

        public uint ComputeChecksum(IEnumerable<byte> bytes) {
            uint crc = 0xffffffff;
            foreach (byte t in bytes) {
                byte index = (byte)((crc & 0xff) ^ t);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }

        public byte[] ComputeChecksumBytes(IEnumerable<byte> bytes) {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }

        public Crc32(uint poly=0xedb88320) {
            Table = new uint[256];
            for(uint i = 0; i < Table.Length; ++i) {
                uint temp = i;
                for(int j = 8; j > 0; --j) {
                    if((temp & 1) == 1) {
                        temp = (temp >> 1) ^ poly;
                    }else {
                        temp >>= 1;
                    }
                }
                Table[i] = temp;
            }
        }
    }

    internal class Program {
        public static void Main(string[] args) {
            Dictionary<uint, string> knownValues = new Dictionary<uint, string> {  // these should all work
                {0x56B6D12E, "STULootbox".ToLowerInvariant()},
                {0x0CC07049, "STUAchievement".ToLowerInvariant()},
                {0xC6A72877, "STUUnlock_Pose".ToLowerInvariant()},
                {0xC23F89EB, "STUUnlock_Weapon".ToLowerInvariant()},
                {0x614BC677, "STUUnlock_Currency".ToLowerInvariant()},
                {0x0B517D2E, "STUUnlock_Emote".ToLowerInvariant()},
                {0x6760479E, "STUUnlock".ToLowerInvariant()},
                {0xBB99FCD3, "m_rarity"},
                {0xB48F1D22, "m_name"},
                {0x3446F580, "m_description"},
                {0xF1CB3BA0, "m_text"},
                {0x2C01908B, "m_level"},
                {0x78A2AC5C, "m_stars"},
                {0x8F736177, "m_rank"},
                {0x7236F6E3, "STUStatescriptGraph".ToLowerInvariant()}
            };
            
            Dictionary<string, byte[]> bytes = new Dictionary<string, byte[]>();  // precalc for lil bit of speed
            foreach (KeyValuePair<uint,string> keyValuePair in knownValues) {
                bytes[keyValuePair.Value] = Encoding.ASCII.GetBytes(keyValuePair.Value);
            }

            const string outputFile = "crc.txt";
            const uint start = 0; // after running for so long, stop, and set this value to where you ended
            Dictionary<uint, int> results = new Dictionary<uint, int>();
            
            for (uint i = start; i < uint.MaxValue; i++) {  // not a joke, lets go
                int goodnessScale = 0;
                foreach (KeyValuePair<uint,string> knownValue in knownValues) {
                    uint trialHash = new Crc32(i).ComputeChecksum(bytes[knownValue.Value]);
                    if (trialHash == knownValue.Key) goodnessScale++;
                }
                if (goodnessScale > 0) results[i] = goodnessScale;
            }

            using (Stream stream = File.OpenWrite(outputFile)) {
                using (StreamWriter writer = new StreamWriter(stream)) {
                    foreach (KeyValuePair<uint,int> keyValuePair in results) {
                        writer.WriteLine($"{keyValuePair.Key:X}: {keyValuePair.Value}");
                    }
                }
            }
        }
    }
}