using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LZ4;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;

namespace TankLib {
    public static class Diff {
        public static void WriteBinaryGUIDs(string file, IReadOnlyCollection<ulong> guids) {
            using (Stream stream = File.OpenWrite(file)) 
            using (var lz4Stream = new LZ4Stream(stream, LZ4StreamMode.Compress))
            using (var writer = new BinaryWriter(lz4Stream)) {
                stream.Write(new byte[4], 0, 4); // non-compressed
                writer.Write((int)guids.Count);
                writer.WriteStructArray(guids.ToArray());
            }
        }

        public static void WriteTextGUIDs(ProductHandler_Tank tankHandler, string file, IEnumerable<ulong> guids) {
            using (StreamWriter writer = new StreamWriter(file)) {
                foreach (ulong asset in guids) {
                    writer.WriteLine(asset.ToString("X"));
                }
            }
        }

        public static ulong[] ReadBinaryGUIDs(BinaryReader reader) {
            var count = reader.ReadInt32();
            return reader.ReadArray<ulong>(count);
        }
        
        public static ulong[] ReadTextGUIDs(TextReader reader) {
            return reader.ReadToEnd().Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();
        }

        public static HashSet<ulong> ReadGUIDs(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                var zero = reader.ReadInt32();

                IEnumerable<ulong> guids;
                if (zero == 0) {
                    using (var lz4Stream = new LZ4Stream(stream, LZ4StreamMode.Decompress))
                    using (var lz4Reader = new BinaryReader(lz4Stream))
                        guids = ReadBinaryGUIDs(lz4Reader);
                } else {
                    stream.Position = 0;
                    using (var streamReader = new StreamReader(stream))
                        guids = ReadTextGUIDs(streamReader);
                }
                return new HashSet<ulong>(guids);
            }
        }
        
        public static void WriteBinaryCKeys( string file, IReadOnlyCollection<CKey> cKeys) {
            using (Stream stream = File.OpenWrite(file)) 
            using (var lz4Stream = new LZ4Stream(stream, LZ4StreamMode.Compress))
            using (var writer = new BinaryWriter(lz4Stream)) {
                stream.Write(new byte[4], 0, 4); // non-compressed
                writer.Write((int)cKeys.Count);
                writer.WriteStructArray(cKeys.ToArray());
            }
        }

        public static void WriteTextCKeys(ProductHandler_Tank tankHandler, string file, IEnumerable<CKey> cKeys) {
            using (StreamWriter writer = new StreamWriter(file)) {
                foreach (CKey asset in cKeys) {
                    writer.WriteLine(asset.ToHexString());
                }
            }
        }
        
        public static IEnumerable<CKey> ReadBinaryCKeys(BinaryReader reader) {
            var count = reader.ReadInt32();
            return reader.ReadArray<CKey>(count);
        }

        public static IEnumerable<CKey> ReadTextCKeys(StreamReader streamReader) {
            return streamReader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).Select(CKey.FromString);
        }
        
        public static HashSet<CKey> ReadCKeys(Stream stream) {
            using (var reader = new BinaryReader(stream)) {
                var zero = reader.ReadInt32();

                IEnumerable<CKey> ckeys;
                if (zero == 0) {
                    using (var lz4Stream = new LZ4Stream(stream, LZ4StreamMode.Decompress))
                    using (var lz4Reader = new BinaryReader(lz4Stream))
                        ckeys = ReadBinaryCKeys(lz4Reader);
                } else {
                    stream.Position = 0;
                    using (var streamReader = new StreamReader(stream))
                        ckeys = ReadTextCKeys(streamReader);
                }
                return new HashSet<CKey>(ckeys, CASCKeyComparer.Instance);
            }
        }
    }
}
