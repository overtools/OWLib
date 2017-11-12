using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using OWLib;

namespace STULib.Types.Generic {
    public static class Common {
        public enum InstanceUsage : uint {
            None = 0,
            Root = 1,
            Embed = 2,
            EmbedArray = 3,
            Inline = 4,
            InlineArray = 5,
            HashmapElement = 6
        }

        public class STUInstance {
            // Version 1.0 prefix
            [STUField(STUVersionOnly = new uint[] { 1 })]
            public uint InstanceChecksum;

            [STUField(STUVersionOnly = new uint[] { 1 })]
            public uint NextInstanceOffset;

            [STUField(STUVersionOnly = new uint[] { 4 })]  // dont
            public InstanceUsage Usage = InstanceUsage.Root;

            // Version 2.0 prefix
            //[STUField(STUVersionOnly = new uint[] {2})]
            //public uint FieldListIndex;

            public override string ToString() {
                return ISTU.GetName(GetType());
            }
        }

        public interface ISTUHashToolPrintExtender {  // this needs to be worked into more things
            string Print(out Color? color);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUReferenceArray {
            public long EntryCount;
            public long SizeOffset;
            public long DataOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUString {
            public uint Size;
            public uint Checksum;
            public long Offset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        public class STUGUID : IDemangleable, IEquatable<STUGUID> {
            [STUField(0x1, DummySize = 8, OnlyBuffer = true, STUVersionOnly = new uint[] { 2 })] // DUMMY
            private ulong Padding = ulong.MaxValue;
            
            [STUField(0x2, DummySize = 8, STUVersionOnly = new uint[] { 1 })] // DUMMY
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // we need to read padding no matter what for v1
            private ulong V1Padding = ulong.MaxValue;

            [STUField(0x3, DummySize = 8)] // DUMMY
            private ulong Key;

            public STUGUID() {
            }

            public STUGUID(ulong key) {
                Key = key;
            }

            public STUGUID(ulong key, ulong padding) {
                Key = key;
                Padding = padding;
            }

            public static implicit operator long(STUGUID i) {
                return (long)(ulong)i;
            }

            public static implicit operator ulong(STUGUID i) {
                return i?.Key ?? 0;
            }

            public new string ToString() {
                return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}";
            }

            public ulong[] GetGUIDs() {
                return new[] {
                    Key
                };
            }

            public ulong[] GetGUIDXORs() {
                return new[] {
                    Padding
                };
            }

            // ReSharper disable once InconsistentNaming
            public void SetGUIDs(ulong[] GUIDs) {
                if (GUIDs?.Length > 0) {
                    Key = GUIDs[0];
                }
            }

            public bool Equals(STUGUID other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Padding == other.Padding && Key == other.Key;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((STUGUID) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (Padding.GetHashCode() * 397) ^ Key.GetHashCode();
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        // ReSharper disable once InconsistentNaming
        public class ulonglong {
            [STUField(0x1, DummySize = 8)] // DUMMY
            public ulong A;
            [STUField(0x2, DummySize = 8)] // DUMMY
            public ulong B;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        public class STUVec2 {
            [STUField(0x1, DummySize = 4)]
            public float X;

            [STUField(0x2, DummySize = 4)]
            public float Y;

            public static implicit operator Vector2(STUVec2 obj) {
                return new Vector2(obj.X, obj.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 12)] // DUMMY
        public class STUVec3 : STUVec2 {
            [STUField(0x3, DummySize = 4)]
            public float Z;

            public static implicit operator Vector3(STUVec3 obj) {
                return new Vector3(obj.X, obj.Y, obj.Z);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 16)] // DUMMY
        public class STUVec4 : STUVec3 {
            [STUField(0x4, DummySize = 4)]
            public float W;

            public static implicit operator Vector4(STUVec4 obj) {
                return new Vector4(obj.X, obj.Y, obj.Z, obj.W);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 16)] // DUMMY
        public class STUVec3A : STUVec4 {
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 4)] // DUMMY
        public class STUEntityID {
            [STUField(0x1, DummySize = 4)]
            public uint Value;

            public static implicit operator uint(STUEntityID obj) {
                return obj.Value;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 12)] // DUMMY
        public class STUColorRGB : ISTUHashToolPrintExtender {
            [STUField(0x1, DummySize = 4)]
            public float R;

            [STUField(0x2, DummySize = 4)]
            public float G;

            [STUField(0x3, DummySize = 4)]
            public float B;

            public static implicit operator Color(STUColorRGB obj) {
                return Color.FromArgb (
                    (int) (obj.R * 255f),
                    (int) (obj.G * 255f),
                    (int) (obj.B * 255f)
                );
            }

            public string Hex() {
                Color c = this;
                return $"#{c.Name}";
            }

            public string Print(out Color? color) {
                color = this;
                return Hex();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 16)] // DUMMY
        public class STUColorRGBA : STUColorRGB {
            [STUField(0x4, DummySize = 4)]
            public float A;

            public static implicit operator Color(STUColorRGBA obj) {
                return Color.FromArgb (
                    (int) (obj.A * 255f),
                    (int) (obj.R * 255f),
                    (int) (obj.G * 255f),
                    (int) (obj.B * 255f)
                );
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 16)] // DUMMY
        public class STUQuaternion {
            [STUField(0x1, DummySize = 4)]
            public float X;

            [STUField(0x2, DummySize = 4)]
            public float Y;

            [STUField(0x3, DummySize = 4)]
            public float Z;

            [STUField(0x4, DummySize = 4)]
            public float W;

            public static implicit operator Quaternion(STUQuaternion obj) {
                return new Quaternion(obj.X, obj.Y, obj.Z, obj.W);
            }
        }

        public interface ISTUCustomSerializable {
            object Deserialize(object instance, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader);
            object DeserializeArray(object instance, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUHashMapData {
            public int Unknown2Size;
            public uint Unknown1;
            public long Unknown2Offset;
            public long DataOffset;
        }

        public class STUHashMap<T> : Dictionary<ulong, T>, ISTUCustomSerializable  where T : STUInstance {
            public void Set(ulong index, T instance) {
                this[index] = instance;
            }

            public object Deserialize(object instace, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader) {
                int offset = reader.ReadInt32();
                if (offset == -1) return null;
                metadataReader.BaseStream.Position = offset;

                STUHashMapData data = metadataReader.Read<STUHashMapData>();
                if (data.Unknown2Size == 0) return null;

                metadataReader.BaseStream.Position = data.Unknown2Offset;
                List<uint> unknown2 = new List<uint>(data.Unknown2Size);
                for (int i = 0; i != data.Unknown2Size; ++i){
                    unknown2.Add(metadataReader.ReadUInt32());
                }

                metadataReader.BaseStream.Position = data.DataOffset;
                uint mapSize = unknown2.Last();
                for (int i = 0; i != mapSize; ++i) {
                    ulong key = metadataReader.ReadUInt64();
                    // Last 4 bytes are padding for in-place deserialization.
                    int value = (int) metadataReader.ReadUInt64();
                    if (value == -1) {
                        Add(key, null);
                    } else {
                        // get instance later
                        stu.HashmapRequests.Add(new KeyValuePair<KeyValuePair<Type, object>, KeyValuePair<uint, ulong>>(new KeyValuePair<Type, object>(typeof(T), this), new KeyValuePair<uint, ulong>((uint)value, key)));
                    }
                }
                return this;
            }

            public object DeserializeArray(object instace, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader) {
                throw new NotImplementedException();
            }
        }

        public class STUDateAndTime : ISTUHashToolPrintExtender {
            [STUField(0x1, DummySize = 8)]
            public ulong Timestamp;

            // todo: the timestamp doesn't work as seconds or milliseconds

            public DateTime ToDateTime() {
                return ToDateTimeUTC().ToLocalTime();
            }

            public DateTime ToDateTimeUTC() {
                DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return time.AddSeconds(Timestamp);
            }

            public override string ToString() {
                return ToDateTime().ToString(CultureInfo.InvariantCulture);
            }

            public string Print(out Color? color) {
                color = Color.Yellow;
                return "(STUDateAndTime doesn't work properly yet)";
            }
        }

        public class STUUUID : ISTUCustomSerializable {
            public Guid Value;

            public object Deserialize(object instance, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader) {
                Value = new Guid(reader.ReadBytes(16));
                return this;
            }

            public object DeserializeArray(object instance, ISTU stu, FieldInfo field, BinaryReader reader, BinaryReader metadataReader) {
                throw new NotImplementedException();
            }
        }
    }
}
