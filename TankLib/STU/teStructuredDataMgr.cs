using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
            AddAssemblyInstances(assembly);
            AddAssemblyFieldReaders(assembly);
            AddAssemblyFactories(assembly);
        }

        private List<Type> GetAssemblyTypes<T>(Assembly assembly) {
            List<Type> types = assembly.GetTypes().Where(type => type != typeof(T) && typeof(T).IsAssignableFrom(type)).ToList();
            return types;
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
                if (type.IsInterface) {
                    continue;
                }
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
            List<uint> fieldOrderTemp = new List<uint>();

            foreach (FieldInfo field in type.GetFields()) {
                STUFieldAttribute fieldAttribute = field.GetCustomAttribute<STUFieldAttribute>();
                if (fieldAttribute == null) continue;
                if (fieldAttribute.Hash == 0) continue;
                fieldOrderTemp.Add(fieldAttribute.Hash);

                FieldAttributes[attribute.Hash][fieldAttribute.Hash] = new KeyValuePair<FieldInfo, STUFieldAttribute>(field, fieldAttribute);
            }
            InstanceFields[attribute.Hash] = fieldOrderTemp.ToArray();
        }

        public STUInstance CreateInstance(uint hash) {
            if (Instances.ContainsKey(hash)) {
                return (STUInstance)Activator.CreateInstance(Instances[hash]);
            }
            Debugger.Log(0, "teStructuredDataMgr", $"Unhandled instance: {hash:X8}\r\n");
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