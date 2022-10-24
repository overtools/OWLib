using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TankLib;

namespace DataTool.ConvertLogic.WEM {
    public class WwiseBank {
        public WwiseBankWemDef[] WemDefs { get; }
        public byte[][] WemData { get; }

        public Dictionary<uint, IBankObject> Objects { get; }
        public List<WwiseBankChunkHeader> Chunks { get; }
        public Dictionary<WwiseBankChunkHeader, long> ChunkPositions { get; }

        public static bool Ready { get; private set; }
        public static Dictionary<byte, Type> Types { get; private set; }

        public static void GetReady() {
            Types = new Dictionary<byte, Type>();

            Assembly assembly = typeof(WwiseBank).Assembly;
            Type baseType = typeof(IBankObject);
            List<Type> types = assembly.GetTypes().Where(type => type != baseType && baseType.IsAssignableFrom(type)).ToList();

            foreach (Type type in types) {
                BankObjectAttribute bankObjectAttribute = type.GetCustomAttribute<BankObjectAttribute>();
                if (bankObjectAttribute == null) continue;
                Types[bankObjectAttribute.Type] = type;
            }

            Ready = true;
        }

        public WwiseBank(Stream stream) {
            if (!Ready) throw new BankNotReadyException();
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                // reference: http://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)

                ChunkPositions = new Dictionary<WwiseBankChunkHeader, long>();
                Chunks = new List<WwiseBankChunkHeader>();

                while (reader.BaseStream.Position < reader.BaseStream.Length) {
                    WwiseBankChunkHeader chunk = reader.Read<WwiseBankChunkHeader>();
                    Chunks.Add(chunk);
                    ChunkPositions[chunk] = reader.BaseStream.Position;
                    reader.BaseStream.Position += chunk.ChunkLength;
                }

                WwiseBankChunkHeader dataHeader = Chunks.FirstOrDefault(x => x.Name == "DATA");
                WwiseBankChunkHeader didxHeader = Chunks.FirstOrDefault(x => x.Name == "DIDX");

                if (dataHeader.MagicNumber != 0 && dataHeader.MagicNumber != 0) {
                    reader.BaseStream.Position = ChunkPositions[didxHeader];
                    if (didxHeader.ChunkLength <= 0) return;

                    WemDefs = new WwiseBankWemDef[didxHeader.ChunkLength / 12];
                    WemData = new byte[didxHeader.ChunkLength / 12][];
                    for (int i = 0; i < didxHeader.ChunkLength / 12; i++) {
                        WemDefs[i] = reader.Read<WwiseBankWemDef>();
                        long temp = reader.BaseStream.Position;

                        reader.BaseStream.Position = ChunkPositions[dataHeader];
                        WemData[i] = reader.ReadBytes(WemDefs[i].FileLength);

                        reader.BaseStream.Position = temp;
                    }
                }

                WwiseBankChunkHeader hircHeader = Chunks.FirstOrDefault(x => x.Name == "HIRC");

                if (hircHeader.MagicNumber != 0) {
                    reader.BaseStream.Position = ChunkPositions[hircHeader];
                    uint objectCount = reader.ReadUInt32();
                    Objects = new Dictionary<uint, IBankObject>((int) objectCount);
                    for (int o = 0; o < objectCount; o++) {
                        byte objectType = reader.ReadByte();
                        uint objectLength = reader.ReadUInt32();

                        long beforeObject = reader.BaseStream.Position;

                        uint objectID = reader.ReadUInt32();

                        if (Types.ContainsKey(objectType)) {
                            if (!(Activator.CreateInstance(Types[objectType]) is IBankObject bankObject)) continue;
                            bankObject.Read(reader);
                            Objects[objectID] = bankObject;
                        } else {
                            Debugger.Log(0, "[DataTool.Convertlogic.Sound]", $"Unhandled Bank object type: {objectType}\r\n");
                            Objects[objectID] = null;
                        }

                        long newPos = beforeObject + objectLength;
                        if (newPos < reader.BaseStream.Position) throw new BankObjectTooMuchReadException($"Bank object of type {objectType} read too much data");
                        reader.BaseStream.Position = newPos;
                    }
                }
            }
        }

        public void WriteWems(string output) {
            if (WemDefs == null) return;
            for (int i = 0; i < WemDefs.Length; i++) {
                WwiseBankWemDef wemDef = WemDefs[i];
                byte[] data = WemData[i];

                using (Stream outputs = File.Open($"{output}{Path.DirectorySeparatorChar}{wemDef.FileID:X8}.wem", FileMode.OpenOrCreate, FileAccess.Write)) {
                    outputs.SetLength(0);
                    outputs.Write(data, 0, data.Length);
                }
            }
        }

        public IEnumerable<T> ObjectsOfType<T>() {
            foreach (KeyValuePair<uint, IBankObject> bankObject in Objects) {
                if (bankObject.Value != null && bankObject.Value.GetType() == typeof(T)) {
                    yield return (T) bankObject.Value;
                }
            }
        }
    }
}