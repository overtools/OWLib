using System.IO;
using System.Linq;
using SharpDX;
using TankLib.Chunks;
using TankLib.Math;
using TankLib.STU.Types;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWMDL format
    /// </summary>
    public class OverwatchModel : IExportFormat {
        public string Extension => "owmdl";

        private readonly teChunkedData _data;

        public string ModelLookFileName;
        public string Name;
        public ulong GUID;
        public sbyte TargetLod;
        
        public OverwatchModel(teChunkedData chunkedData, ulong guid, sbyte targetLod=1) {
            _data = chunkedData;
            GUID = guid;
            TargetLod = targetLod;
        }
        
        public void Write(Stream stream) {
            teModelChunk_RenderMesh renderMesh = _data.GetChunk<teModelChunk_RenderMesh>();
            teModelChunk_Model model = _data.GetChunk<teModelChunk_Model>();
            teModelChunk_Skeleton skeleton = _data.GetChunk<teModelChunk_Skeleton>();
            //teModelChunk_Cloth cloth = _data.GetChunk<teModelChunk_Cloth>();
            teModelChunk_STU stu = _data.GetChunk<teModelChunk_STU>();
            
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((ushort)1);
                writer.Write((ushort)5);  // todo: not full support
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

                if (renderMesh == null) {
                    writer.Write(0u);
                    writer.Write(0);
                    return;
                }
                teModelChunk_RenderMesh.Submesh[] submeshes = renderMesh.Submeshes.Where(x => x.Descriptor.LOD == TargetLod || x.Descriptor.LOD == -1).ToArray();
                
                writer.Write((uint)submeshes.Length);
                
                if (stu?.StructuredData.m_hardPoints != null) {
                    writer.Write(stu.StructuredData.m_hardPoints.Length);
                } else {
                    writer.Write(0);  // hardpoints
                }
                
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

                int submeshIdx = 0;
                foreach (teModelChunk_RenderMesh.Submesh submesh in submeshes) {
                    writer.Write($"Submesh_{submeshIdx}.{model.Materials[submesh.Descriptor.Material]:X16}");
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
                    
                    submeshIdx++;
                }
                
                
                if (stu?.StructuredData.m_hardPoints != null) {
                    foreach (STUModelHardpoint hardPoint in stu.StructuredData.m_hardPoints) {
                        writer.Write(IdToString("hardpoint", teResourceGUID.Index(hardPoint.m_EDF0511C)));
                        
                        Matrix parentMat = Matrix.Identity;
                        if (hardPoint.m_FF592924 != 0 && skeleton != null) {
                            int boneIdx = skeleton.IDs.TakeWhile(id => id != teResourceGUID.Index(hardPoint.m_FF592924))
                                .Count();
                            
                            teMtx43 boneMat = skeleton.Matrices34[boneIdx];
                            teQuat rotation = new teQuat(boneMat[0, 0], boneMat[0, 1], boneMat[0, 2], boneMat[0, 3]);
                            teVec3 scale = new teVec3(boneMat[1, 0], boneMat[1, 1], boneMat[1, 2]);
                            teVec3 translation = new teVec3(boneMat[2, 0], boneMat[2, 1], boneMat[2, 2]);
                            parentMat = Matrix.Scaling(scale) *
                                        Matrix.RotationQuaternion(rotation) *
                                        Matrix.Translation(translation);
                        }

                        Matrix hardPointMat = Matrix.RotationQuaternion(hardPoint.m_rotation) *
                                              Matrix.Translation(hardPoint.m_position);

                        hardPointMat = hardPointMat * parentMat;
                        hardPointMat.Decompose(out Vector3 _, out Quaternion rot, out Vector3 pos);
                        
                        writer.Write(pos);
                        writer.Write(rot);
                    }

                    foreach (STUModelHardpoint modelHardpoint in stu.StructuredData.m_hardPoints) {
                        writer.Write(IdToString("bone", teResourceGUID.Index(modelHardpoint.m_FF592924)));
                    }
                }
                
                // ext 1.3: old cloth
                writer.Write(0);
                
                // ext 1.4: embedded refpose
                if (skeleton != null) {
                    for (int i = 0; i < skeleton.Header.BonesAbs; ++i) {
                        writer.Write(IdToString("bone", skeleton.IDs[i]));
                        short parent = skeleton.Hierarchy[i];
                        writer.Write(parent);

                        teMtx43 bone = skeleton.Matrices34Inverted[i];
                        
                        teQuat quat = new teQuat(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                        teVec3 scl = new teVec3(bone[1, 0], bone[1, 1], bone[1, 2]);
                        teVec3 pos = new teVec3(bone[2, 0], bone[2, 1], bone[2, 2]);
                        
                        teVec3 rot = quat.ToEulerAngles();
                        writer.Write(pos);
                        writer.Write(scl);
                        writer.Write(rot);
                    }
                }
                
                writer.Write(teResourceGUID.Index(GUID));
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