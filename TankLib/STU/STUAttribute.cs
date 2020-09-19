using System;

namespace TankLib.STU {
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class STUAttribute : Attribute {
        /// <summary>Instance name CRC32 (mangled in 1.14+)</summary>
        public uint Hash;
        
        /// <summary>Real value of name hash</summary>
        public string Name;
        
        public STUAttribute(uint hash) {
            Hash = hash;
        }

        public STUAttribute(uint hash, string name) {
            Hash = hash;
            Name = name;
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

        public STUFieldAttribute(uint hash) {
            Hash = hash;
            ReaderType = typeof(DefaultStructuredDataFieldReader);
        }
        
        public STUFieldAttribute(uint hash, string name) {
            Hash = hash;
            Name = name;
            ReaderType = typeof(DefaultStructuredDataFieldReader);
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