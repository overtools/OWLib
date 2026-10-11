using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using TankLib.STU.Primitives;

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
        // Share typed delegates across readers to avoid reflection on every struct value.
        private static readonly ConcurrentDictionary<Type, Func<BinaryReader, object>> StructReaders = new ConcurrentDictionary<Type, Func<BinaryReader, object>>();

        /// <summary>Reads built-in primitive arrays directly into their destination storage.</summary>
        /// <returns>True if the array was read; false if scalar decoding is required. A false result consumes no data.</returns>
        internal static bool TryDeserializePrimitiveArray(teStructuredDataMgr manager, teStructuredData data, Array target) {
            // STU stores little-endian values; copying bytes uses the host representation.
            if (!BitConverter.IsLittleEndian) {
                return false;
            }

            Type elementType = target.GetType().GetElementType();
            if (!manager.Factories.TryGetValue(elementType, out var factory)) {
                return false;
            }

            // Exact factory types preserve registrations that customize primitive decoding.
            Type factoryType = factory.GetType();
            // Legacy uint arrays use the main stream, matching the scalar factory.
            BinaryReader reader = elementType == typeof(uint) && data.Format != teStructuredDataFormat.V2 ? data.Data : data.DynData;
            // Custom BinaryReader subclasses may override the scalar primitive methods.
            if (reader?.GetType() != typeof(BinaryReader)) {
                return false;
            }

            if (elementType == typeof(byte) && target is byte[] bytes && factoryType == typeof(STUu8Primitive)) {
                ReadPrimitiveArray(reader, bytes);
            } else if (elementType == typeof(sbyte) && target is sbyte[] signedBytes && factoryType == typeof(STUs8Primitive)) {
                ReadPrimitiveArray(reader, signedBytes);
            } else if (elementType == typeof(ushort) && target is ushort[] unsignedShorts && factoryType == typeof(STUu16Primitive)) {
                ReadPrimitiveArray(reader, unsignedShorts);
            } else if (elementType == typeof(short) && target is short[] shorts && factoryType == typeof(STUs16Primitive)) {
                ReadPrimitiveArray(reader, shorts);
            } else if (elementType == typeof(uint) && target is uint[] unsignedInts && factoryType == typeof(STUu32Primitive)) {
                ReadPrimitiveArray(reader, unsignedInts);
            } else if (elementType == typeof(int) && target is int[] ints && factoryType == typeof(STUs32Primitive)) {
                ReadPrimitiveArray(reader, ints);
            } else if (elementType == typeof(ulong) && target is ulong[] unsignedLongs && factoryType == typeof(STUu64Primitive)) {
                ReadPrimitiveArray(reader, unsignedLongs);
            } else if (elementType == typeof(long) && target is long[] longs && factoryType == typeof(STUs64Primitive)) {
                ReadPrimitiveArray(reader, longs);
            } else if (elementType == typeof(float) && target is float[] singles && factoryType == typeof(STUf32Primitive)) {
                ReadPrimitiveArray(reader, singles);
            } else if (elementType == typeof(double) && target is double[] doubles && factoryType == typeof(STUf64Primitive)) {
                ReadPrimitiveArray(reader, doubles);
            } else {
                return false;
            }

            return true;
        }

        /// <summary>Fills an unmanaged array in bounded chunks, retrying partial reads.</summary>
        /// <exception cref="EndOfStreamException">The stream ends before the array is filled.</exception>
        private static void ReadPrimitiveArray<T>(BinaryReader reader, T[] target) where T : unmanaged {
            // Bound each span so even arrays larger than Int32.MaxValue bytes can be read.
            for (int offset = 0; offset < target.Length;) {
                int count = System.Math.Min(target.Length - offset, 1024 * 1024);
                Span<byte> remaining = MemoryMarshal.AsBytes(target.AsSpan(offset, count));
                while (!remaining.IsEmpty) {
                    int read = reader.Read(remaining);
                    if (read == 0) {
                        throw new EndOfStreamException("Unable to read beyond the end of the stream.");
                    }

                    remaining = remaining.Slice(read);
                }
                offset += count;
            }
        }

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
                return StructReaders.GetOrAdd(target.FieldType, CreateStructReader)(data.Data);
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
                return StructReaders.GetOrAdd(elementType, CreateStructReader)(data.DynData);
            }

            throw new NotImplementedException();
        }

        /// <summary>Creates a typed struct delegate for caching and reuse by scalar and array decoding.</summary>
        private static Func<BinaryReader, object> CreateStructReader(Type type) {
            MethodInfo method = typeof(DefaultStructuredDataFieldReader)
                .GetMethod(nameof(ReadStruct), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            return (Func<BinaryReader, object>)method.CreateDelegate(typeof(Func<BinaryReader, object>));
        }

        /// <summary>Reads and boxes a struct while preserving the reflection invocation exception contract.</summary>
        private static object ReadStruct<T>(BinaryReader reader) where T : unmanaged {
            try {
                return reader.Read<T>();
            } catch (Exception exception) {
                // Keep the exception contract of the previous MethodInfo.Invoke call.
                throw new TargetInvocationException(exception);
            }
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
            // if (data.Format == teStructuredDataFormat.V1) {
            //     var n = data.Data.ReadInt64();
            //     if (n > 0) {
            //
            //     }
            // }

            STUInstance instanceObj = (STUInstance) DeserializeInternal(manager, data, field, target);
            instanceObj.Usage = TypeUsage.Inline;
            target.SetValue(instance, instanceObj);
        }

        public override void Deserialize_Array(teStructuredDataMgr manager, teStructuredData data, STUField_Info field, Array target, int index) {
            // todo: ???
            //if (data.Format == teStructuredDataFormat.V1 && index == 0) {
            //    var n = data.Data.ReadInt64();
            //    if (n > 0) {
            //
            //    }
            //}
            STUInstance instanceObj = (STUInstance) DeserializeArrayInternal(manager, data, field, target);
            instanceObj.Usage = TypeUsage.InlineArray;
            target.SetValue(instanceObj, index);
        }
    }
}