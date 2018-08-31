using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TankLib.Helpers;
using TankLib.Helpers.Hash;

namespace TankLib.STU {
    /// <summary>
    /// Structured Data parser
    /// </summary>
    public class teStructuredData {
        // ReSharper disable once InconsistentNaming
        /// <summary>"v1" STU magic number</summary>
        public static readonly int STRUCTURED_DATA_IMMUTABLE_MAGIC = Util.GetMagicBytes('S', 'T', 'U', 'D');
        public static readonly teStructuredDataMgr Manager = new teStructuredDataMgr();
        
        /// <summary>CRC64 of the header</summary>
        public ulong HeaderChecksum;
        
        /// <summary>Loaded instances</summary>
        public STUInstance[] Instances;
        
        /// <summary>Type of STU structure this asset uses</summary>
        public teStructuredDataFormat Format;
        
        #region V2
        public STUBag<STUInstance_Info> InstanceInfo;
        public STUBag<STUInlineArray_Info> InlinedTypesInfo;
        public STUBag<STUBag<STUField_Info>> FieldInfoBags;
        #endregion

        #region V1
        private Dictionary<long, STUInstance> _instanceOffsets;
        public STUInstanceRecordV1[] InstanceInfoV1;
        #endregion
        
        #region Streams
        public BinaryReader DynData;
        public BinaryReader Data;
        #endregion

        /// <summary>Data start position</summary>
        internal readonly long StartPos;

        private bool _preserveStream;
   
