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
                    if (!System.Diagnostics.Debugger.IsAttached) {
                        try {
                            ConvertAnimation(refpose, file);
                        } catch (Exception ex) {
                            Console.Error.WriteLine(ex.ToString());
                        }
                    } else {
                        ConvertAnimation(refpose, file);
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
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalSeparator = ".";
            StreamReader refpose_reader = new StreamReader(refpose);
            int refpose_bonecount = Convert.ToInt32(refpose_reader.ReadLine());
            Dictionary<int, int> refpose_parentmap = new Dictionary<int, int>();
            Dictionary<int, int> refpose_indexmap = new Dictionary<int, int>();
            int[] refpose_bonearray = new int[refpose_bonecount];
            int[] refpose_hierarchy = new int[refpose_bonecount];
            refpose_reader.ReadLine();
            refpose_reader.ReadLine();
            for (int index = 0; index < refpose_bonecount; ++index) {
                string[] array = refpose_reader.ReadLine().Replace("\"", string.Empty).Split(' ');
                refpose_bonearray[index] = Convert.ToInt32(array[1].Split('_').Last(), 16);
                refpose_hierarchy[index] = Convert.ToInt32(array[2]);
            }

            for (int index = 0; index < refpose_bonecount; ++index) {
                if (refpose_hierarchy[index] != -1)
                    refpose_parentmap.Add(refpose_bonearray[index], refpose_bonearray[refpose_hierarchy[index]]);
                else
                    refpose_parentmap.Add(refpose_bonearray[index], -1);
                refpose_indexmap.Add(refpose_bonearray[index], index);
            }
            refpose_reader.ReadLine();
            refpose_reader.ReadLine();
            refpose_reader.ReadLine();
            Vector3D[] refpose_position = new Vector3D[refpose_bonecount];
            Vector3D[] refpose_rotation = new Vector3D[refpose_bonecount];
            Vector3D[] refpose_scale = new Vector3D[refpose_bonecount];
            for (int index = 0; index < refpose_bonecount; ++index) {
                refpose_position[index] = new Vector3D();
                refpose_rotation[index] = new Vector3D();
                refpose_scale[index] = new Vector3D(1, 1, 1);
                string[] array = refpose_reader.ReadLine().Replace("\"", string.Empty).Split(' ');
                refpose_position[index].X = Convert.ToSingle(array[2], format);
                refpose_position[index].Y = Convert.ToSingle(array[3], format);
                refpose_position[index].Z = Convert.ToSingle(array[4], format);
                refpose_rotation[index].X = Convert.ToSingle(array[6], format);
                refpose_rotation[index].Y = Convert.ToSingle(array[7], format);
                refpose_rotation[index].Z = Convert.ToSingle(array[8], format);
            }
            refpose_reader.Close();
            FileStream inputStream = new FileStream(input, FileMode.Open);
            BinaryReader input_reader = new BinaryReader(inputStream);
            input_reader.ReadInt32();
            float duration = input_reader.ReadSingle();
            float fps = input_reader.ReadSingle();
            ushort bone_count = input_reader.ReadUInt16();
            input_reader.ReadUInt16();
            int frame_count = (int)(fps * (double)duration) + 1;
            inputStream.Seek(24L, SeekOrigin.Current);
            long offset_bone_list = input_reader.ReadInt64();
            long offset_info_table = input_reader.ReadInt64();
            inputStream.Seek(24L, SeekOrigin.Current);
            StreamWriter output_writer = new StreamWriter(output);
            output_writer.WriteLine("version 1");
            output_writer.WriteLine("nodes");
            int[] bone_list = new int[bone_count];
            inputStream.Seek(offset_bone_list, SeekOrigin.Begin);
            Dictionary<int, int> bone_translation_map = new Dictionary<int, int>();
            int bone_id;
            for (int index = 0; index < bone_count; ++index) {
                bone_id = input_reader.ReadInt32();
                bone_list[index] = bone_id;
                bone_translation_map.Add(bone_id, index);
            }
            for (int index = 0; index < bone_count; ++index) {
                bone_id = bone_list[index];
                int num3 = -1;
                if (refpose_parentmap.ContainsKey(bone_id)) {
                    int key2 = refpose_parentmap[bone_id];
                    num3 = !bone_translation_map.ContainsKey(key2) ? -1 : bone_translation_map[key2];
                }
                output_writer.WriteLine(index.ToString() + " \"bone_" + bone_id.ToString("X4") + "\" " + num3);
            }
            int last_bone_index = bone_count;
            Dictionary<int, int> secondary_bone_translation_map = new Dictionary<int, int>();
            for (int index = 0; index < refpose_bonecount; ++index) {
                if (!bone_translation_map.ContainsKey(refpose_bonearray[index])) {
                    secondary_bone_translation_map.Add(refpose_bonearray[index], last_bone_index);
                }
            }

            for (int index = 0; index < refpose_bonecount; ++index) {
                if (!bone_translation_map.ContainsKey(refpose_bonearray[index])) {
                    int key2 = refpose_parentmap[refpose_bonearray[index]];
                    int num3 = !bone_translation_map.ContainsKey(key2) ? (!secondary_bone_translation_map.ContainsKey(key2) ? -1 : secondary_bone_translation_map[key2]) : bone_translation_map[key2];
                    output_writer.WriteLine(last_bone_index.ToString() + " \"bone_" + refpose_bonearray[index].ToString("X4") + "\" " + num3);
                    ++last_bone_index;
                }
            }
            output_writer.WriteLine("end");
            float[,] x_array = new float[last_bone_index, frame_count];
            float[,] y_array = new float[last_bone_index, frame_count];
            float[,] z_array = new float[last_bone_index, frame_count];
            float[,] sx_array = new float[last_bone_index, frame_count];
            float[,] sy_array = new float[last_bone_index, frame_count];
            float[,] sz_array = new float[last_bone_index, frame_count];
            float[,] rx_array = new float[last_bone_index, frame_count];
            float[,] ry_array = new float[last_bone_index, frame_count];
            float[,] rz_array = new float[last_bone_index, frame_count];
            float[,] rw_array = new float[last_bone_index, frame_count];
            bool[,] has_rotation_frame = new bool[last_bone_index, frame_count];
            bool[,] has_position_frame = new bool[last_bone_index, frame_count];
            bool[,] has_scale_frame = new bool[last_bone_index, frame_count];
            output_writer.WriteLine("skeleton");
            output_writer.WriteLine("time 0");
            for (int index = 0; index < bone_count; ++index) {
                if (refpose_indexmap.ContainsKey(bone_list[index])) {
                    bone_id = refpose_indexmap[bone_list[index]];
                    output_writer.Write(index);
                    output_writer.Write(" " + refpose_position[bone_id].X.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_position[bone_id].Y.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_position[bone_id].Z.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[bone_id].X.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[bone_id].Y.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[bone_id].Z.ToString("0.000000", format));
                    output_writer.WriteLine();
                } else {
                    output_writer.WriteLine(index.ToString() + " 0 0 0 0 0 0");
                }
            }
            int num4 = bone_count;
            for (int index = 0; index < refpose_bonecount; ++index) {
                if (!bone_translation_map.ContainsKey(refpose_bonearray[index])) {
                    output_writer.Write(num4++);
                    output_writer.Write(" " + refpose_position[index].X.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_position[index].Y.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_position[index].Z.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[index].X.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[index].Y.ToString("0.000000", format));
                    output_writer.Write(" " + refpose_rotation[index].Z.ToString("0.000000", format));
                    output_writer.WriteLine();
                }
            }
            Quaternion3D q = new Quaternion3D();
            Vector3D vector3D = new Vector3D();
            inputStream.Seek(offset_info_table, SeekOrigin.Begin);
            for (int index1 = 0; index1 < bone_count; ++index1) {
                long position = inputStream.Position;
                int scale_count = input_reader.ReadInt16();
                int position_count = input_reader.ReadInt16();
                int rotation_count = input_reader.ReadInt16();
                int flags = input_reader.ReadInt16();
                long scale_indices_offset = input_reader.ReadInt32() * 4 + position;
                long position_indices_offset = input_reader.ReadInt32() * 4 + position;
                long rotation_indices_offset = input_reader.ReadInt32() * 4 + position;
                long scale_data_offset = input_reader.ReadInt32() * 4 + position;
                long position_data_offset = input_reader.ReadInt32() * 4 + position;
                long rotation_data_offset = input_reader.ReadInt32() * 4 + position;
                int[] scale_indices = new int[scale_count];
                inputStream.Seek(scale_indices_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < scale_count; ++index2)
                    scale_indices[index2] = frame_count > byte.MaxValue ? input_reader.ReadInt16() : input_reader.ReadByte();
                inputStream.Seek(scale_data_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < scale_count; ++index2) {
                    int index3 = scale_indices[index2];
                    has_scale_frame[index1, index3] = true;
                    float x = input_reader.ReadUInt16() / 1024.0f;
                    sx_array[index1, index3] = x;
                    float y = input_reader.ReadUInt16() / 1024.0f;
                    sy_array[index1, index3] = y;
                    float z = input_reader.ReadUInt16() / 1024.0f;
                    sz_array[index1, index3] = z;
                }
                int[] position_indices = new int[position_count];
                inputStream.Seek(position_indices_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < position_count; ++index2)
                    position_indices[index2] = frame_count > byte.MaxValue ? input_reader.ReadInt16() : input_reader.ReadByte();
                inputStream.Seek(position_data_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < position_count; ++index2) {
                    int index3 = position_indices[index2];
                    has_position_frame[index1, index3] = true;
                    float x = input_reader.ReadSingle();
                    x_array[index1, index3] = x;
                    float y = input_reader.ReadSingle();
                    y_array[index1, index3] = y;
                    float z = input_reader.ReadSingle();
                    z_array[index1, index3] = z;
                }
                int[] rotation_indices = new int[rotation_count];
                inputStream.Seek(rotation_indices_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < rotation_count; ++index2)
                    rotation_indices[index2] = frame_count > byte.MaxValue ? input_reader.ReadInt16() : input_reader.ReadByte();
                inputStream.Seek(rotation_data_offset, SeekOrigin.Begin);
                for (int index2 = 0; index2 < rotation_count; ++index2) {
                    ushort rot_a = input_reader.ReadUInt16();
                    ushort rot_b = input_reader.ReadUInt16();
                    ushort rot_c = input_reader.ReadUInt16();
                    Vec4d rot = Animation.UnpackRotation(rot_a, rot_b, rot_c);
                    int index3 = rotation_indices[index2];
                    has_rotation_frame[index1, index3] = true;
                    rx_array[index1, index3] = (float) rot.x;
                    ry_array[index1, index3] = (float) rot.y;
                    rz_array[index1, index3] = (float) rot.z;
                    rw_array[index1, index3] = (float) rot.w;
                }
                inputStream.Seek(position + 32L, SeekOrigin.Begin);
            }
            for (int index1 = 0; index1 < frame_count; ++index1) {
                output_writer.WriteLine($"time {index1 + 1}");
                for (int index2 = 0; index2 < last_bone_index; ++index2) {
                    if (has_position_frame[index2, index1] || has_rotation_frame[index2, index1] || has_scale_frame[index2, index1]) {
                        if (!has_position_frame[index2, index1]) {
                            int index3 = index1;
                            int index4 = index1;
                            while (!has_position_frame[index2, index3])
                                --index3;
                            while (index4 < frame_count && !has_position_frame[index2, index4])
                                ++index4;
                            if (index4 == frame_count) {
                                x_array[index2, index1] = x_array[index2, index3];
                                y_array[index2, index1] = y_array[index2, index3];
                                z_array[index2, index1] = z_array[index2, index3];
                            } else {
                                float num3 = (index1 - index3) / (float)(index4 - index3);
                                x_array[index2, index1] = (x_array[index2, index4] - x_array[index2, index3]) * num3 + x_array[index2, index3];
                                y_array[index2, index1] = (y_array[index2, index4] - y_array[index2, index3]) * num3 + y_array[index2, index3];
                                z_array[index2, index1] = (z_array[index2, index4] - z_array[index2, index3]) * num3 + z_array[index2, index3];
                            }
                        }
                        if (!has_scale_frame[index2, index1]) {
                            int index3 = index1;
                            int index4 = index1;
                            while (!has_scale_frame[index2, index3])
                                --index3;
                            while (index4 < frame_count && !has_scale_frame[index2, index4])
                                ++index4;
                            if (index4 == frame_count) {
                                sx_array[index2, index1] = sx_array[index2, index3];
                                sy_array[index2, index1] = sy_array[index2, index3];
                                sz_array[index2, index1] = sz_array[index2, index3];
                            } else {
                                float num3 = (index1 - index3) / (float)(index4 - index3);
                                sx_array[index2, index1] = (sx_array[index2, index4] - sx_array[index2, index3]) * num3 + sx_array[index2, index3];
                                sy_array[index2, index1] = (sy_array[index2, index4] - sy_array[index2, index3]) * num3 + sy_array[index2, index3];
                                sz_array[index2, index1] = (sz_array[index2, index4] - sz_array[index2, index3]) * num3 + sz_array[index2, index3];
                            }
                        }
                        if (!has_rotation_frame[index2, index1]) {
                            int index3 = index1;
                            int index4 = index1;
                            while (!has_rotation_frame[index2, index3])
                                --index3;
                            while (index4 < frame_count && !has_rotation_frame[index2, index4])
                                ++index4;
                            if (index4 == frame_count) {
                                rx_array[index2, index1] = rx_array[index2, index3];
                                ry_array[index2, index1] = ry_array[index2, index3];
                                rz_array[index2, index1] = rz_array[index2, index3];
                                rw_array[index2, index1] = rw_array[index2, index3];
                            } else {
                                double num3 = rx_array[index2, index4] * (double)rx_array[index2, index3] + ry_array[index2, index4] * (double)ry_array[index2, index3] + rz_array[index2, index4] * (double)rz_array[index2, index3] + rw_array[index2, index4] * (double)rw_array[index2, index3];
                                float num9 = (index1 - index3) / (float)(index4 - index3);
                                if (num3 < 0.0) {
                                    rx_array[index2, index1] = (-rx_array[index2, index4] - rx_array[index2, index3]) * num9 + rx_array[index2, index3];
                                    ry_array[index2, index1] = (-ry_array[index2, index4] - ry_array[index2, index3]) * num9 + ry_array[index2, index3];
                                    rz_array[index2, index1] = (-rz_array[index2, index4] - rz_array[index2, index3]) * num9 + rz_array[index2, index3];
                                    rw_array[index2, index1] = (-rw_array[index2, index4] - rw_array[index2, index3]) * num9 + rw_array[index2, index3];
                                } else {
                                    rx_array[index2, index1] = (rx_array[index2, index4] - rx_array[index2, index3]) * num9 + rx_array[index2, index3];
                                    ry_array[index2, index1] = (ry_array[index2, index4] - ry_array[index2, index3]) * num9 + ry_array[index2, index3];
                                    rz_array[index2, index1] = (rz_array[index2, index4] - rz_array[index2, index3]) * num9 + rz_array[index2, index3];
                                    rw_array[index2, index1] = (rw_array[index2, index4] - rw_array[index2, index3]) * num9 + rw_array[index2, index3];
                                }
                            }
                        }
                        output_writer.Write(index2);
                        output_writer.Write(" " + x_array[index2, index1].ToString("0.000000", format));
                        output_writer.Write(" " + y_array[index2, index1].ToString("0.000000", format));
                        output_writer.Write(" " + z_array[index2, index1].ToString("0.000000", format));
                        q.i = rx_array[index2, index1];
                        q.j = ry_array[index2, index1];
                        q.k = rz_array[index2, index1];
                        q.real = rw_array[index2, index1];
                        Vector3D eulerAngles = C3D.ToEulerAngles(q);
                        output_writer.Write(" " + eulerAngles.X.ToString("0.000000", format));
                        output_writer.Write(" " + eulerAngles.Y.ToString("0.000000", format));
                        output_writer.Write(" " + eulerAngles.Z.ToString("0.000000", format));
                        output_writer.WriteLine();
                    }
                }
            }
            output_writer.WriteLine("end");
            output_writer.Close();
        }
    }
}
