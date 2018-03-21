using System.IO;
using System.Linq;
using TankLib.Chunks;
using TankLib.Math;

namespace TankLib.ExportFormats {
    public class OverwatchModel : IExportFormat {
        private readonly teChunkedData _data;
        
        public OverwatchModel(teChunkedData chunkedData) {
            _data = chunkedData;
        }

        public string Extension => "owmdl";
        
        public void Write(Stream stream) {
            teModelChunk_RenderMesh renderMesh = _data.GetChunk<teModelChunk_RenderMesh>();
            teModelChunk_Model model = _data.GetChunk<teModelChunk_Model>();
            teModelChunk_Skeleton skeleton = _data.GetChunk<teModelChunk_Skeleton>();
            teModelChunk_Hardpoint hardpoints = _data.GetChunk<teModelChunk_Hardpoint>();
            teModelChunk_Cloth cloth = _data.GetChunk<teModelChunk_Cloth>();
            
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((ushort)1);
                writer.Write((ushort)1);  // todo: not full support
                writer.Write((byte)0);
                writer.Write((byte)0);

                if (skeleton != null) {
                    writer.Write(skeleton.Header.BonesAbs);
                } else {
                    writer.Write((ushort)0);
                }
                
                teModelChunk_RenderMesh.Submesh[] submeshes = renderMesh.Submeshes.Where(x => x != null).ToArray(); // .Descriptor.LOD == 0
                writer.Write((uint)submeshes.Length);
                writer.Write(0);  // hardpoints
                
                if (skeleton != null) {
                    for (int j = 0; j < skeleton.Header.BonesAbs; ++j) {
                        writer.Write(IdToString("bone", skeleton.IDs[j]));
                        short parent = skeleton.Hierarchy[j];
                        if (parent == -1) {
                            parent = (short)j;
                        }
                        writer.Write(parent);
                        
                        teMtx43A bone = skeleton.Matrices34[j];
                        teQuat rot = new teQuat(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                        teVec3 scl = new teVec3(bone[1, 0], bone[1, 1], bone[1, 2]);
                        teVec3 pos = new teVec3(bone[2, 0], bone[2, 1], bone[2, 2]);
                        //if (nodeMap.ContainsKey(j)) {
                        //    HTLC.ClothNode thisNode = nodeMap[j];
                        //    pos.X = thisNode.X;
                        //    pos.Y = thisNode.Y;
                        //    pos.Z = thisNode.Z;
                        //}
                        writer.Write(pos);
                        writer.Write(scl);
                        writer.Write(rot);
                    }
                }

                int i = 0;
                foreach (teModelChunk_RenderMesh.Submesh submesh in submeshes) {
                    writer.Write($"Submesh_{i}.{model.Materials[submesh.Descriptor.Material]:X16}");
                    writer.Write(model.Materials[submesh.Descriptor.Material]);
                    writer.Write(submesh.UVCount);
                    
                    writer.Write(submesh.Vertices.Length);
                    //writer.Write(submesh.Indices.Length/3);
                    writer.Write(submesh.Faces.Length);

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Vertices[j]);
                        writer.Write(-submesh.Normals[j].X);
                        writer.Write(-submesh.Normals[j].Y);
                        writer.Write(-submesh.Normals[j].Z);

                        foreach (teVec2 uv in submesh.UV[j]) {
                            writer.Write(uv);
                        }
                        
                        if (skeleton != null) {
                            writer.Write((byte)4);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][0]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][1]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][2]]);
                            writer.Write(skeleton.Lookup[submesh.BoneIndices[j][3]]);
                            writer.Write(submesh.BoneWeights[j][0]);
                            writer.Write(submesh.BoneWeights[j][1]);
                            writer.Write(submesh.BoneWeights[j][2]);
                            writer.Write(submesh.BoneWeights[j][3]);
                        } else {
                            writer.Write((byte)0);
                        }
                    }

                    //for (int j = 0; j < submesh.Indices.Length/3; j+=3) {
                    //    writer.Write((byte)3);
                    //    writer.Write((int)submesh.Indices[j]);
                    //    writer.Write((int)submesh.Indices[j+1]);
                    //    writer.Write((int)submesh.Indices[j+2]);
                    //}
                    foreach (teModelChunk_RenderMesh.MeshFace face in submesh.Faces) {
                        writer.Write((byte)3);
                        writer.Write(face.ToExportStruct());
                    }
                    
                    i++;
                }
            }
        }
        
        public static string IdToString(string prefix, uint id) {  // hmm
            return $"{prefix}_{id:X4}";
        }
    }
}