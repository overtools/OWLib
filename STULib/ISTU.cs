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
    // ReSharper disable once InconsistentNaming
    public abstract class ISTU : IDisposable {
        protected internal static Dictionary<uint, Type> _InstanceTypes;
        protected internal static Dictionary<uint, Type> _EnumTypes;
        protected internal static Dictionary<Type, string> _InstanceNames;

        public static Dictionary<uint, Type> InstanceTypes => _InstanceTypes;
        public static Dictionary<uint, Type> EnumTypes => _EnumTypes;

        protected static Dictionary<uint, List<STUSuppressWarningAttribute>> SuppressedWarnings;

        public HashSet<uint> TypeHashes { get; protected set; } = new HashSet<uint>();
        
        internal Dictionary<KeyValuePair<object, FieldInfo>, int> EmbedRequests;
        internal Dictionary<Array, int[]> EmbedArrayRequests;
        internal List<KeyValuePair<KeyValuePair<Type, object>, KeyValuePair<uint, ulong>>> HashmapRequests; 

        public static bool IsSimple(Type type) {
            return type.IsPrimitive || type == typeof(string) || type == typeof(object);
        }

        internal static bool CheckCompatVersion(FieldInfo field, uint buildVersion) {
           
            BuildVersionRangeAttribute[] buildVersionRanges = (BuildVersionRangeAttribute[])Attribute.GetCustomAttributes(field, typeof(BuildVersionRangeAttribute), false);

            return !buildVersionRanges.Any() || buildVersionRanges.Any(buildVersionRange => buildVersion >= buildVersionRange?.Min && buildVersion <= buildVersionRange.Max);
        }


        internal static FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null && !type.BaseType.Namespace.StartsWith("System.")) {
                parent = GetFields(type.BaseType);
            }
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
        }

        protected internal static void LoadInstanceTypes() {
            _InstanceTypes = new Dictionary<uint, Type>();
            _InstanceNames = new Dictionary<Type, string>();
            SuppressedWarnings = new Dictionary<uint, List<STUSuppressWarningAttribute>>();

            Assembly asm = typeof(STUInstance).Assembly;
            Type t = typeof(STUInstance);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            
            foreach (Type type in types) {
                STUAttribute[] attributes = (STUAttribute[])Attribute.GetCustomAttributes(type, typeof(STUAttribute), true);
                
                STUSuppressWarningAttribute[] warningAttributes = (STUSuppressWarningAttribute[])Attribute.GetCustomAttributes(type, typeof(STUSuppressWarningAttribute), true);
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
                    _InstanceNames[type] = attrib.Name;
                    _InstanceTypes[attrib.Checksum] = type;
                    break;  // todo: MAJOR: fixes inherited types overriding parent checksums
                }
                foreach (STUSuppressWarningAttribute warn in warningAttributes) {
                    if (!SuppressedWarnings.ContainsKey(warn.ThisInstance)) {
                        SuppressedWarnings[warn.ThisInstance] = new List<STUSuppressWarningAttribute>();
                    }
                    SuppressedWarnings[warn.ThisInstance].Add(warn);
                }
            }
            Type t2 = typeof(Enum);
            List<Type> types2 = asm.GetTypes().Where(type => type != t2 && t2.IsAssignableFrom(type)).ToList();
            _EnumTypes = new Dictionary<uint, Type>();
            foreach (Type type in types2) {
                STUEnumAttribute attribute = type.GetCustomAttribute<STUEnumAttribute>();
                if (attribute == null) continue;
                if (attribute.Checksum == 0) continue;
                // if (_EnumTypes.ContainsKey(attribute.Checksum)) {
                //     throw new Exception($"Collision of STUEnum checksums ({attribute.Checksum:X})");
                // }
                _EnumTypes[attribute.Checksum] = type;
            }
        }

        public abstract IEnumerable<STUInstance> Instances { get; }
        public abstract uint Version { get; }

        public static string GetName<T>() {
            return GetName(typeof(T));
        }

        public static string GetName(Type t) {
            if (_InstanceNames != null && _InstanceNames.ContainsKey(t)) {
                return _InstanceNames[t];
            }
            return null;
        }

        public static ISTU NewInstance(Stream stream, uint owVersion, Type type = null) {
            if (stream == null) {
                return null;
            }
            long pos = stream.Position;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                if (type != null) {
                    return (ISTU)Activator.CreateInstance(type, stream, owVersion);
                    // return new Impl.Version2HashComparer.Version2Comparer(stream, owVersion);  // for debug
                    // return new Impl.Version2HashComparer.MapComparer(stream, owVersion);  // for debug
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

        public static void Clear() {
            _InstanceTypes = new Dictionary<uint, Type>();
            _EnumTypes = new Dictionary<uint, Type>();
            _InstanceNames = new Dictionary<Type, string>();
            SuppressedWarnings = new Dictionary<uint, List<STUSuppressWarningAttribute>>();
        }

        public abstract void Dispose();
    }
}
