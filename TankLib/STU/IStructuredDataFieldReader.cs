using System;
using System.IO;
using System.Reflection;

namespace TankLib.STU {
    /// <summary>STU field reader interface</summary>
    public interface IStructuredDataFieldReader {
        void Deserialize(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, object instance, FieldInfo target);
        void Deserialize_Array(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target, int index);
        //void Serialize();
        //void Serialize_Array();
    }

    /// <summary>Standard STU field reader</summary>
    public class DefaultStructuredDataFieldReader : IStructuredDataFieldReader {
        public virtual void Deserialize(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, object instance, FieldInfo target) {
            target.SetValue(instance, DeserializeInternal(manager, data, field, target));
        }

        protected object DeserializeInternal(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, FieldInfo target) {
            if (manager.Factories.TryGetValue(target.FieldType, out var factory)) {
                return factory.Deserialize(data, field);
            }

            if (typeof(ISerializable_STU).IsAssignableFrom(target.FieldType)) {
                ISerializable_STU ret = (ISerializable_STU)Activator.CreateInstance(target.FieldType);
                ret.Deserialize(data, field);

                return ret;
            }

            if (target.FieldType.IsEnum) {
                IStructuredDataPrimitiveFactory enumFactory = manager.Factories[target.FieldType.GetEnumUnderlyingType()];
                return enumFactory.Deserialize(data, field);
            }
            
            bool isStruct = target.FieldType.IsValueType && !target.FieldType.IsPrimitive;

            if (isStruct) {
                MethodInfo method = typeof(Extensions).GetMethod(nameof(Extensions.Read))?.MakeGenericMethod(target.FieldType);
                return method?.Invoke(data.Data, new object[] { data.Data });
            }
            
            throw new NotImplementedException();
        }
        
        protected object DeserializeArrayInternal(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target) {
            Type elementType = target.GetType().GetElementType();
            if (elementType == null) throw new InvalidDataException("elementType is null");
            if (manager.Factories.TryGetValue(elementType, out var factory)) {
                return factory.DeserializeArray(data, field);
            }

            if (typeof(ISerializable_STU).IsAssignableFrom(elementType)) {
                ISerializable_STU ret = (ISerializable_STU)Activator.CreateInstance(elementType);
                ret.Deserialize_Array(data, field);

                return ret;
            }

            if (elementType.IsEnum) {
                IStructuredDataPrimitiveFactory enumFactory = manager.Factories[elementType.GetEnumUnderlyingType()];
                return Enum.ToObject(elementType, enumFactory.DeserializeArray(data, field));
            }
            
            bool isStruct = elementType.IsValueType && !elementType.IsPrimitive;

            if (isStruct) {
                MethodInfo method = typeof(Extensions).GetMethod(nameof(Extensions.Read))?.MakeGenericMethod(elementType);
                return method?.Invoke(data.DynData, new object[] { data.DynData });
            }
            
            throw new NotImplementedException();
        }

        public virtual void Deserialize_Array(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target, int index) {
            target.SetValue(DeserializeArrayInternal(manager, data, field, target), index);
        }
    }
    
    /// <summary>STU field reader for reading embedded instances</summary>
    public class EmbeddedInstanceFieldReader : IStructuredDataFieldReader {
        public void Deserialize(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, object instance, FieldInfo target) {
            if (data.Format == teStructuredDataFormat.V2) {
                int value = data.Data.ReadInt32();
            
                if (value == -1) return;
                if (value < data.Instances.Length)  {
                    STUInstance embeddedInstance = data.Instances[value];
                    if (embeddedInstance != null) {
                        embeddedInstance.Usage = TypeUsage.Embed;
                    }
                
                    target.SetValue(instance, embeddedInstance);
                }
            } else if (data.Format == teStructuredDataFormat.V1) {
                int value = data.Data.ReadInt32(); data.Data.ReadInt32();
            
                if (value <= 0) return;
                
                STUInstance embeddedInstance = data.GetInstanceAtOffset(value);
                if (embeddedInstance != null) {
                    embeddedInstance.Usage = TypeUsage.Embed;
                }
                
                target.SetValue(instance, embeddedInstance);
            }
        }
        
        public void Deserialize_Array(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target, int index) {
            if (data.Format == teStructuredDataFormat.V2) {
                int value = data.DynData.ReadInt32();
                data.DynData.ReadInt32(); // Padding for in-place deserialization
                if (value == -1) return;
                if (value < data.Instances.Length) {
                    STUInstance embeddedInstance = data.Instances[value];
                    if (embeddedInstance != null) {
                        embeddedInstance.Usage = TypeUsage.EmbedArray;
                    }

                    target.SetValue(embeddedInstance, index);
                } else {
                    throw new ArgumentOutOfRangeException(
                        $"Instance index is out of range. Id: {value}, Type: EmbeddedInstanceFieldReader, DynData offset: {data.DynData.Position() - 8}");
                }
            } else if (data.Format == teStructuredDataFormat.V1) {
                long offset = data.Data.ReadInt32(); data.Data.ReadInt32();
                if (offset == -1) return;
                
                STUInstance embeddedInstance = data.GetInstanceAtOffset(offset);
                if (embeddedInstance != null) {
                    embeddedInstance.Usage = TypeUsage.EmbedArray;
                }
                
                target.SetValue(embeddedInstance, index);
            }
        }
    }
    
    /// <summary>STU field reader for reading inline instances</summary>
    public class InlineInstanceFieldReader : DefaultStructuredDataFieldReader {
        public override void Deserialize(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, object instance, FieldInfo target) {
            if (data.Format == teStructuredDataFormat.V1) {
                var n = data.Data.ReadInt64();
                if (n > 0) {
                    
                }
            }

            STUInstance instanceObj = (STUInstance) DeserializeInternal(manager, data, field, target);
            instanceObj.Usage = TypeUsage.Inline;
            target.SetValue(instance, instanceObj);
        }
        
        public override void Deserialize_Array(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target, int index) {
            if (data.Format == teStructuredDataFormat.V1 && index == 0) {
                var n = data.Data.ReadInt64();
                if (n > 0) {
                    
                }
            }
            STUInstance instanceObj = (STUInstance) DeserializeArrayInternal(manager, data, field, target);
            instanceObj.Usage = TypeUsage.InlineArray;
            target.SetValue(instanceObj, index);
        }
    }
}