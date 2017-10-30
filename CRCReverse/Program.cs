using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRCReverse {
    public class Crc32 {
        public readonly uint[] Table;

        public uint ComputeChecksum(IEnumerable<byte> bytes, uint xorStart = 0xffffffff, uint xorEnd = 0xffffffff) {
            uint crc = xorStart;
            foreach (byte t in bytes) {
                byte index = (byte)((crc & 0xff) ^ t);
                crc = (crc >> 8) ^ Table[index];
            }
            return crc ^ xorEnd;
        }
        
        public Crc32(uint poly = 0xedb88320) {
            Table = new uint[256];
            for (uint i = 0; i < Table.Length; ++i) {
                uint temp = i;
                for (int j = 8; j > 0; --j) {
                    if ((temp & 1) == 1) {
                        temp = (temp >> 1) ^ poly;
                    } else {
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
                // {0x7236F6E3, "STUStatescriptGraph".ToLowerInvariant()}  // idk about this one so I'll disable it for speed
            };
            // knownValues = new Dictionary<uint, string> {  // old vals
            //     {0xa6886a1, "STULootbox".ToLowerInvariant()},
            //     {0x7ce5c1b2, "stuachievement"}
            // };

            Dictionary<string, byte[]> bytes = new Dictionary<string, byte[]>();  // precalc for lil bit of speed
            foreach (KeyValuePair<uint, string> keyValuePair in knownValues) {
                bytes[keyValuePair.Value] = Encoding.ASCII.GetBytes(keyValuePair.Value);
            }

            int goodCount = knownValues.Count/2;
            long counter = 0;
            Crc32 crc32 = new Crc32(); // for this we only need to make the table once

            const long start = 0;
            const long end = (long) uint.MaxValue + 1;

            Parallel.For(start, end, i => {
                // i is start xor
                // if (i != 0xffffffff) return;  // old hashes test
                counter++;
                
                if (counter % 100000000 == 0) Console.Error.WriteLine($"(I'm at {counter}/{end})");
                
                Dictionary<uint, int> goodness = new Dictionary<uint, int>();  // end_xor: count
                
                foreach (KeyValuePair<uint, string> knownValue in knownValues) {
                    uint trialHash = crc32.ComputeChecksum(bytes[knownValue.Value], (uint)i, 0);  // don't xor at the end, and i is start
                    uint testEndXor = trialHash | knownValue.Key;  // get end xor
                    if (!goodness.ContainsKey(testEndXor)) goodness[testEndXor] = 0;
                    goodness[testEndXor]++;
                }
                foreach (KeyValuePair<uint,int> goodPair in goodness.OrderByDescending(x => x.Value).Where(x => x.Value >= goodCount)) {
                    Console.Out.WriteLine($"Result: start_xor={i:X}, end_xor={goodPair.Key:X} (count: {goodPair.Value})");
                    Console.Out.WriteLine("Trials:");
                    foreach (KeyValuePair<uint, string> knownValue in knownValues) {
                        uint trialHash = crc32.ComputeChecksum(bytes[knownValue.Value], (uint)i, goodPair.Key);
                        Console.Out.WriteLine($"\t{knownValue.Value}: {trialHash:X}");
                    }
                }
            });
        }
    }
}