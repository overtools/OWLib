using System;

namespace TankLib.STU {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class STUAttribute : Attribute {
        /// <summary>Instance name CRC32 (mangled in 1.14+)</summary>
        public uint Hash;

        public uint m_size;
        
        public STUAttribute(uint hash, uint size) {
            Hash = hash;
            m_size = size;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class STUFieldAttribute : Attribute {
        /// <summary>Field name CRC32 (mangled in 1.14+)</summary>
        public uint Hash;
        
        /// <summary>Real value of name hash</summary>
        public string Name;
        
        /// <summary>IStructuredDataFieldReader to use</summary>
        public Type ReaderType;

        public uint m_offset;

        public STUFieldAttribute(uint hash, uint offset=0) {
            Hash = hash;
            ReaderType = typeof(DefaultStructuredDataFieldReader);
            m_offset = offset;
        }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class STUEnumAttribute : Attribute {
        /// <summary>Enum name CRC32 (mangled in 1.14+)</summary>
        public uint Hash;
        
        /// <summary>Real value of name hash</summary>
        public string Name;

        public STUEnumAttribute(uint hash) {
            Hash = hash;
        }

        public STUEnumAttribute(uint hash, string name) {
            Hash = hash;
            Name = name;
        }
    }
}