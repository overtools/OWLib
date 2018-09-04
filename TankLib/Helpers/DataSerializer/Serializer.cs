using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TankLib.Helpers.DataSerializer
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class ReadableType : Attribute
    {
        public virtual object Read(BinaryReader reader, FieldInfo field)
        {
            throw new NotImplementedException();
        }

        public virtual void Write(BinaryWriter writer, FieldInfo field, object obj)
        {
            throw new NotImplementedException();
        }

        public virtual long GetSize(FieldInfo field, object obj)
        {
            throw new NotImplementedException();
        }

        public virtual long GetNoDataStartSize(FieldInfo field, object obj)
        {
            return 0;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class ConditionalType : Attribute
    {
        public virtual bool ShouldDo(FieldInfo[] fields, object owner)
        {
            throw new NotImplementedException();
        }
    }

    public class ReadableData
    {
        [Logical.Skip]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        // ReSharper disable once InconsistentNaming
        internal Dictionary<string, ReadableType> __readabledata_attrs = new Dictionary<string, ReadableType>();

        public long GetMutableSize(Type t, object obj)
        {
            FieldInfo[] fields = ReaderHelper.GetFields(t);
            long size = 0;
            foreach (FieldInfo field in fields)
            {
                ReadableType type = field.GetCustomAttributes<ReadableType>().FirstOrDefault();
                if (type == null) type = new Logical.Default();

                IEnumerable<ConditionalType> conditions = field.GetCustomAttributes<ConditionalType>();

                if (field.FieldType.IsArray || field.GetValue(obj) is string)
                {
                    size += type.GetSize(field, field.GetValue(obj));
                }
                else if (field.FieldType.IsClass)
                {
                    size += type.GetSize(field, field.GetValue(obj));
                }
                else if (conditions.Any())
                {
                    size += type.GetSize(field, field.GetValue(obj));
                }
            }

            return size;
        }

        public long GetMutableSize()
        {
            return GetMutableSize(GetType(), this);
        }

        public static long GetArraySize(Type t, object obj)
        {
            if (obj == null) return 0;
            object[] array = ((IEnumerable)obj).Cast<object>().ToArray();
            long size = 0;

            foreach (object item in array)
            {
                size += GetObjectSize(t.GetElementType(), item);
            }

            return size;
        }

        public long GetSize()
        {
            return GetObjectSize(GetType(), this);
        }

        public static long GetClassSize(Type t, object obj, string breakStartField = null, string breakEndField = null, bool breakIncludeBaseStartSize = false)
        {
            FieldInfo[] fields = ReaderHelper.GetFields(t);
            long size = 0;
            Dictionary<string, ReadableType> attrs = null;

            if (t.IsSubclassOf(typeof(ReadableData)))
            {
                attrs = ((ReadableData)obj).__readabledata_attrs;  // get the same Attribute instances as before
            }

            foreach (FieldInfo field in fields)
            {
                ReadableType type = field.GetCustomAttributes<ReadableType>()
                    .FirstOrDefault(x => x.GetType() != typeof(Logical.Conditional));
                if (type == null) type = new Logical.Default();
                if (breakStartField != null && field.Name == breakStartField)
                    return size + (breakIncludeBaseStartSize ? type.GetNoDataStartSize(field, field.GetValue(obj)) : 0);

                IEnumerable<ConditionalType> conditions = field.GetCustomAttributes<ConditionalType>();
                bool skip = false;
                foreach (ConditionalType condition in conditions)
                {
                    if (!condition.ShouldDo(fields, obj)) skip = true;
                }

                if (skip == false)
                {
                    if (attrs != null && attrs.ContainsKey(field.Name)) type = attrs[field.Name];  // restore
                    size += type.GetSize(field, field.GetValue(obj));
                }
                if (breakEndField != null && field.Name == breakEndField) return size;
            }

            if (breakEndField != null || breakStartField != null) return -1;

            return size;
        }

        public static long GetObjectSize(Type t, object obj)
        {
            long size = 0;

            bool isPrimitive = t.IsPrimitive || t == typeof(decimal);
            bool isStruct = t.IsValueType && !t.IsPrimitive;

            if (t.IsArray)
            {
                size += GetArraySize(t, obj);
            }
            else if (obj is string)
            {
                size += ((string)obj).Length;
            }
            else if (t.IsClass)
            {
                size += GetClassSize(t, obj);
            }
            else if (isPrimitive)
            {
                size += ReaderHelper.GetPrimitiveSize(t);
            }
            else if (t.IsEnum)
            {
                size += GetObjectSize(t.GetEnumUnderlyingType(), obj);
            }
            else if (isStruct)
            {
                size += GetClassSize(t, obj);
            }
            else
            {
                throw new Exception("error");
            }

            return size;
        }

        public long GetFieldStartPos(string field, bool breakIncludeBaseStartSize = false)
        {
            return GetClassSize(GetType(), this, field, null, breakIncludeBaseStartSize);
        }

        public long GetFieldEndPos(string field)
        {
            // todo: tests, write then read then compare
            // todo: array to list easy convert
            return GetClassSize(GetType(), this, null, field);
        }

        public virtual void Read(BinaryReader reader)
        {
            FieldInfo[] fields = ReaderHelper.GetFields(GetType());
            foreach (FieldInfo field in fields)
            {
                ReadableType type = field.GetCustomAttributes<ReadableType>().FirstOrDefault();

                IEnumerable<ConditionalType> conditions = field.GetCustomAttributes<ConditionalType>();
                bool skip = false;
                foreach (ConditionalType condition in conditions)
                {
                    if (!condition.ShouldDo(fields, this)) skip = true;
                }

                if (skip) continue;

                if (type == null) type = new Logical.Default();
                field.SetValue(this, type.Read(reader, field));
                if (__readabledata_attrs == null) __readabledata_attrs = new Dictionary<string, ReadableType>();
                __readabledata_attrs[field.Name] = type;
            }
        }
    }

    internal class ExpressionHelper
    {
        public int BitwiseAnd(byte b1, int b2)
        {  // for some reason DynamicExpresso doesn't support &
            return b1 & b2;
        }
    }

    internal static class ReaderHelper
    {
        public static FieldInfo[] GetFields(Type type)
        {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null && !type.BaseType.Namespace.StartsWith("System."))
            {
                parent = GetFields(type.BaseType);
            }
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
        }

        public static int GetPrimitiveSize(Type t)
        {
            switch (t.Name)
            {
                case "UInt64":
                case "Int64":
                    return 8;
                case "Int32":
                case "UInt32":
                case "Single":
                    return 4;
                case "UInt16":
                case "Int16":
                    return 2;
                case "Char":
                case "Byte":
                    return 1;

            }
            throw new Exception("unknown type");
        }
        public static uint ReadUInt24(this BinaryReader br)
        {
            byte[] buf = br.ReadBytes(3);
            return (uint)(buf[0] | (buf[1] << 8) | (buf[2] << 16));
        }

        public static BinaryReader CreateBinaryReader(this byte[] bytes) => new BinaryReader(new MemoryStream(bytes) { Position = 0 });

        public static object ReadType(Type type, BinaryReader reader)
        {
            bool isStruct = type.IsValueType && !type.IsPrimitive;

            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(sbyte)) return reader.ReadSByte();
            
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            
            if (type == typeof(float)) return reader.ReadSingle();

            if (type.IsEnum)
            {
                return ReadType(type.GetEnumUnderlyingType(), reader);
            }

            if (type.IsSubclassOf(typeof(ReadableData)))
            {
                ReadableData inst = (ReadableData)Activator.CreateInstance(type);
                inst.Read(reader);
                return inst;
            }

            if (isStruct)
            {
                // ReSharper disable once PossibleNullReferenceException
                MethodInfo method = typeof(Extensions).GetMethod(nameof(Extensions.Read)).MakeGenericMethod(type);
                return method.Invoke(reader, new object[] { reader });
            }

            throw new NotImplementedException();
        }
    }
}
