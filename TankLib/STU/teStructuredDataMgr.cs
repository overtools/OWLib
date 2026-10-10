using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using static TankLib.Util;

namespace TankLib.STU {
    /// <summary>Manages StructuredData objects. Singleton</summary>
    public class teStructuredDataMgr {
        public Dictionary<Type, IStructuredDataPrimitiveFactory> Factories;
        public Dictionary<Type, IStructuredDataFieldReader> FieldReaders;

        public Dictionary<uint, Type> Instances;
        public Dictionary<uint, Type> Enums;
        public Dictionary<Type, uint> InstancesInverted;

        public Dictionary<uint, STUAttribute> InstanceAttributes;
        public Dictionary<uint, Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>>> FieldAttributes;
        public Dictionary<uint, uint[]> InstanceFields;  // in the correct order

        private readonly HashSet<uint> _missingInstances;
        private readonly Dictionary<Type, Dictionary<int, STUFieldAttribute>> _declaredFieldAttributes = new Dictionary<Type, Dictionary<int, STUFieldAttribute>>();
        private readonly Dictionary<Type, uint[]> _fieldOrders = new Dictionary<Type, uint[]>();

        public teStructuredDataMgr() {
            Factories = new Dictionary<Type, IStructuredDataPrimitiveFactory>();
            FieldReaders = new Dictionary<Type, IStructuredDataFieldReader>();
            
            Instances = new Dictionary<uint, Type>();
            Enums = new Dictionary<uint, Type>();
            InstancesInverted = new Dictionary<Type, uint>();
            
            InstanceAttributes = new Dictionary<uint, STUAttribute>();
            FieldAttributes = new Dictionary<uint, Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>>>();
            InstanceFields = new Dictionary<uint, uint[]>();
            
            Assembly assembly = typeof(teStructuredDataMgr).Assembly;
            Type[] types = assembly.GetTypes();
            foreach (Type type in types) {
                if (type != typeof(STUInstance) && typeof(STUInstance).IsAssignableFrom(type)) AddInstance(type);
            }
            foreach (Type type in types) {
                if (!type.IsInterface && typeof(IStructuredDataFieldReader).IsAssignableFrom(type)) AddFieldReader(type);
            }
            foreach (Type type in types) {
                if (!type.IsInterface && typeof(IStructuredDataPrimitiveFactory).IsAssignableFrom(type)) AddFactory(type);
            }
            
            _missingInstances = new HashSet<uint>();
        }
        
        public void AddAssemblyFactories(Assembly assembly) {
            foreach (Type type in GetAssemblyTypes<IStructuredDataPrimitiveFactory>(assembly)) {
                if (type.IsInterface) continue;
                AddFactory(type);
            }
        }
        
        public void AddFactory(Type type) {
            IStructuredDataPrimitiveFactory reader = (IStructuredDataPrimitiveFactory) Activator.CreateInstance(type);
            Factories[reader.GetValueType()] = reader;
        }

        public void AddAssemblyFieldReaders(Assembly assembly) {
            foreach (Type type in GetAssemblyTypes<IStructuredDataFieldReader>(assembly)) {
                if (type.IsInterface) continue;
                AddFieldReader(type);
            }
        }

        public void AddFieldReader(Type type) {
            IStructuredDataFieldReader reader = (IStructuredDataFieldReader) Activator.CreateInstance(type);
            FieldReaders[type] = reader;
        }
        
        public void AddAssemblyInstances(Assembly assembly) {
            foreach (Type type in GetAssemblyTypes<STUInstance>(assembly)) {
                AddInstance(type);
            }
        }

        public void AddInstance(Type type) {
            STUAttribute attribute = type.GetCustomAttribute<STUAttribute>();
            if (attribute == null) return;
            if (attribute.Hash == 0) return;

            InstanceAttributes[attribute.Hash] = attribute;
            Instances[attribute.Hash] = type;
            InstancesInverted[type] = attribute.Hash;
            FieldAttributes[attribute.Hash] = new Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>>();
            //List<uint> fieldOrderTemp = new List<uint>();

            foreach (FieldInfo field in type.GetFields()) {
                STUFieldAttribute template = GetDeclaredFieldAttributes(field.DeclaringType).GetValueOrDefault(field.MetadataToken);
                if (template == null) continue;
                // Public dictionaries must own their attributes: callers can mutate them, and
                // inherited fields appear in multiple instances. Preserve custom attribute subclasses.
                STUFieldAttribute fieldAttribute = template.GetType() == typeof(STUFieldAttribute)
                    ? new STUFieldAttribute(template.Hash, template.m_offset) { Name = template.Name, ReaderType = template.ReaderType }
                    : field.GetCustomAttribute<STUFieldAttribute>();
                if (fieldAttribute.Hash == 0) continue;

                FieldAttributes[attribute.Hash][fieldAttribute.Hash] = new KeyValuePair<FieldInfo, STUFieldAttribute>(field, fieldAttribute);
            }
            
            //InstanceFields[attribute.Hash] = fieldOrderTemp.ToArray();
            InstanceFields[attribute.Hash] = (uint[]) GetCachedFieldOrder(type).Clone();
        }

        private Dictionary<int, STUFieldAttribute> GetDeclaredFieldAttributes(Type type) {
            if (_declaredFieldAttributes.TryGetValue(type, out var attributes)) return attributes;
            attributes = new Dictionary<int, STUFieldAttribute>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                STUFieldAttribute attribute = field.GetCustomAttribute<STUFieldAttribute>();
                if (attribute != null) attributes[field.MetadataToken] = attribute;
            }
            _declaredFieldAttributes[type] = attributes;
            return attributes;
        }

        private uint[] GetCachedFieldOrder(Type type) {
            if (_fieldOrders.TryGetValue(type, out uint[] order)) return order;
            List<uint> fields = new List<uint>();
            bool cacheable = true;
            if (type.BaseType != typeof(STUInstance)) {
                fields.AddRange(GetCachedFieldOrder(type.BaseType));
                cacheable = _fieldOrders.ContainsKey(type.BaseType);
            }
            Dictionary<int, STUFieldAttribute> attributes = GetDeclaredFieldAttributes(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                if (!attributes.TryGetValue(field.MetadataToken, out STUFieldAttribute attribute)) continue;
                if (attribute.GetType() != typeof(STUFieldAttribute)) {
                    attribute = field.GetCustomAttribute<STUFieldAttribute>();
                    cacheable = false;
                }
                if (attribute.Hash != 0) fields.Add(attribute.Hash);
            }
            order = fields.ToArray();
            if (cacheable) _fieldOrders[type] = order;
            return order;
        }

        public List<uint> GetFieldOrder(Type type) {
            return new List<uint>(GetCachedFieldOrder(type));
        }

        public STUInstance CreateInstance(uint hash) {
            if (Instances.TryGetValue(hash, out Type instanceType)) {
                return (STUInstance)Activator.CreateInstance(instanceType);
            }

            lock (_missingInstances) {
                if (_missingInstances.Add(hash)) {
                    Debugger.Log(0, "teStructuredDataMgr", $"Unhandled instance: {hash:X8}\r\n");
                }
            }
            return null;
        }

        public void WipeInstances() {
            Instances.Clear();
            InstancesInverted.Clear();
            InstanceAttributes.Clear();
            FieldAttributes.Clear();
            InstanceFields.Clear();
        }

        public void WipeEnums() {
            Enums.Clear();
        }
    }
}