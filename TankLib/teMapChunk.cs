using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using TankLib.Math;
using TankLib.STU;
using TankLib.STU.Types;
using static TankLib.Util;
using static TankLib.Enums;

namespace TankLib {
    /// <summary>
    /// Tank MapChunk, file type 0BC
    /// </summary>
    public abstract class teMapChunk {
        protected abstract void Read(BinaryReader reader, teMAP_PLACEABLE_TYPE type);

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected teMapChunk() {

        }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected teMapChunk(Stream stream, teMAP_PLACEABLE_TYPE type) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader, type);
            }
        }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected teMapChunk(BinaryReader reader, teMAP_PLACEABLE_TYPE type) {
            Read(reader, type);
        }
    }

    public class teMapPlaceableData : teMapChunk {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct CommonStructure {
            /// <summary>
            /// Placeable UUID
            /// </summary>
            public teUUID UUID;

            public byte Unknown16; // 16
            public byte Unknown17; // 17
            public byte Unknown18;

            /// <summary>
            /// Size in bytes (including this structure)
            /// </summary>
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PlaceableDataHeader {
            /// <summary>
            /// Number of placeables
            /// </summary>
            public uint PlaceableCount;

            /// <summary>
            /// Offset to component instance data STUs
            /// </summary>
            /// <remarks>
            /// AFAIK you have to add 16 to get a useable value
            /// </remarks>
            public uint InstanceDataOffset;

            /// <summary>
            /// Offset to placeables
            /// </summary>
            public uint PlaceableOffset;

            public uint Unknown2;
        }

        public static teMapPlaceableManager Manager = new teMapPlaceableManager();

        public PlaceableDataHeader Header;
        public IMapPlaceable[] Placeables = {};
        public CommonStructure[] CommonStructures = {};

        public teMapPlaceableData() {
        }

        public teMapPlaceableData(Stream stream, teMAP_PLACEABLE_TYPE type) : base(stream, type) { }
        public teMapPlaceableData(BinaryReader reader, teMAP_PLACEABLE_TYPE type) : base(reader, type) { }

        protected override void Read(BinaryReader reader, teMAP_PLACEABLE_TYPE type) {
            Header = reader.Read<PlaceableDataHeader>();

            if (Header.PlaceableOffset > 0) {
                reader.BaseStream.Position = Header.PlaceableOffset;

                CommonStructures = new CommonStructure[Header.PlaceableCount];
                Placeables = new IMapPlaceable[Header.PlaceableCount];

                for (int i = 0; i < Header.PlaceableCount; i++) {
                    long beforePos = reader.BaseStream.Position;

                    CommonStructure commonStructure = reader.Read<CommonStructure>();
                    CommonStructures[i] = commonStructure;

                    if ((commonStructure.Unknown16 & 0x40) != 0) throw new Exception("placeable with variant bitmask. please tell zingy which map");

                    Placeables[i] = Manager.CreateType(commonStructure, type, reader);

                    reader.BaseStream.Position = beforePos + CommonStructures[i].Size;
                }

                if (CommonStructures.Length > 0 && type == teMAP_PLACEABLE_TYPE.ENTITY && Header.InstanceDataOffset > 0) {
                    int execCount = 0;
                    reader.BaseStream.Position = Header.InstanceDataOffset+16;
                    foreach (IMapPlaceable placeable in Placeables) {
                        if (!(placeable is teMapPlaceableEntity entity)) continue;

                        entity.InstanceData = new STUComponentInstanceData[entity.Header.InstanceDataCount];
                        for (int i = 0; i < entity.Header.InstanceDataCount; i++) {
                            reader.BaseStream.Position = entity.m_instanceDataOffsets[i];

                            try {
                                teStructuredData structuredData = new teStructuredData(reader);
                                entity.InstanceData[i] = structuredData.GetInstance<STUComponentInstanceData>();
                            } catch
                            {
                                execCount++;
                            }
                        }
                    }

                    if (execCount > 0) {
                        Debugger.Log(0, "teMapChunk", $"Threw {execCount} exceptions when trying to parse entity instance data\r\n");
                    }
                }
            }
        }

        /*private void AlignPosition(long start, BinaryReader reader, teStructuredData stu) {
            long maxOffset = stu.InstanceInfoV1.Max(x => x.Offset)+start;
            AlignPositionInternal(reader, maxOffset+8);
        }

        private void AlignPositionInternal(BinaryReader reader, long pos) {
            for (long i = pos; i < reader.BaseStream.Length; i++) {
                reader.BaseStream.Position = i;
                if (reader.BaseStream.Position + 4 > reader.BaseStream.Length) {
                    reader.BaseStream.Position = reader.BaseStream.Length;
                    break;
                }
                uint magic = reader.ReadUInt32();
                if (magic != teStructuredData.STRUCTURED_DATA_IMMUTABLE_MAGIC) continue;
                reader.BaseStream.Position -= 4;
                break;
            }
        }*/
    }

    public interface IMapPlaceable {
        teMAP_PLACEABLE_TYPE Type { get; }

        void Read(BinaryReader reader);
    }

    public class teMapPlaceableModelGroup : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.MODEL_GROUP;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Model;
            public uint GroupCount;
            public uint TotalModelCount;
            public uint Mask;
            public uint Unk1;
            public uint Unk2;
            public uint Unk3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Group {
            public teResourceGUID ModelLook;
            public uint UnkSize;
            public int EntryCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Entry {
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public uint Unk1;
            public uint Unk2;
            public uint Unk3;
            public uint Unk4;
            public uint Unk5;
            public uint Unk6;
            public uint Unk7;
            public int Unk8;
            public int Unk9;
            public int UnkA;
            public int UnkB;
            public int UnkC;
        }

        public Structure Header;
        public Group[] Groups;
        public Entry[][] Entries;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();

            Groups = new Group[Header.GroupCount];
            Entries = new Entry[Header.GroupCount][];

            for (int i = 0; i < Header.GroupCount; i++) {
                Group group = reader.Read<Group>();
                Groups[i] = group;

                Entries[i] = reader.ReadArray<Entry>(group.EntryCount);
            }
        }
    }

    public class teMapPlaceableEntity : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.ENTITY;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID EntityDefinition;  // 003
            public teResourceGUID Identifier1;  // 01C
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public uint InstanceDataCount;

            // todo: more data here
        }

        public Structure Header;
        public STUComponentInstanceData[] InstanceData;
        public uint[] m_instanceDataOffsets;

        public void Read(BinaryReader reader) {
            long start = reader.BaseStream.Position;

            Header = reader.Read<Structure>();

            m_instanceDataOffsets = new uint[Header.InstanceDataCount];

            const int instArrayOffset = 104;
            reader.BaseStream.Position = start + instArrayOffset;
            for (int i = 0; i < Header.InstanceDataCount; i++) {
                long disStart = reader.BaseStream.Position;

                var _ = reader.ReadUInt32(); // type
                var relOffset = reader.ReadUInt32();

                var offset = disStart + relOffset;
                m_instanceDataOffsets[i] = (uint)offset;
            }
        }
    }

    public class teMapPlaceableLight : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.LIGHT;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teQuat Rotation;
            public teVec3 Translation;
            public uint Unknown1;
            public uint Unknown2;
            public teLIGHTTYPE Type;
            public uint Unknown3;
            public uint Unknown4;
            public teColorRGB Color;
            public teVec3 UnknownPos1;
            public teQuat UnknownQuat1;
            public float LightFOV;      // Cone angle, in degrees. Set to -1.0 for Point Lights
            public teVec3 UnknownPos2;
            public teQuat UnknownQuat2;
            public teVec3 UnknownPos3;
            public teVec3 UnknownPos4;
            public float IntensityGUESS;
            public float Unknown6;
            public float Unknown7;
            public float Unknown8;
            public float Unknown9;
            public float Unknown10;
            public teResourceGUID ProjectionTexture1;
            public teResourceGUID ProjectionTexture2;
        }

        // [StructLayout(LayoutKind.Sequential, Pack = 4)]
        // public struct Structure {
        //     public teQuat Rotation;
        //     public teVec3 Translation;
        //     public uint Unknown1A;
        //     public uint Unknown1B;
        //     public byte Unknown2A;
        //     public byte Unknown2B;
        //     public byte Unknown2C;
        //     public byte Unknown2D;
        //     public uint Unknown3A;
        //     public uint Unknown3B;
        //     public teLIGHTTYPE Type;
        //     public teColorRGB Color;
        //     public teVec3 UnknownPos1;
        //     public teQuat UnknownQuat1;
        //     public float LightFOV;      // Cone angle, in degrees. Set to -1.0 for Point Lights
        //     public teVec3 UnknownPos2;
        //     public teQuat UnknownQuat2;
        //     public teVec3 UnknownPos3;
        //     public teQuat UnknownQuat3;
        //     public float Unknown4A;
        //     public float Unknown4B;
        //     public teResourceGUID ProjectionTexture1;
        //     public teResourceGUID ProjectionTexture2;
        // }

        public Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();
        }
    }

    public class teMapPlaceableSingleModel : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.SINGLE_MODEL;

        public teMapPlaceableModel.Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<teMapPlaceableModel.Structure>();
        }
    }

    public class teMapPlaceableModel : IMapPlaceable
    {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.MODEL;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure
        {
            public teResourceGUID Model;
            public teResourceGUID ModelLook;
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public teQuat Unknown;
        }

        public Structure Header;

        public void Read(BinaryReader reader)
        {
            Header = reader.Read<Structure>();
        }
    }

    public class teMapPlaceableReflectionPoint : IMapPlaceable
    {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.REFLECTIONPOINT;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure
        {
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public teVec3 IdentityTranslation;
            public teVec3 IdentityScale;
            public teQuat IdentityRotation;
            public teVec3 Corner;
            public uint Unknown1;
            public teResourceGUID Texture1;
            public teResourceGUID Texture2;
            public uint Unknown2;
            public uint Unknown3;
            public float UnknownFloat1;
            public float UnknownFloat2;
            public float UnknownFloat3;
            public float UnknownFloat4;
            public float UnknownFloat5;
            public float UnknownFloat6;
            public float UnknownFloat7;
            public int UnknownInt1;
            public int UnknownInt2;
            public int UnknownInt3;
            public int UnknownInt4;
            public int UnknownInt5;
        }

        public Structure Header;

        public void Read(BinaryReader reader)
        {
            Header = reader.Read<Structure>();
        }
    }

    public class teMapPlaceableText : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.TEXT;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public float FloatA;
            public float FloatB;
            public float FloatC;
            public float FloatD;

            public float FloatE;
            public float FloatF;
            public float FloatG;
            public float FloatH;

            public teResourceGUID String;
            public teResourceGUID MapFont;
            public teResourceGUID Material;
        }

        public Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();
        }
    }

    public class teMapPlaceableSound : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.SOUND;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Sound;
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
        }

        public Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();
        }
    }

    public class teMapPlaceableEffect : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.EFFECT;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Effect;
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
        }

        public Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();
        }
    }
    
    public class teMapPlaceableSequence : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.SEQUENCE;

        public teMapPlaceableEffect.Structure Header;

        public void Read(BinaryReader reader) {
            Header = reader.Read<teMapPlaceableEffect.Structure>();
        }
    }
    
    public class teMapPlaceableArea : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.AREA;

        [StructLayout(LayoutKind.Explicit)]
        public struct Structure {

            [FieldOffset(12)] public ushort BoxCount;
            [FieldOffset(14)] public ushort SphereCount;
            [FieldOffset(16)] public ushort CapsuleCount;
            [FieldOffset(18)] public ushort Unknown1Count;
            [FieldOffset(20)] public ushort Unknown2Count;

            [FieldOffset(24)] public uint BoxOffset;
            [FieldOffset(28)] public uint SphereOffset;
            [FieldOffset(32)] public uint CapsuleOffset;
            [FieldOffset(36)] public uint Unknown1Offset;
            [FieldOffset(40)] public uint Unknown2Offset;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Box {
            [FieldOffset(0)] public Quaternion Orientation;
            [FieldOffset(16)] public Vector3 Translation;
            [FieldOffset(28)] public Vector3 Extents;
            [FieldOffset(40)] public Vector4 NoIdea;
        }

        public Structure Header;
        public Box[] Boxes;

        public void Read(BinaryReader reader) {
            var basePos = reader.BaseStream.Position;
            var header = reader.Read<Structure>();

            reader.BaseStream.Position = basePos + header.BoxOffset;
            Boxes = reader.ReadArray<Box>(header.BoxCount);
        }
    }

    public class teMapPlaceableDummy : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.UNKNOWN;

        public byte[] Data;
        public int Size;

        public teMapPlaceableDummy() { }

        public unsafe teMapPlaceableDummy(int size)
        {
            Size = size-sizeof(teMapPlaceableData.CommonStructure);
        }

        public void Read(BinaryReader reader)
        {
            Data = reader.ReadBytes(Size);
        }
    }

    public class teMapPlaceableManager {
        public Dictionary<teMAP_PLACEABLE_TYPE, Type> Types;
        private readonly HashSet<teMAP_PLACEABLE_TYPE> _misingTypes;

        public teMapPlaceableManager() {
            _misingTypes = new HashSet<teMAP_PLACEABLE_TYPE>();
            Types = new Dictionary<teMAP_PLACEABLE_TYPE, Type>();
            AddAssemblyTypes(typeof(teMapPlaceableData).Assembly);
        }

        public void AddAssemblyTypes(Assembly assembly) {
            foreach (Type type in GetAssemblyTypes<IMapPlaceable>(assembly)) {
                if (type.IsInterface) continue;
                AddType(type);
            }
        }

        private void AddType(Type type) {
            IMapPlaceable instance = (IMapPlaceable)Activator.CreateInstance(type);

            Types[instance.Type] = type;
        }

        public IMapPlaceable CreateType(teMapPlaceableData.CommonStructure commonStructure, teMAP_PLACEABLE_TYPE type, BinaryReader reader) {
            IMapPlaceable value = new teMapPlaceableDummy((int)commonStructure.Size);
            if (Types.TryGetValue(type, out Type placeableType)) {
                value = (IMapPlaceable)Activator.CreateInstance(placeableType);
            } else if (_misingTypes.Add(type)) {
                Debugger.Log(0, "teMapPlaceableManager", $"Unhandled placeable type: {type}\r\n");
            }
            value.Read(reader);
            return value;
        }
    }
}