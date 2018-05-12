using System.IO;
using System.Linq;
using TankLib.Chunks;
using TankLib.Math;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWMDL format
    /// </summary>
    public class OverwatchModel : IExportFormat {
        public string Extension => "owmdl";

        private readonly teChunkedData _data;

        public string ModelLookFileName;
        public string Name;
        
        public OverwatchModel(teChunkedData chunkedData) {
            _data = chunkedData;
        }
        
        public void Write(Stream stream) {
            teModelChunk_RenderMesh renderMesh = _data.GetChunk<teModelChunk_RenderMesh>();
            teModelChunk_Model model = _data.GetChunk<teModelChunk_Model>();
            teModelChunk_Skeleton skeleton = _data.GetChunk<teModelChunk_Skeleton>();
            teModelChunk_Hardpoint hardpoints = _data.GetChunk<teModelChunk_Hardpoint>();
            teModelChunk_Cloth cloth = _data.GetChunk<teModelChunk_Cloth>();
            teModelChunk_STU stu = _data.GetChunk<teModelChunk_STU>();
            
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((ushort)1);
                writer.Write((ushort)1);  // todo: not full support
                if (ModelLookFileName == null) {   // mat ref
                    writer.Write((byte)0);
                } else {
                    writer.Write(ModelLookFileName);
                }
                if (Name == null) {   // model name
                    writer.Write((byte)0);
                } else {
                    writer.Write(Name);
                }

                if (skeleton != null) {
                    writer.Write(skeleton.Header.BonesAbs);
                } else {
                    writer.Write((ushort)0);
                }
                
                // todo: specify lod
                teModelChunk_RenderMesh.Submesh[] submeshes = renderMesh.Submeshes.Where(x => x.Descriptor.LOD == 1 || x.Descriptor.LOD == -1).ToArray(); // .Descriptor.LOD == 0
                writer.Write((uint)submeshes.Length);
                writer.Write(0);  // hardpoints
                
                if (skeleton != null) {
                    for (int j = 0; j < skeleton.Header.BonesAbs; ++j) {
                        writer.Write(GetBoneName(skeleton.IDs[j]));
                        short parent = skeleton.Hierarchy[j];
                        if (parent == -1) {
                            parent = (short)j;
                        }
                        writer.Write(parent);
                        
                        teMtx43 boneMat = skeleton.Matrices34[j];
                        teQuat rotation = new teQuat(boneMat[0, 0], boneMat[0, 1], boneMat[0, 2], boneMat[0, 3]);
                        teVec3 scale = new teVec3(boneMat[1, 0], boneMat[1, 1], boneMat[1, 2]);
                        teVec3 translation = new teVec3(boneMat[2, 0], boneMat[2, 1], boneMat[2, 2]);
                        
                        writer.Write(translation);
                        writer.Write(scale);
                        writer.Write(rotation);
                    }
                }

                int i = 0;
                foreach (teModelChunk_RenderMesh.Submesh submesh in submeshes) {
                    writer.Write($"Submesh_{i}.{model.Materials[submesh.Descriptor.Material]:X16}");
                    writer.Write(model.Materials[submesh.Descriptor.Material]);
                    writer.Write(submesh.UVCount);
                    
                    writer.Write(submesh.Vertices.Length);
                    writer.Write(submesh.Indices.Length/3);

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Vertices[j]);
                        writer.Write(-submesh.Normals[j].X);
                        writer.Write(-submesh.Normals[j].Y);
                        writer.Write(-submesh.Normals[j].Z);

                        foreach (teVec2 uv in submesh.UV[j]) {
                            writer.Write(uv);
                        }
                        
                        if (skeleton != null && submesh.BoneIndices[j] != null) {
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

                    for (int j = 0; j < submesh.Descriptor.IndicesToDraw; j+=3) {
                        writer.Write((byte)3);
                        writer.Write((int)submesh.Indices[j]);
                        writer.Write((int)submesh.Indices[j+1]);
                        writer.Write((int)submesh.Indices[j+2]);
                    }
                    
                    i++;
                }
            }
        }

        public static string GetBoneName(uint id) {
            return IdToString("bone", id);
        }
        
        public static string IdToString(string prefix, uint id) {  // hmm
            return $"{prefix}_{id:X4}";
        }
    }
}