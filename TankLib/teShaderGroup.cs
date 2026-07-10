using System.IO;
using System.Runtime.InteropServices;
using TankLib.Helpers;

namespace TankLib {
    /// <summary>Tank ShaderGroup, file type 085</summary>
    public class teShaderGroup {
        [StructLayout(LayoutKind.Explicit)]
        public struct GroupHeader {
            /// <summary>Number of entries for both ShadersOffset and InstancesOffset</summary>
            [FieldOffset(100)] public int NumShaders; // 64 -> 76 -> 100
            /// <summary>ShaderCode array offset</summary>
            [FieldOffset(104)] public long ShadersOffset; // na -> 104
            /// <summary>ShaderInstance array offset</summary>
            [FieldOffset(120)] public long InstancesOffset; // 0 -> 100
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderQuality {
            public short VertexIndex;
            public short PixelIndex;

            public short UnkA;
            public short UnkB;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderUnk {
            public short Vertex;
            public short Pixel;

            public short UnkA;
            public short UnkB;
        }

        /// <summary>Header Data</summary>
        public GroupHeader Header;
        
        /// <summary>teShaderCode GUIDs (087)</summary>
        public teResourceGUID[] Shaders;

        /// <summary>teShaderInstance info GUIDs (040)</summary>
        public teResourceGUID[] Instances;

        /// <summary>Flags that are used to select the correct ShaderInstance pair based on parameters</summary>
        public ulong[] InstanceFlags;

        public ShaderQuality[] ShaderQualities;
        public ShaderUnk[] ShaderUnks;
        
        /// <summary>
        /// Read ShaderGroup from a stream
        /// </summary>
        /// <param name="stream">The stream to load from</param>
        public teShaderGroup(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        /// <summary>
        /// Read ShaderGroup from a BinaryReader
        /// </summary>
        /// <param name="reader">The reader to load from</param>
        public teShaderGroup(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            Header = reader.Read<GroupHeader>();
            
            if (Header.ShadersOffset > 0) {
                reader.BaseStream.Position = Header.ShadersOffset;
                Shaders = reader.ReadArray<teResourceGUID>(Header.NumShaders);
            }

            if (Header.InstancesOffset > 0) {
                reader.BaseStream.Position = Header.InstancesOffset;
                Instances = reader.ReadArray<teResourceGUID>(Header.NumShaders);
            }
        }

        /// <summary>
        /// DEPRECATED: teShaderGroup used to store a hash to match with a specific teShaderInstance. This mapping has since been broken as
        /// teShaderGroup now holds guids to both teShaderInstance and teShaderCode mapping them with a simple positional index instead.
        /// Get a ShaderInstance GUID from a "hash"
        /// </summary>
        /// <note>Mostly used on 088 groups</note>
        /// <param name="hash">"Hash" associated with a specific ShaderInstance</param>
        /// <returns></returns>
        public teResourceGUID GetShaderByHash(uint hash) {
            Logger.Error("teShaderGroup", $"GetShaderByHash doesn't work any longer for {hash:X16}, see documentation on GetShaderByHash."); 
            return (teResourceGUID) 0;
        }
    }
}