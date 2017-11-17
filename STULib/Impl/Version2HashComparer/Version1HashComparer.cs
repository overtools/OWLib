using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static OWLib.Extensions;

namespace STULib.Impl.Version2HashComparer {
    public class Version1Comparer : Version2Comparer {
        public Version1Comparer(Stream stuStream, uint owVersion) : base(stuStream, owVersion) { }

        protected override void ReadInstanceData(long offset) {
            Stream.Position = offset;
            InternalInstances = new Dictionary<uint, InstanceData>();
            
            using (BinaryReader reader = new BinaryReader(Stream, Encoding.UTF8, true)) {
                Types.Generic.Version1.STUHeader data = reader.Read<Types.Generic.Version1.STUHeader>();
                
                Types.Generic.Version1.STUInstanceRecord[] records = new Types.Generic.Version1.STUInstanceRecord[data.InstanceCount];
                InstanceData = new InstanceData[data.InstanceCount];

                for (int i = 0; i < data.InstanceCount; i++) {
                    try {
                        records[i] = reader.Read<Types.Generic.Version1.STUInstanceRecord>();
                    } catch (ArgumentOutOfRangeException) {
                        break;
                    }
                }

                int index = 0;
                foreach (Types.Generic.Version1.STUInstanceRecord record in records) {
                    if (record.Offset + 4 > reader.BaseStream.Length || record.Offset < 0) {
                        index++;
                        continue;
                    }
                    reader.BaseStream.Position = record.Offset;
                    uint checksum = reader.ReadUInt32();
                    InstanceData[index] = GetInstanceData(checksum);
                    index++;
                }
            }
        }

        public InstanceData GetInstanceData(uint instanceChecksum) {
            if (!InstanceJSON.ContainsKey(instanceChecksum)) {
                Debugger.Log(0, "STULib",
                    $"[Version1HashComparer]: Instance {instanceChecksum:X} does not exist in the dataset\n");
                return null;
            }
            STUInstanceJSON json = InstanceJSON[instanceChecksum];

            if (json.Parent != null && !InternalInstances.ContainsKey(json.ParentChecksum)) {
                InternalInstances[json.ParentChecksum] = GetInstanceData(json.ParentChecksum);
            }
            
            // get all children
            // WARNING: NOT THREAD SAFE
            if (GetAllChildren) {
                foreach (KeyValuePair<uint,STUInstanceJSON> instanceJSON in InstanceJSON) {
                    if (instanceJSON.Value.ParentChecksum != json.Hash ||
                        InternalInstances.ContainsKey(instanceJSON.Value.Hash)) continue;
                    InternalInstances[instanceJSON.Value.Hash] = null;
                    InternalInstances[instanceJSON.Value.Hash] = GetInstanceData(instanceJSON.Value.Hash);
                }
            }
            
            FieldData[] jsonFields = ProcessJSONFields(instanceChecksum, true);
            return new InstanceData {
                WrittenFields = null,
                Fields = jsonFields,
                Checksum = instanceChecksum,
                ParentType = json.Parent,
                ParentChecksum = json.ParentChecksum
            };
        }

        protected FieldData[] ProcessJSONFields(uint instanceChecksum, bool overrideThinger=false) {  // what no
            if (!InstanceJSON.ContainsKey(instanceChecksum)) return null;
            FieldData[] output = new FieldData[InstanceJSON[instanceChecksum].GetFields().Length];

            uint fieldIndex = 0;
            foreach (STUInstanceJSON.STUFieldJSON field in InstanceJSON[instanceChecksum].GetFields()) {
                output[fieldIndex] = new FieldData(field);
                if (output[fieldIndex].TypeInstanceChecksum != 0) {
                    if (!InternalInstances.ContainsKey(output[fieldIndex].TypeInstanceChecksum)) {
                        InternalInstances[output[fieldIndex].TypeInstanceChecksum] = null; // prevent stackoverflow
                        InternalInstances[output[fieldIndex].TypeInstanceChecksum] =
                            GetInstanceData(output[fieldIndex].TypeInstanceChecksum, null);
                    }
                }
                fieldIndex++;
            }
            return output;
        }
    }
}