        /// <summary>Load STU asset from a stream</summary>
        /// <param name="stream">The stream to load from</param>
        /// <param name="keepOpen">Leave the stream open after reading</param>
        public teStructuredData(Stream stream, bool keepOpen=false) {
            StartPos = stream.Position;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, keepOpen)) {
                Deserialize(reader);
            }
            FinishDeserialize();
        }
        
        /// <summary>Load STU asset from a BinaryReader</summary>
        /// <param name="reader">The BinaryReader to load from</param>
        public teStructuredData(BinaryReader reader) {
            _preserveStream = true;
            StartPos = reader.BaseStream.Position;
            Deserialize(reader);
            FinishDeserialize();
        }

        private void Deserialize(BinaryReader reader) {
            uint magic = reader.ReadUInt32();

            if (magic == STRUCTURED_DATA_IMMUTABLE_MAGIC) {
                Format = teStructuredDataFormat.V1;
                DeserializeV1(reader);
            } else {
                // "v2" stu with no magic :(
                Format = teStructuredDataFormat.V2;
                if (reader.BaseStream.Length < 36) throw new EndOfStreamException("Invalid STU asset. Truncated header.");
                DeserializeV2(reader);
            }
        }

        /// <summary>Read a "Version1" STU asset</summary>
        private void DeserializeV1(BinaryReader reader) {
            _instanceOffsets = new Dictionary<long, STUInstance>();

            reader.BaseStream.Position = StartPos;

            STUHeaderV1 header = reader.Read<STUHeaderV1>();

            InstanceInfoV1 = new STUInstanceRecordV1[header.InstanceCount];
            Instances = new STUInstance[header.InstanceCount];
            for (int i = 0; i < header.InstanceCount; i++) {
                STUInstanceRecordV1 record = reader.Read<STUInstanceRecordV1>();
                InstanceInfoV1[i] = record;

                long position = reader.BaseStream.Position;
                reader.BaseStream.Position = record.Offset + StartPos;
                uint instanceHash = reader.ReadUInt32();
                uint nextOffset = reader.ReadUInt32();

                STUInstance instance = Manager.CreateInstance(instanceHash);
                _instanceOffsets[record.Offset] = instance;
                Instances[i] = instance;

                reader.BaseStream.Position = position;
            }

            Data = reader; // hmm

            for (int i = 0; i < header.InstanceCount; i++) {
                STUInstanceRecordV1 record = InstanceInfoV1[i];

                if (Instances[i] == null) continue;

                // dump instance
                /*reader.BaseStream.Position = record.Offset + StartPos;
                Directory.CreateDirectory("STUV1Dump");
                using (Stream dumpFile = File.OpenWrite($"STUV1Dump\\{Instances[i].GetType().Name}_{i}")) {
                    dumpFile.SetLength(0);
                    int next;
                    {
                        IEnumerable<STUInstanceRecordV1> more = InstanceInfoV1.Where(x => x.Offset > record.Offset);
                        if (more.Any()) {
                            next = InstanceInfoV1.Where(x => x.Offset > record.Offset).Min(x => x.Offset);
                        } else {
                            next = (int)Data.BaseStream.Length; // err, not great
                        }
                    }
                    reader.BaseStream.Position = record.Offset + StartPos;
                    int size = next - (int) reader.BaseStream.Position;
                    byte[] buf = reader.ReadBytes(size);
                    dumpFile.Write(buf, 0, size);
                }*/

                reader.BaseStream.Position = record.Offset + StartPos;

                uint instanceHash = reader.ReadUInt32();
                uint nextOffset = reader.ReadUInt32();

                //if (instanceHash == 0xEA30C5E9) continue;
                //if (instanceHash == 0x05C7059E) {
                //    Debugger.Break();
                //}

                try {
                    Instances[i].Deserialize(this);
                } catch { }

                //if (instanceHash == 0x05C7059E) {
                //    Debugger.Break();
                //}
            }
        }

        /// <summary>Read a "Version2" STU asset</summary>
        private void DeserializeV2(BinaryReader reader) {
            reader.BaseStream.Position = StartPos;
            HeaderChecksum = CRC.CRC64(reader.ReadBytes(36));
            reader.BaseStream.Position = StartPos;
            SerializableHelper.Deserialize(reader, out InstanceInfo);
            SerializableHelper.Deserialize(reader, out InlinedTypesInfo);
            SerializableHelper.Deserialize(reader, out FieldInfoBags);
            
            int dynDataSize = reader.ReadInt32();
            int dynDataOff = reader.ReadInt32();
            int dataBufferOffset = reader.ReadInt32();
            if (dynDataSize > 0) {
                reader.BaseStream.Position = dynDataOff + StartPos;
                DynData = new BinaryReader(new MemoryStream(reader.ReadBytes(dynDataSize)));
            }
            if (dataBufferOffset < reader.BaseStream.Length) {
                long dataSize = reader.BaseStream.Length - dataBufferOffset;
                if (dataSize > int.MaxValue) throw new Exception("oops");

                reader.BaseStream.Position = dataBufferOffset + StartPos;
                Data = new BinaryReader(new MemoryStream(reader.ReadBytes((int)dataSize)));
            }

            if (InstanceInfo.Count > 0) {
                Instances = new STUInstance[InstanceInfo.Count];
                for (int i = 0; i < InstanceInfo.Count; i++) {
                    Instances[i] = Manager.CreateInstance(InstanceInfo[i].Hash);
                }
            }
            
            for (int i = 0; i != InstanceInfo.Count; ++i) {
                STUInstance_Info info = InstanceInfo[i];
                STUInstance instance = Instances[i];

                long startPosition = Data.Position();

                if (instance != null) {
                    instance.Deserialize(this);
                    long endPosition = Data.Position();
                }

                Data.BaseStream.Position = startPosition + info.Size;
                //if (endPosition - startPosition != info.Size)
                //    throw new Exceptions.InvalidTypeSize($"read len != type size. Type: '{instance.GetName()}', Data offset: {startPosition}");
            }
        }

        /// <summary>Gets the STUInstance at an offset</summary>
        public STUInstance GetInstanceAtOffset(long offset) {
            if (Format != teStructuredDataFormat.V1) throw new InvalidOperationException();
            return !_instanceOffsets.ContainsKey(offset) ? null : _instanceOffsets[offset];
        }

        /// <summary>Cleanup after deserializing</summary>
        private void FinishDeserialize() {
            if (!_preserveStream) {
                Data?.Dispose();
                DynData?.Dispose();
            }
            Data = null;
            DynData = null;
            
        }

        /// <summary>Get the primary instance of this asset</summary>
        public T GetMainInstance<T>() where T : STUInstance {
            if (Instances.Length == 0) return null;
            return Instances[0] as T;
        }
        
        public T GetInstance<T>() where T : STUInstance {
            return Instances.OfType<T>().FirstOrDefault();
        }
        
        public IEnumerable<T> GetInstances<T>() where T : STUInstance {
            return Instances.OfType<T>();
        }
    }

    public enum teStructuredDataFormat {
        V1,
        V2
    }
}