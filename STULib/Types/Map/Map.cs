using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            long maxOffset = stu.Records.Max(x => x.Offset)+stu.Start;
            for (long i = maxOffset+4; i < reader.BaseStream.Length; i++) {
                reader.BaseStream.Position = i;
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length) {
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
                }
                uint magic = reader.ReadUInt32();
                if (magic == Version1.Magic) {
                    reader.BaseStream.Position -= 4;
                    break;
                }
            }
        }
        
        private void AlignPositionNew(BinaryReader reader) {
            int maxOffset = (int)reader.BaseStream.Position + 4;  // after the last magic
            for (int i = maxOffset; i < reader.BaseStream.Length; i++) {
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length) {
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
                }
                uint magic = reader.ReadUInt32();
                if (magic == Version1.Magic) {
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
                        long before = reader.BaseStream.Position;
                        long nps = input.Position + CommonHeaders[i].size - 24;
                        if (Manager.InitializeInstance(CommonHeaders[i].type, input, out Records[i]) !=
                            MANAGER_ERROR.E_SUCCESS) {
                            if (Debugger.IsAttached) {
                                Debugger.Log(0, "STULib.Types.Map", $"[STULib.Types.Map.Map]: Error reading Map type {CommonHeaders[i].type:X} (offset: {before})\n");
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
                
                if (Records.Length > 0 && Records[0] != null && Records[0].HasSTUD) {
                    AlignPosition(input, input.Position);
                    while (true) {
                        if (input.Position >= input.Length) {
                            break;
                        }
                        ISTU tmp;
                        try {
                            tmp = ISTU.NewInstance(input, owVersion);
                        }
                        catch (ArgumentOutOfRangeException) {
                            Debugger.Log(0, "STULib.Types.Map", "[STULib.Types.Map.Map]: Error while reading STU (fix the damn parser)\r\n");
                            AlignPositionNew(reader);
                            continue;
                        }
                        
                        AlignPositionNew(reader, tmp as Version1);
                        STUInstances = new HashSet<uint>(STUInstances.Concat(tmp.TypeHashes).ToList());
                        STUs.Add(tmp);
                    }
                }
            }
        }

        public void Dispose() {
            CommonHeaders = null;
            Records = null;
            GC.SuppressFinalize(this);
        }
    }
}
