// Decompiled with JetBrains decompiler

using APPLIB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OWLib.Types;
using OWLib;

namespace Overwatch_anim {
    public class Overwatch_anim {
        public static void Main(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: overwatch_anim.exe refpose.smd files");
                return;
            }
            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());
            Console.Out.WriteLine("Credit to id-daemon");

            string refpose = args[0];
            List<string> files = new List<string>();
            foreach (string name in args.Skip(1)) {
                if (name.Contains('*')) {
                    files.AddRange(Glob.Glob.ExpandNames(name));
                } else {
                    if (File.Exists(name)) {
                        files.Add(name);
                    }
                }
            }

            foreach (string file in files) {
                if (Path.GetExtension(file) == ".006") {
                    Console.Out.WriteLine("Converting animation {0}", file);
                    try {
                        ConvertAnimation(refpose, file);
                    } catch (Exception ex) {
                        Console.Error.WriteLine(ex.ToString());
                    }
                }
            }

            if (System.Diagnostics.Debugger.IsAttached) {
                System.Diagnostics.Debugger.Break();
            }
        }

        public static void ConvertAnimation(string refpose, string input) {
            ConvertAnimation(refpose, input, Path.GetDirectoryName(input) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(input) + ".smd");
        }

        public static void ConvertAnimation(string refpose, string input, string output) {
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";
            StreamReader streamReader = new StreamReader(refpose);
            int int32 = Convert.ToInt32(streamReader.ReadLine());
            Dictionary<int, int> dictionary1 = new Dictionary<int, int>();
            Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
            int[] numArray1 = new int[int32];
            int[] numArray2 = new int[int32];
            streamReader.ReadLine();
            streamReader.ReadLine();
            for (int index = 0; index < int32; ++index) {
                string[] strArray = streamReader.ReadLine().Split(' ');
                numArray1[index] = Convert.ToInt32(strArray[1].Substring(6, 4), 16);
                numArray2[index] = Convert.ToInt32(strArray[2]);
                if (numArray2[index] != -1)
                    dictionary1.Add(numArray1[index], numArray1[numArray2[index]]);
                else
                    dictionary1.Add(numArray1[index], -1);
                dictionary2.Add(numArray1[index], index);
            }
            streamReader.ReadLine();
            streamReader.ReadLine();
            streamReader.ReadLine();
            Vector3D[] vector3DArray1 = new Vector3D[int32];
            Vector3D[] vector3DArray2 = new Vector3D[int32];
            for (int index = 0; index < int32; ++index) {
                vector3DArray1[index] = new Vector3D();
                vector3DArray2[index] = new Vector3D();
                string[] strArray = streamReader.ReadLine().Split(' ');
                vector3DArray1[index].X = Convert.ToSingle(strArray[2], (IFormatProvider)numberFormatInfo);
                vector3DArray1[index].Y = Convert.ToSingle(strArray[3], (IFormatProvider)numberFormatInfo);
                vector3DArray1[index].Z = Convert.ToSingle(strArray[4], (IFormatProvider)numberFormatInfo);
                vector3DArray2[index].X = Convert.ToSingle(strArray[6], (IFormatProvider)numberFormatInfo);
                vector3DArray2[index].Y = Convert.ToSingle(strArray[7], (IFormatProvider)numberFormatInfo);
                vector3DArray2[index].Z = Convert.ToSingle(strArray[8], (IFormatProvider)numberFormatInfo);
            }
            streamReader.Close();
            FileStream fileStream = new FileStream(input, FileMode.Open);
            BinaryReader binaryReader = new BinaryReader((Stream)fileStream);
            binaryReader.ReadInt32();
            float num1 = binaryReader.ReadSingle();
            float num2 = binaryReader.ReadSingle();
            ushort length1 = binaryReader.ReadUInt16();
            binaryReader.ReadUInt16();
            int length2 = (int)((double)num2 * (double)num1) + 1;
            fileStream.Seek(24L, SeekOrigin.Current);
            long offset1 = binaryReader.ReadInt64();
            long offset2 = binaryReader.ReadInt64();
            fileStream.Seek(24L, SeekOrigin.Current);
            StreamWriter streamWriter = new StreamWriter(output);
            streamWriter.WriteLine("version 1");
            streamWriter.WriteLine("nodes");
            int[] numArray3 = new int[length1];
            fileStream.Seek(offset1, SeekOrigin.Begin);
            Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
            int key1;
            for (int index = 0; index < length1; ++index) {
                key1 = binaryReader.ReadInt32();
                numArray3[index] = key1;
                dictionary3.Add(key1, index);
            }
            for (int index = 0; index < length1; ++index) {
                key1 = numArray3[index];
                int num3 = -1;
                if (dictionary1.ContainsKey(key1)) {
                    int key2 = dictionary1[key1];
                    num3 = !dictionary3.ContainsKey(key2) ? -1 : dictionary3[key2];
                }
                streamWriter.WriteLine(index.ToString() + " \"bone_" + key1.ToString("X4") + "\" " + (object)num3);
            }
            int length3 = length1;
            Dictionary<int, int> dictionary4 = new Dictionary<int, int>();
            for (int index = 0; index < int32; ++index) {
                if (!dictionary3.ContainsKey(numArray1[index])) {
                    dictionary4.Add(numArray1[index], length3);
                }
            }

            for (int index = 0; index < int32; ++index) {
                if (!dictionary3.ContainsKey(numArray1[index])) {
                    int key2 = dictionary1[numArray1[index]];
                    int num3 = !dictionary3.ContainsKey(key2) ? (!dictionary4.ContainsKey(key2) ? -1 : dictionary4[key2]) : dictionary3[key2];
                    streamWriter.WriteLine(length3.ToString() + " \"bone_" + numArray1[index].ToString("X4") + "\" " + (object)num3);
                    ++length3;
                }
            }
            streamWriter.WriteLine("end");
            float[,] numArray4 = new float[length3, length2];
            float[,] numArray5 = new float[length3, length2];
            float[,] numArray6 = new float[length3, length2];
            float[,] numArray7 = new float[length3, length2];
            float[,] numArray8 = new float[length3, length2];
            float[,] numArray9 = new float[length3, length2];
            float[,] numArray10 = new float[length3, length2];
            bool[,] flagArray1 = new bool[length3, length2];
            bool[,] flagArray2 = new bool[length3, length2];
            streamWriter.WriteLine("skeleton");
            streamWriter.WriteLine("time 0");
            for (int index = 0; index < length1; ++index) {
                if (dictionary2.ContainsKey(numArray3[index])) {
                    key1 = dictionary2[numArray3[index]];
                    streamWriter.Write(index);
                    streamWriter.Write(" " + vector3DArray1[key1].X.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray1[key1].Y.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray1[key1].Z.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[key1].X.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[key1].Y.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[key1].Z.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.WriteLine();
                } else
                    streamWriter.WriteLine(index.ToString() + " 0 0 0 0 0 0");
            }
            int num4 = length1;
            for (int index = 0; index < int32; ++index) {
                if (!dictionary3.ContainsKey(numArray1[index])) {
                    streamWriter.Write(num4++);
                    streamWriter.Write(" " + vector3DArray1[index].X.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray1[index].Y.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray1[index].Z.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[index].X.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[index].Y.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.Write(" " + vector3DArray2[index].Z.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                    streamWriter.WriteLine();
                }
            }
            Quaternion3D q = new Quaternion3D();
            Vector3D vector3D = new Vector3D();
            fileStream.Seek(offset2, SeekOrigin.Begin);
            for (int index1 = 0; index1 < length1; ++index1) {
                long position = fileStream.Position;
                int num3 = (int)binaryReader.ReadInt16();
                int length4 = (int)binaryReader.ReadInt16();
                int length5 = (int)binaryReader.ReadInt16();
                int num9 = (int)binaryReader.ReadInt16();
                binaryReader.ReadInt32();
                long offset3 = (long)(binaryReader.ReadInt32() * 4) + position;
                long offset4 = (long)(binaryReader.ReadInt32() * 4) + position;
                binaryReader.ReadInt32();
                long offset5 = (long)(binaryReader.ReadInt32() * 4) + position;
                long offset6 = (long)(binaryReader.ReadInt32() * 4) + position;
                int[] numArray11 = new int[length4];
                fileStream.Seek(offset3, SeekOrigin.Begin);
                for (int index2 = 0; index2 < length4; ++index2)
                    numArray11[index2] = length2 >= (int)byte.MaxValue ? (int)binaryReader.ReadInt16() : (int)binaryReader.ReadByte();
                fileStream.Seek(offset5, SeekOrigin.Begin);
                for (int index2 = 0; index2 < length4; ++index2) {
                    int index3 = numArray11[index2];
                    flagArray2[index1, index3] = true;
                    float num10 = binaryReader.ReadSingle();
                    numArray4[index1, index3] = num10;
                    float num11 = binaryReader.ReadSingle();
                    numArray5[index1, index3] = num11;
                    float num12 = binaryReader.ReadSingle();
                    numArray6[index1, index3] = num12;
                }
                int[] numArray12 = new int[length5];
                fileStream.Seek(offset4, SeekOrigin.Begin);
                for (int index2 = 0; index2 < length5; ++index2)
                    numArray12[index2] = length2 >= (int)byte.MaxValue ? (int)binaryReader.ReadInt16() : (int)binaryReader.ReadByte();
                fileStream.Seek(offset6, SeekOrigin.Begin);
                for (int index2 = 0; index2 < length5; ++index2) {
                    ushort rot_a = binaryReader.ReadUInt16();
                    ushort rot_b = binaryReader.ReadUInt16();
                    ushort rot_c = binaryReader.ReadUInt16();
                    Vec4d rot = Animation.UnpackRotation(rot_a, rot_b, rot_c);
                    int index3 = numArray12[index2];
                    flagArray1[index1, index3] = true;
                    numArray7[index1, index3] = (float) rot.x;
                    numArray8[index1, index3] = (float) rot.y;
                    numArray9[index1, index3] = (float) rot.z;
                    numArray10[index1, index3] = (float) rot.w;
                }
                fileStream.Seek(position + 32L, SeekOrigin.Begin);
            }
            for (int index1 = 0; index1 < length2; ++index1) {
                streamWriter.WriteLine("time " + (object)(index1 + 1));
                for (int index2 = 0; index2 < length3; ++index2) {
                    if (flagArray2[index2, index1] || flagArray1[index2, index1]) {
                        if (!flagArray2[index2, index1]) {
                            int index3 = index1;
                            int index4 = index1;
                            while (!flagArray2[index2, index3])
                                --index3;
                            while (index4 < length2 && !flagArray2[index2, index4])
                                ++index4;
                            if (index4 == length2) {
                                numArray4[index2, index1] = numArray4[index2, index3];
                                numArray5[index2, index1] = numArray5[index2, index3];
                                numArray6[index2, index1] = numArray6[index2, index3];
                            } else {
                                float num3 = (float)(index1 - index3) / (float)(index4 - index3);
                                numArray4[index2, index1] = (numArray4[index2, index4] - numArray4[index2, index3]) * num3 + numArray4[index2, index3];
                                numArray5[index2, index1] = (numArray5[index2, index4] - numArray5[index2, index3]) * num3 + numArray5[index2, index3];
                                numArray6[index2, index1] = (numArray6[index2, index4] - numArray6[index2, index3]) * num3 + numArray6[index2, index3];
                            }
                        }
                        if (!flagArray1[index2, index1]) {
                            int index3 = index1;
                            int index4 = index1;
                            while (!flagArray1[index2, index3])
                                --index3;
                            while (index4 < length2 && !flagArray1[index2, index4])
                                ++index4;
                            if (index4 == length2) {
                                numArray7[index2, index1] = numArray7[index2, index3];
                                numArray8[index2, index1] = numArray8[index2, index3];
                                numArray9[index2, index1] = numArray9[index2, index3];
                                numArray10[index2, index1] = numArray10[index2, index3];
                            } else {
                                double num3 = (double)numArray7[index2, index4] * (double)numArray7[index2, index3] + (double)numArray8[index2, index4] * (double)numArray8[index2, index3] + (double)numArray9[index2, index4] * (double)numArray9[index2, index3] + (double)numArray10[index2, index4] * (double)numArray10[index2, index3];
                                float num9 = (float)(index1 - index3) / (float)(index4 - index3);
                                if (num3 < 0.0) {
                                    numArray7[index2, index1] = (-numArray7[index2, index4] - numArray7[index2, index3]) * num9 + numArray7[index2, index3];
                                    numArray8[index2, index1] = (-numArray8[index2, index4] - numArray8[index2, index3]) * num9 + numArray8[index2, index3];
                                    numArray9[index2, index1] = (-numArray9[index2, index4] - numArray9[index2, index3]) * num9 + numArray9[index2, index3];
                                    numArray10[index2, index1] = (-numArray10[index2, index4] - numArray10[index2, index3]) * num9 + numArray10[index2, index3];
                                } else {
                                    numArray7[index2, index1] = (numArray7[index2, index4] - numArray7[index2, index3]) * num9 + numArray7[index2, index3];
                                    numArray8[index2, index1] = (numArray8[index2, index4] - numArray8[index2, index3]) * num9 + numArray8[index2, index3];
                                    numArray9[index2, index1] = (numArray9[index2, index4] - numArray9[index2, index3]) * num9 + numArray9[index2, index3];
                                    numArray10[index2, index1] = (numArray10[index2, index4] - numArray10[index2, index3]) * num9 + numArray10[index2, index3];
                                }
                            }
                        }
                        streamWriter.Write(index2);
                        streamWriter.Write(" " + numArray4[index2, index1].ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        streamWriter.Write(" " + numArray5[index2, index1].ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        streamWriter.Write(" " + numArray6[index2, index1].ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        q.i = numArray7[index2, index1];
                        q.j = numArray8[index2, index1];
                        q.k = numArray9[index2, index1];
                        q.real = numArray10[index2, index1];
                        Vector3D eulerAngles = C3D.ToEulerAngles(q);
                        streamWriter.Write(" " + eulerAngles.X.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        streamWriter.Write(" " + eulerAngles.Y.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        streamWriter.Write(" " + eulerAngles.Z.ToString("0.000000", (IFormatProvider)numberFormatInfo));
                        streamWriter.WriteLine();
                    }
                }
            }
            streamWriter.WriteLine("end");
            streamWriter.Close();
        }
    }
}
