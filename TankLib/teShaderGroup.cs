using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderGroup, file type 085</summary>
    public class teShaderGroup {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct GroupHeader {
            /// <summary>ShaderInstance array offset</summary>
            public long InstanceOffset;
            
            public long OffsetB;
            public long OffsetC;
            public long OffsetD;
            public long OffsetE;
            
            /// <summary>teShaderSource that this group was generated from</summary>
            /// <remarks>088 GUID</remarks>
            public teResourceGUID ShaderSource;
            
            /// <summary>A virtual reference. Usage unknown</summary>
            /// <remarks>00F GUID</remarks>
            public teResourceGUID GUIDx00F;

            public ulong Unknown;
            
            /// <summary>ShaderInstance count</summary>
            public int InstanceCount;
        }

        /// <summary>Header Data</summary>
        public GroupHeader Header;
        
        /// <summary>ShaderInstances</summary>
        public teResourceGUID[] Instances;
        
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
            
            if (Header.InstanceOffset > 0) {
                reader.BaseStream.Position = Header.InstanceOffset;
                Instances = reader.ReadArray<teResourceGUID>(Header.InstanceCount);
            }
        }
    }
}