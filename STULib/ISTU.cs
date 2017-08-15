using System;
using System.Collections.Generic;
using System.Data.HashFunction.CRCStandards;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using STULib.Impl;
using static STULib.Types.Generic.Common;

namespace STULib {
    public abstract class ISTU : IDisposable {
        internal static Dictionary<uint, Type> instanceTypes;
        internal static Dictionary<Type, string> instanceNames;

        internal static void LoadInstanceTypes() {
            instanceTypes = new Dictionary<uint, Type>();
            instanceNames = new Dictionary<Type, string>();
            
            Assembly asm = typeof(STUInstance).Assembly;
            Type t = typeof(STUInstance);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                STUAttribute attrib = type.GetCustomAttribute<STUAttribute>();
                if (attrib == null) {
                    continue;
                }
                if (attrib.Name == null) {
                    attrib.Name = type.Name;
                }
                if (!attrib.Name.StartsWith("STU") && type.Namespace != null) {
                    IEnumerable<string> parts = type.Namespace.Split('.').Reverse();
                    foreach (string part in parts) {
                        attrib.Name = part + "_" + attrib.Name;
                        if (attrib.Name.StartsWith("STU")) {
                            continue;
                        }
                    }
                }
                if (attrib.Checksum == 0) {
                    attrib.Checksum =
                        BitConverter.ToUInt32(new CRC32().ComputeHash(Encoding.ASCII.GetBytes(attrib.Name.ToLowerInvariant())), 0);
                }
                instanceNames[type] = attrib.Name;
                instanceTypes[attrib.Checksum] = type;
            }
        }

        public abstract IEnumerator<STUInstance> Instances { get; }
        public abstract uint Version { get; }

        public static string GetName<T>() {
            return GetName(typeof(T));
        }

        public static string GetName(Type t) {
            if (instanceNames != null && instanceNames.ContainsKey(t)) {
                return instanceNames[t];
            }
            return null;
        }

        public static ISTU NewInstance(Stream stream, uint owVersion) {
            long pos = stream.Position;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                if (Version1.IsValidVersion(reader)) {
                    stream.Position = pos;
                    return new Version1(stream, owVersion);
                }
                if (Version2.IsValidVersion(reader)) {
                    stream.Position = pos;
                    return new Version2(stream, owVersion);
                }
                throw new InvalidDataException("Data stream is not a STU file");
            }
        }

        public abstract void Dispose();
    }
}
