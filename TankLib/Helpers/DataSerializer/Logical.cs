using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DynamicExpresso;
using ZstdNet;

namespace TankLib.Helpers.DataSerializer
{
    public class Logical
    {
        public class Skip : ConditionalType
        {
            public override bool ShouldDo(FieldInfo[] fields, object owner)
            {
                return false;
            }
        }

        public class Default : ReadableType
        {
            public override object Read(BinaryReader reader, FieldInfo field)
            {
                return ReaderHelper.ReadType(field.FieldType, reader);
            }

            public override long GetSize(FieldInfo field, object obj)
            {
                return ReadableData.GetObjectSize(obj.GetType(), obj);
            }
        }

        public class Conditional : ConditionalType
        {
            public string Expression;
            public string[] Variables;

            public Conditional(string expression, string[] variables)
            {
                Expression = expression;
                Variables = variables;
            }

            public override bool ShouldDo(FieldInfo[] fields, object owner)
            {
                Interpreter interpreter = new Interpreter().SetVariable("helper", new ExpressionHelper());

                foreach (string variable in Variables)
                {
                    FieldInfo variableField = fields.First(x => x.Name == variable);
                    interpreter.SetVariable(variable, variableField.GetValue(owner));
                }

                return interpreter.Eval<bool>(Expression);
            }
        }

        public class FixedSizeArrayAttribute : ReadableType
        {
            protected Type Type;
            protected uint Count;

            public FixedSizeArrayAttribute(Type type, uint count)
            {
                Type = type;
                Count = count;
            }

            public override object Read(BinaryReader reader, FieldInfo field)
            {
                Array array = Array.CreateInstance(Type, Count);
                for (int i = 0; i < Count; i++)
                {
                    array.SetValue(ReaderHelper.ReadType(Type, reader), i);
                }
                return array;
            }

            public override long GetSize(FieldInfo field, object obj)
            {
                object[] array = ((IEnumerable)obj).Cast<object>().ToArray();
                long size = 0;

                foreach (object o in array)
                {
                    size += ReadableData.GetObjectSize(Type, o);
                }

                return size;
            }
        }

        public class DynamicSizeArrayAttribute : ReadableType
        {
            public Type CountType;
            public Type Type;

            public DynamicSizeArrayAttribute(Type countType, Type type)
            {
                CountType = countType;
                Type = type;
            }

            public override object Read(BinaryReader reader, FieldInfo field)
            {
                long count = Convert.ToInt64(ReaderHelper.ReadType(CountType, reader));

                Array array = Array.CreateInstance(Type, count);
                for (int i = 0; i < count; i++)
                {
                    array.SetValue(ReaderHelper.ReadType(Type, reader), i);
                }
                return array;
            }

            public override long GetSize(FieldInfo field, object obj)
            {
                object[] array = ((IEnumerable)obj).Cast<object>().ToArray();
                long size = ReadableData.GetObjectSize(CountType, 0);

                foreach (object o in array)
                {
                    size += ReadableData.GetObjectSize(Type, o);
                }

                return size;
            }

            public override long GetNoDataStartSize(FieldInfo field, object obj)
            {
                return ReadableData.GetObjectSize(CountType, 0);
            }
        }

        public class NullPaddedStringAttribute : ReadableType
        {
            public Encoding EncodingType;
            public int? TotalWidth;
            public char PadChar;

            public NullPaddedStringAttribute(int totalWidth, char padChar)
            {
                EncodingType = Encoding.UTF8;
                TotalWidth = totalWidth;
                PadChar = padChar;
            }


            public NullPaddedStringAttribute(object encoding = null)
            {
                EncodingType = (Encoding)encoding ?? Encoding.UTF8;
            }

            public override object Read(BinaryReader reader, FieldInfo field)
            {
                List<byte> bytes = new List<byte>();
                byte b;
                while ((b = reader.ReadByte()) != 0)
                    bytes.Add(b);
                return EncodingType.GetString(bytes.ToArray());
            }

            public override long GetSize(FieldInfo field, object obj)
            {
                return EncodingType.GetByteCount((string)obj) + 1;
            }
        }

        public enum ZstdBufferSize : uint
        {
            None = 0,
            StreamEnd = 1
        }

        public class ZstdBuffer : ReadableType
        {
            public ZstdBufferSize Size;
            public long CompressedSize;

            public ZstdBuffer(ZstdBufferSize size)
            {
                Size = size;
            }

            public override object Read(BinaryReader reader, FieldInfo field)
            {
                if (Size == ZstdBufferSize.StreamEnd)
                {
                    byte[] compressedBuffer =
                        reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                    CompressedSize = compressedBuffer.Length;
                    // return Decompress(compressedBuffer);
                    return compressedBuffer;
                }
                return null;
            }

            public static byte[] Decompress(byte[] compressedBuffer)
            {
                uint compressedMagic = BitConverter.ToUInt32(compressedBuffer, 0);
                Debug.Assert(compressedMagic == 0xFD2FB528);

                byte[] decompressedBuffer = new byte[1024 * 1024]; // 1MB should be enough for anyone!
                int length;
                using (Decompressor dec = new Decompressor())
                {
                    length = dec.Unwrap(compressedBuffer, decompressedBuffer, 0);
                }
                byte[] shrunkBuffer = new byte[length];
                Array.Copy(decompressedBuffer, 0, shrunkBuffer, 0, length);
                return shrunkBuffer;
            }

            public static byte[] Compress(byte[] decompressedBuffer)
            {
                byte[] compressedBuffer;
                using (CompressionOptions options = new CompressionOptions(null, compressionLevel: 4))
                using (Compressor compressor = new Compressor(options))
                {
                    compressedBuffer = compressor.Wrap(decompressedBuffer);
                }

                uint compressedMagic = BitConverter.ToUInt32(compressedBuffer, 0);
                Debug.Assert(compressedMagic == 0xFD2FB528);
                return compressedBuffer;
            }

            public override void Write(BinaryWriter writer, FieldInfo field, object obj)
            {
                if (Size == ZstdBufferSize.StreamEnd)
                {
                    // writer.Write(Compress((byte[])obj));
                    writer.Write((byte[])obj);
                }
            }

            public override long GetSize(FieldInfo field, object obj)
            {
                return CompressedSize;
            }
        }
    }
}
