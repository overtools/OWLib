using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CRCReverse {
    public class Crc32 {
        readonly uint[] table;

        public uint ComputeChecksum(byte[] bytes) {
            uint crc = 0xffffffff;
            for(int i = 0; i < bytes.Length; ++i) {
                byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
                crc = (uint)((crc >> 8) ^ table[index]);
            }
            return ~crc;
        }

        public byte[] ComputeChecksumBytes(byte[] bytes) {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }

        public Crc32(uint poly=0xedb88320) {
            table = new uint[256];
            for(uint i = 0; i < table.Length; ++i) {
                uint temp = i;
                for(int j = 8; j > 0; --j) {
                    if((temp & 1) == 1) {
                        temp = (temp >> 1) ^ poly;
                    }else {
                        temp >>= 1;
                    }
                }
                table[i] = temp;
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
                {0x8F736177, "m_rank"}
            };
            List<uint> trialPolys = new List<uint> {0x814141AB, 0x32583499, 0x741B8CD7, 0x1EDC6F41, 0x04C11DB7, // normal?
                0xEDB88320, 0x82F63B78, 0xEB31D82E, 0x992C1A4C, 0xD5828281,// reversed?
                0xC0A0A0D5, 0x992C1A4C, 0xBA0DC66B, 0x8F6E37A0 // reverced reciprocal
            };  // none of the standard thingers work

            KeyValuePair<uint, string> chosenPair = knownValues.SingleOrDefault(p => p.Value == "stulootbox");  // 99.9% this one good

            // const uint start = uint.MaxValue; // after running for so long, stop, and set this value to where you ended
            const uint start = 3947316757; // after running for so long, stop, and set this value to where you ended
            
            for (uint i = start; i < uint.MaxValue; i++) {  // not a joke, lets go
            // for (uint i = start; i > 0 ; i--) {
                int goodnessScale = 0;
                foreach (KeyValuePair<uint,string> knownValue in knownValues) {
                    uint trialHash = new Crc32(i).ComputeChecksum(Encoding.ASCII.GetBytes(knownValue.Value));
                    if (trialHash == knownValue.Key) goodnessScale++;
                }
                if ((double) goodnessScale / knownValues.Count*100 > 50.0) {  // ok so I'm not acutally sure how many are correct
                    Debugger.Break();  // good boi
                }
            }
        }
    }
}