using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using STULib.Impl;

namespace STULib.Types.Map {
    public class Map : IDisposable {
        public List<ISTU> STUs { get; } = new List<ISTU>();
        public MapHeader Header { get; }
        public MapCommonHeader[] CommonHeaders { get; private set; }
        public IMapFormat[] Records { get; private set; }
        public MapManager Manager { get; } = MapManager.Instance;
        public HashSet<uint> STUInstances { get; } = new HashSet<uint>();

        private void AlignPosition(Stream input, long end) {
            input.Position = (long)(Math.Ceiling(end / 16.0f) * 16);
        }
        
        private void AlignPositionNew(BinaryReader reader, Version1 stu) {
            int maxOffset = stu.Records.Max(x => x.Offset);
            Generic.Version1.STUInstanceRecord record = stu.Records.FirstOrDefault(x => x.Offset == maxOffset);
            for (int i = record.Offset; i < reader.BaseStream.Length; i++) {
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length) {
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
                }
                uint magic = reader.ReadUInt32();
                if (magic == Version1.MAGIC) {
                    reader.BaseStream.Position -= 4;
                    break;
                }
                reader.BaseStream.Position -= 3;
            }
        }

        public Map(Stream input, uint owVersion, bool leaveOpen = false) {
            if (input == null) return;
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, leaveOpen)) {
                Header = reader.Read<MapHeader>();
                input.Position = Header.offset;
                Records = new IMapFormat[Header.recordCount];
                CommonHeaders = new MapCommonHeader[Header.recordCount];
                for (uint i = 0; i < Header.recordCount; ++i) {
                    try {
                        CommonHeaders[i] = reader.Read<MapCommonHeader>();
                        long nps = input.Position + CommonHeaders[i].size - 24;
                        if (Manager.InitializeInstance(CommonHeaders[i].type, input, out Records[i]) !=
                            MANAGER_ERROR.E_SUCCESS) {
                            if (System.Diagnostics.Debugger.IsAttached) {
                                System.Diagnostics.Debugger.Log(2, "MAP",
                                    $"Error reading Map type {CommonHeaders[i].type:X}\n");
                            }
                        }
                        input.Position = nps;
                    }
                    catch (OutOfMemoryException) {
                        CommonHeaders = null;
                        Records = null;
                        return;
                    }
                    catch (ArgumentOutOfRangeException) {
                        CommonHeaders = null;
                        Records = null;
                        return;
                    }
                }
                
                // todo: fix all of the existing classes
                // if (Records.Length > 0 && Records[0] != null && Records[0].HasSTUD) {
                //     AlignPosition(input, input.Position);
                //     while (true) {
                //         if (input.Position >= input.Length) {
                //             break;
                //         }
                //         ISTU tmp = ISTU.NewInstance(input, owVersion);
                //         AlignPositionNew(reader, tmp as Version1);
                //         STUInstances = new HashSet<uint>(STUInstances.Concat(tmp.TypeHashes).ToList());
                //         STUs.Add(tmp);
                //     }
                // }
            }
        }

        public void Dispose() {
            CommonHeaders = null;
            Records = null;
            GC.SuppressFinalize(this);
        }
    }
}