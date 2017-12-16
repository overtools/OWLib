using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;
using System.Globalization;
using System.Linq;
using APPLIB;
using OWLib.Types.Chunk.LDOM;

namespace OWLib.Writer {
    public class RefPoseWriter : IDataWriter {
        public string Format => ".smd";
        public char[] Identifier => new char[1] { 'R' };
        public string Name => "Reference Pose";
        private CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        public WriterSupport SupportLevel => WriterSupport.BONE | WriterSupport.POSE | WriterSupport.MODEL | WriterSupport.REFPOSE;

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }
        
        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            culture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            IChunk chunk = model.FindNextChunk("lksm").Value;
            if (chunk == null) {
                return false;
            }
            lksm skeleton = (lksm)chunk;
            short[] hierarchy = (short[])skeleton.Hierarchy.Clone();
            HashSet<short> weightedParNodes = new HashSet<short>();
            Dictionary<int, HTLC.ClothNode> nodeMap = new Dictionary<int, HTLC.ClothNode>();
            HTLC cloth = model.FindNextChunk("HTLC").Value as HTLC;
            if (cloth != null) {
                uint clothIndex = 0;
                foreach (HTLC.ClothNode[] nodeCollection in cloth.Nodes) {
                    if (nodeCollection == null) continue;
                    int nodeIndex = 0;
                    foreach (HTLC.ClothNode node in nodeCollection) {
                        int parentRaw = node.VerticalParent;
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                            cloth.NodeBones[clothIndex].ContainsKey(parentRaw)) {
                            // good code:
                            if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = cloth.NodeBones[clothIndex][parentRaw];
                                if (cloth.NodeBones[clothIndex][parentRaw] == -1) {
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    weightedParNodes.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
                            }
                            // else: on subskele
                            // todo: add subskelebones?

                        } else {
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {  // if on main skele
                                // good code:
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = -1;
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    weightedParNodes.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
                                // else: on subskele
                                // todo: add subskelebones?
                            }
                        }
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                            // good code:
                            nodeMap[cloth.NodeBones[clothIndex][nodeIndex]] = node;
                        }
                        nodeIndex++;
                    }
                    clothIndex++;
                }
            }
            using (StreamWriter writer = new StreamWriter(output)) {
                writer.WriteLine("{0}", skeleton.Data.bonesAbs);
                writer.WriteLine("version 1");
                writer.WriteLine("nodes");
                for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                    writer.WriteLine("{0} \"bone_{1:X4}\" {2}", i, skeleton.IDs[i], hierarchy[i]);
                }
                writer.WriteLine("end");
                writer.WriteLine("skeleton");
                writer.WriteLine("time 0");
                for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                    Matrix3x4 bone = skeleton.Matrices34Inverted[i];
                    Quaternion3D quat = new Quaternion3D(bone[0, 3], bone[0, 0], bone[0, 1], bone[0, 2]);
                    Vector3D rot = C3D.ToEulerAngles(quat);
                    Vector3 scale = new Vector3(bone[1, 0], bone[1, 1], bone[1, 2]);
                    Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
                    if (nodeMap.ContainsKey(i)) {
                        HTLC.ClothNode thisNode = nodeMap[i];
                        if (weightedParNodes.Contains((short)i)) {
                            Vector3 pos2 = GetGlobalPos(skeleton.Matrices34Inverted, hierarchy[i], hierarchy);
                            pos.X = thisNode.X - pos2.X;
                            pos.Y = thisNode.Y - pos2.Y;
                            pos.Z = thisNode.Z - pos2.Z;
                        } else if (nodeMap.ContainsKey(hierarchy[i])) {
                            HTLC.ClothNode parentNode = nodeMap[hierarchy[i]];
                            pos.X = thisNode.X - parentNode.X;
                            pos.Y = thisNode.Y - parentNode.Y;
                            pos.Z = thisNode.Z - parentNode.Z;
                        } else {
                            pos.X = thisNode.X;
                            pos.Y = thisNode.Y;
                            pos.Z = thisNode.Z;
                            
                        }
                    }
                    if (rot.X == -3.14159274f && rot.Y == 0 && rot.Z == 0) {
                        rot = new Vector3D(0, 3.14159274f, 3.14159274f); // effectively the same but you know, eulers.
                    }
                    writer.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0}  {1:0.000000} {2:0.000000} {3:0.000000}  {4:0.000000} {5:0.000000} {6:0.000000}  {7:0.000000} {8:0.000000} {9:0.000000}", i, pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z, scale.X, scale.Y, scale.Z));
                }
            }
            return true;
        }
        
        public static Quaternion GetGlobalRot(lksm skeleton, short s, IReadOnlyList<short> hierarchy) {
            if (skeleton.Matrices34Inverted.Length <= s || s == -1) return new Quaternion();
            Matrix3x4 bone = skeleton.Matrices34Inverted[s];
            Quaternion quat = new Quaternion(bone[0, 3], bone[0, 0], bone[0, 1], bone[0, 2]);
            Quaternion parent = new Quaternion();
            if (hierarchy.Count > s) parent = GetGlobalRot(skeleton, hierarchy[s], hierarchy);
            return quat + parent;
        }

        public static Vector3 GetPos(lksm skeleton, int index) {
            Matrix3x4 bone = skeleton.Matrices34[index];
            Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
            return pos;
        }
        
        public static Quaternion GetRotTest(lksm skeleton, int index) {
            Matrix3x4 bone = skeleton.Matrices34[index];
            Quaternion quat = new Quaternion(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
            return quat;
        }

        public static Vector3 GetGlobalPos(IReadOnlyList<Matrix3x4> skeletonMatrices34Inverted, short s, IReadOnlyList<short> hierarchy) {
             if (skeletonMatrices34Inverted.Count <= s || s == -1) return new Vector3();
             Matrix3x4 bone = skeletonMatrices34Inverted[s];
             Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
             Vector3 parent = new Vector3();
             if (hierarchy.Count > s) parent = GetGlobalPos(skeletonMatrices34Inverted, hierarchy[s], hierarchy);
             return pos + parent;
        }

        public bool WriteCloth(Chunked model, Stream output, HTLC cloth, uint index) {  // todo:
            culture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            IChunk chunk = model.FindNextChunk("lksm").Value;
            if (chunk == null) {
                return false;
            }
            lksm skeleton = (lksm)chunk;

            if (cloth.Nodes[index] == null) return false;
            List<HTLC.ClothNode> nodes = cloth.Nodes[index].ToList();
            using (StreamWriter writer = new StreamWriter(output)) {
                writer.WriteLine("{0}", nodes.Count);
                writer.WriteLine("version 1");
                writer.WriteLine("nodes");
                Dictionary<HTLC.ClothNode, uint> boneMap = new Dictionary<HTLC.ClothNode, uint>();
                uint nodeIndex = 0;
                foreach (HTLC.ClothNode clothNode in nodes) {
                    uint bone = nodeIndex;
                    bool isNonskele = false;
                    if (cloth.NodeBones[index].ContainsKey((short)bone) && cloth.NodeBones[index][(int) bone] != -1) {
                        bone = skeleton.IDs[cloth.NodeBones[index][(int) bone]];
                        isNonskele = true;
                    }
                    boneMap[clothNode] = nodeIndex;
                    writer.WriteLine($"{nodeIndex} \"bone_{(isNonskele ? "" : "noskele_")}{bone:X}\" {clothNode.VerticalParent}");
                    nodeIndex++;
                }
                writer.WriteLine("end");
                writer.WriteLine("skeleton");
                writer.WriteLine("time 0");
                foreach (HTLC.ClothNode node in nodes) {
                    if (!boneMap.ContainsKey(node)) continue;
                    short parent = node.VerticalParent;
                    float parentX = parent != -1 ? nodes[parent].X : 0f;
                    float parentY = parent != -1 ? nodes[parent].Y : 0f;
                    float parentZ = parent != -1 ? nodes[parent].Z : 0f;
                    writer.Write($"{boneMap[node]}");
                    writer.Write($" {node.X-parentX:0.000000}");
                    writer.Write($" {node.Y-parentY:0.000000}");
                    writer.Write($" {node.Z-parentZ:0.000000}");
                    writer.WriteLine(" 0 0 0");
                    }
            }
            
            return true;
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
    }
}
