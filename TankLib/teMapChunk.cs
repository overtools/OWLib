using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        protected abstract void Read(BinaryReader reader);
        
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public teMapChunk(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        public teMapChunk(BinaryReader reader) {
            Read(reader);
        }
    }
    
    public class teMapPlaceableData : teMapChunk {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct CommonStructure {
            /// <summary>
            /// Placeable UUID
            /// </summary>
            public teUUID UUID;
            
            public ushort Unknown1;
            public byte Unknown2;
            
            /// <summary>
            /// Placeable type
            /// </summary>
            public teMAP_PLACEABLE_TYPE Type;
            
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
        public IMapPlaceable[] Placeables;
        public CommonStructure[] CommonStructures;

        public teMapPlaceableData(Stream stream) : base(stream) { }
        public teMapPlaceableData(BinaryReader reader) : base(reader) { }
        
        protected override void Read(BinaryReader reader) {
            Header = reader.Read<PlaceableDataHeader>();
            
            if (Header.PlaceableOffset > 0) {
                reader.BaseStream.Position = Header.PlaceableOffset;
                
                CommonStructures = new CommonStructure[Header.PlaceableCount];
                Placeables = new IMapPlaceable[Header.PlaceableCount];

                for (int i = 0; i < Header.PlaceableCount; i++) {
                    long beforePos = reader.BaseStream.Position;

                    CommonStructure commonStructure = reader.Read<CommonStructure>();
                    CommonStructures[i] = commonStructure;

                    Placeables[i] = Manager.CreateType(commonStructure, reader);
                    
                    reader.BaseStream.Position = beforePos + CommonStructures[i].Size;
                }

                if (CommonStructures.Length > 0 && CommonStructures[0].Type == teMAP_PLACEABLE_TYPE.ENTITY && Header.InstanceDataOffset > 0) {
                    int execCount = 0;
                    reader.BaseStream.Position = Header.InstanceDataOffset+16;
                    foreach (IMapPlaceable placeable in Placeables) {
                        if (!(placeable is teMapPlaceableEntity entity)) continue;

                        entity.InstanceData = new STUComponentInstanceData[entity.Header.InstanceDataCount];
                        for (int i = 0; i < entity.Header.InstanceDataCount; i++) {
                            long beforePos = reader.BaseStream.Position;
                            try {
                                teStructuredData structuredData = new teStructuredData(reader);
                                entity.InstanceData[i] = structuredData.GetInstance<STUComponentInstanceData>();
                                AlignPosition(beforePos, reader, structuredData);
                            } catch (Exception) {
                                execCount++;
                                AlignPositionInternal(reader, beforePos + 8); // try and recover
                            }
                        }
                    }

                    if (execCount > 0) {
                        Debugger.Log(0, "teMapChunk", $"Threw {execCount} exceptions when trying to parse entity instance data\r\n");
                    }
                }
            }
        }
        
        private void AlignPosition(long start, BinaryReader reader, teStructuredData stu) {
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
        }
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
            public teResourceGUID Identifier2;  // 01C
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public uint InstanceDataCount;
            
            // todo: more data here
        }

        public Structure Header;
        public STUComponentInstanceData[] InstanceData;

        public void Read(BinaryReader reader) {
            Header = reader.Read<Structure>();
        }
    }
    
    public class teMapPlaceableLight : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.LIGHT;
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teQuat Rotation;
            public teVec3 Translation;
            public uint Unknown1A;
            public uint Unknown1B;
            public byte Unknown2A;
            public byte Unknown2B;
            public byte Unknown2C;
            public byte Unknown2D;
            public uint Unknown3A;
            public uint Unknown3B;
            public teLIGHTTYPE Type;
            public teColorRGB Color;
            public teVec3 UnknownPos1;
            public teQuat UnknownQuat1;
            public float LightFOV;      // Cone angle, in degrees. Set to -1.0 for Point Lights
            public teVec3 UnknownPos2;
            public teQuat UnknownQuat2;
            public teVec3 UnknownPos3;
            public teQuat UnknownQuat3;
            public float Unknown4A;
            public float Unknown4B;
            public uint Unknown5;       // definitly an Int of some kind
            public short Unknown6A;
            public short Unknown6B;
            public uint Unknown7A;
            public uint Unknown7B;
        }

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
    
    public class teMapPlaceableModel : IMapPlaceable {
        public teMAP_PLACEABLE_TYPE Type => teMAP_PLACEABLE_TYPE.MODEL;
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Model;
            public teResourceGUID ModelLook;
            public teVec3 Translation;
            public teVec3 Scale;
            public teQuat Rotation;
            public teQuat Unknown;
        }

        public Structure Header;

        public void Read(BinaryReader reader) {
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

        public IMapPlaceable CreateType(teMapPlaceableData.CommonStructure commonStructure, BinaryReader reader) {
            if (Types.TryGetValue(commonStructure.Type, out Type placeableType)) {
                IMapPlaceable value = (IMapPlaceable)Activator.CreateInstance(placeableType);
                value.Read(reader);
                return value;
            }

            if (_misingTypes.Add(commonStructure.Type)) {
                Debugger.Log(0, "teMapPlaceableManager", $"Unhandled placeable type: {commonStructure.Type}\r\n");
            }
            return null;
        }
    }
}