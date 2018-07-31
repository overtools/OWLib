using System.Globalization;
using System.IO;
using TankLib.Chunks;
using TankLib.Math;
using Encoding = System.Text.Encoding;

namespace TankLib.ExportFormats {
    /// <summary>
    /// Reference Pose (SMD) format
    /// </summary>
    public class RefPoseSkeleton : IExportFormat {
        public string Extension => "smd";

        protected readonly teChunkedData ChunkedData;

        private static CultureInfo _culture;

        public RefPoseSkeleton(teChunkedData chunkedData) {
            ChunkedData = chunkedData;
            if (_culture != null) return;
            _culture = (CultureInfo) CultureInfo.InvariantCulture.Clone();
            _culture.NumberFormat.NumberDecimalSeparator = ".";
        }

        public void Write(Stream stream) {
            System.Threading.Thread.CurrentThread.CurrentCulture = _culture;

            teModelChunk_Skeleton skeleton = ChunkedData.GetChunk<teModelChunk_Skeleton>();

            if (skeleton == null) return;

            short[] hierarchy = (short[]) skeleton.Hierarchy.Clone();
            /*HashSet<short> weightedParNodes = new HashSet<short>();
            Dictionary<int, teModelChunk_Cloth.ClothNode> nodeMap = new Dictionary<int, teModelChunk_Cloth.ClothNode>();
            teModelChunk_Cloth cloth = ChunkedData.GetChunk<teModelChunk_Cloth>();
            if (cloth != null) {
                uint clothIndex = 0;
                foreach (teModelChunk_Cloth.ClothNode[] nodeCollection in cloth.Nodes) {
                    if (nodeCollection == null) continue;
                    int nodeIndex = 0;
                    foreach (teModelChunk_Cloth.ClothNode node in nodeCollection) {
                        int parentRaw = node.VerticalParent;
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                            cloth.NodeBones[clothIndex].ContainsKey(parentRaw)) {
                            // good code:
                            if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] =
                                    cloth.NodeBones[clothIndex][parentRaw];
                                if (cloth.NodeBones[clothIndex][parentRaw] == -1) {
                                    teModelChunk_Cloth.ClothNodeWeight weightedBone =
                                        node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    weightedParNodes.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
                            }
                        } else {
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                                // if on main skele
                                // good code:
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = -1;
                                    teModelChunk_Cloth.ClothNodeWeight weightedBone =
                                        node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    weightedParNodes.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
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
            }*/

            using (StreamWriter writer = new StreamWriter(stream, Encoding.Default, 512)) {
                writer.WriteLine("{0}", skeleton.Header.BonesAbs);
                writer.WriteLine("version 1");
                writer.WriteLine("nodes");
                for (int i = 0; i < skeleton.Header.BonesAbs; ++i) {
                    writer.WriteLine("{0} \"bone_{1:X4}\" {2}", i, skeleton.IDs[i], hierarchy[i]);
                }

                writer.WriteLine("end");
                writer.WriteLine("skeleton");
                writer.WriteLine("time 0");
                for (int i = 0; i < skeleton.Header.BonesAbs; ++i) {
                    teMtx43 bone = skeleton.Matrices34Inverted[i];
                    teQuat quat = new teQuat(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);

                    teVec3 rot = quat.ToEulerAngles();
                    teVec3 scale = new teVec3(bone[1, 0], bone[1, 1], bone[1, 2]);
                    teVec3 pos = new teVec3(bone[2, 0], bone[2, 1], bone[2, 2]);
                    /*if (nodeMap.ContainsKey(i)) {
                        teModelChunk_Cloth.ClothNode thisNode = nodeMap[i];
                        if (weightedParNodes.Contains((short)i)) {
                            teVec3 pos2 = GetGlobalPos(skeleton.Matrices34Inverted, hierarchy[i], hierarchy);
                            pos.X = thisNode.X - pos2.X;
                            pos.Y = thisNode.Y - pos2.Y;
                            pos.Z = thisNode.Z - pos2.Z;
                        } else if (nodeMap.ContainsKey(hierarchy[i])) {
                            teModelChunk_Cloth.ClothNode parentNode = nodeMap[hierarchy[i]];
                            pos.X = thisNode.X - parentNode.X;
                            pos.Y = thisNode.Y - parentNode.Y;
                            pos.Z = thisNode.Z - parentNode.Z;
                        } else {
                            pos.X = thisNode.X;
                            pos.Y = thisNode.Y;
                            pos.Z = thisNode.Z;
                            
                        }
                    }*/
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}  {1:0.000000} {2:0.000000} {3:0.000000}  {4:0.000000} {5:0.000000} {6:0.000000}  {7:0.000000} {8:0.000000} {9:0.000000}",
                        i, pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z, scale.X, scale.Y, scale.Z));
                }
            }
        }
    }
}