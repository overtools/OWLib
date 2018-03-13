using System;

namespace TankLib.STU {
    public class STUAttribute : Attribute {
        /// <summary>Instance name CRC32 (mangled)</summary>
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

    public class STUFieldAttribute : Attribute {
        /// <summary>Field name CRC32 (mangled)</summary>
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
        
        public STUFieldAttribute(uint hash, Type readerType) {
            Hash = hash;
            ReaderType = readerType;
        }
        
        public STUFieldAttribute(uint hash, string name, Type readerType) {
            Hash = hash;
            Name = name;
            ReaderType = readerType;
        }
    }
}