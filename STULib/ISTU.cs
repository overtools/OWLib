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
        protected internal static Dictionary<uint, Type> instanceTypes;
        protected internal static Dictionary<Type, string> instanceNames;

        internal static bool CheckCompatVersion(FieldInfo field, uint buildVersion) {
            IEnumerable<BuildVersionRangeAttribute> buildVersionRanges = field.GetCustomAttributes<BuildVersionRangeAttribute>();
            IEnumerable<BuildVersionRangeAttribute> buildVersionRangeAttributes = buildVersionRanges as BuildVersionRangeAttribute[] ?? buildVersionRanges.ToArray();

            return !buildVersionRangeAttributes.Any() || buildVersionRangeAttributes.Any(buildVersionRange => buildVersion >= buildVersionRange?.Min && buildVersion <= buildVersionRange.Max);
        }


        internal static FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null && !type.BaseType.Namespace.StartsWith("System.")) {
                parent = GetFields(type.BaseType);
            }
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
        }

        protected internal static void LoadInstanceTypes() {
            instanceTypes = new Dictionary<uint, Type>();
            instanceNames = new Dictionary<Type, string>();

            Assembly asm = typeof(STUInstance).Assembly;
            Type t = typeof(STUInstance);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                IEnumerable<STUAttribute> stuAttributes = type.GetCustomAttributes<STUAttribute>();
                IEnumerable<STUAttribute> attributes = stuAttributes as STUAttribute[] ?? stuAttributes.ToArray();
                if (!attributes.Any()) {
                    continue;
                }
                foreach (STUAttribute attrib in attributes) {
                    if (attrib == null) {
                        continue;
                    }
                    if (attrib.Name != null) {
                        uint crcCheck = BitConverter.ToUInt32(
                                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(attrib.Name.ToLowerInvariant())), 0);
                        if (attrib.Checksum != crcCheck) {
                            // Debugger.Log(0, "STU", $"[STU] Invalid name for {attrib.Name}, checksum mismatch ({attrib.Checksum}, {crcCheck})\n");
                            // No longer a CRC32 of the name
                        }
                    } else {
                        attrib.Name = type.Name;
                    }
                    if (!attrib.Name.StartsWith("STU") && type.Namespace != null) {
                        IEnumerable<string> parts = type.Namespace.Split('.').Reverse();
                        foreach (string part in parts) {
                            attrib.Name = part + "_" + attrib.Name;
                            if (attrib.Name.StartsWith("STU")) {
                                break;
                            }
                        }
                    }
                    if (attrib.Checksum == 0) {
                        attrib.Checksum =
                            BitConverter.ToUInt32(
                                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(attrib.Name.ToLowerInvariant())), 0);
                    }
                    instanceNames[type] = attrib.Name;
                    instanceTypes[attrib.Checksum] = type;
                }
            }
        }

        public abstract IEnumerable<STUInstance> Instances { get; }
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

        public static ISTU NewInstance(Stream stream, uint owVersion, Type type = null) {
            long pos = stream.Position;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                if (type != null) {
                    return (ISTU)Activator.CreateInstance(type, stream, owVersion);
                }
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
