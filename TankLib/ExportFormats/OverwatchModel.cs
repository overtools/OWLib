using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using TankLib.Chunks;
using TankLib.Helpers;
using TankLib.Math;
using TankLib.STU.Types;

namespace TankLib.ExportFormats {
    public class StreamingLodsInfo {
        public readonly teModelChunk_Model m_model;
        public readonly teModelChunk_RenderMesh m_renderMesh;

        public StreamingLodsInfo(teChunkedData chunkedData) {
            m_model = chunkedData.GetChunk<teModelChunk_Model>();
            m_renderMesh = chunkedData.GetChunk<teModelChunk_RenderMesh>();
        }
    }

    /// <summary>
    /// OWMDL format
    /// </summary>
    public class OverwatchModel : IExportFormat {
        public string Extension => "owmdl";

        private readonly teChunkedData _data;

        public string ModelLookFileName;
        public string Name;
        public ulong GUID;

        public readonly StreamingLodsInfo m_streamedLods;

        public OverwatchModel(teChunkedData chunkedData, ulong guid, string name, StreamingLodsInfo streamedLods=null) {
            _data = chunkedData;
            GUID = guid;
            Name = name;
            m_streamedLods = streamedLods;
        }

        private struct TempSubmesh {
            public readonly teModelChunk_RenderMesh.Submesh m_submesh;
            public readonly ulong m_material;

            public TempSubmesh(teModelChunk_RenderMesh.Submesh submesh, teModelChunk_Model srcModel) {
                m_submesh = submesh;
                m_material = srcModel.Materials[submesh.Descriptor.Material];
            }
        }

        public void Write(Stream stream) {
            teModelChunk_RenderMesh renderMesh = _data.GetChunk<teModelChunk_RenderMesh>();
            teModelChunk_Skeleton skeleton = _data.GetChunk<teModelChunk_Skeleton>();
            teModelChunk_Cloth cloth = _data.GetChunk<teModelChunk_Cloth>();
            teModelChunk_STU stu = _data.GetChunk<teModelChunk_STU>();

            var allSubmeshes = new List<TempSubmesh>();
            if (renderMesh?.Submeshes != null) {
                var model = _data.GetChunk<teModelChunk_Model>();
                allSubmeshes.AddRange(renderMesh.Submeshes.Select(x => new TempSubmesh(x, model)));
            }
            if (m_streamedLods?.m_renderMesh != null) {
                // todo: can multiple stream payloads be required? and therefore we miss meshes again?
                // i wasn't paying attention in class
                 
                // todo: also turns out this wasn't needed for necromancer, which is why i write this code. (instead lod was equality instead of bitflag)
                // but i guess its useful if we wanted to add lod models back
                 
                var streamedModel = m_streamedLods.m_model;
                allSubmeshes.AddRange(m_streamedLods.m_renderMesh.Submeshes.Select(x => new TempSubmesh(x, streamedModel)));
            }

            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((ushort)2);
                writer.Write((ushort)0);
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
                writer.Write(teResourceGUID.Index(GUID));

                var highestLOD = 1;
                var submeshesWithLod = allSubmeshes.Where(x => x.m_submesh.Descriptor.LOD != -1);
                if (submeshesWithLod.Any()) {
                    highestLOD = submeshesWithLod.Min(x => x.m_submesh.Descriptor.LOD);
                }

                TempSubmesh[] submeshesToWrite = allSubmeshes.Where(x => (x.m_submesh.Descriptor.LOD & highestLOD) != 0 || x.m_submesh.Descriptor.LOD == -1).ToArray();

                short[] hierarchy = null;
                Dictionary<int, teModelChunk_Cloth.ClothNode> clothNodeMap = null;
                if (skeleton != null) {
                    if (cloth != null) {
                        hierarchy = cloth.CreateFakeHierarchy(skeleton, out clothNodeMap);
                    } else {
                        hierarchy = skeleton.Hierarchy;
                    }

                    writer.Write(skeleton.Header.BonesAbs);
                } else {
                    writer.Write((ushort)0);
                }

                writer.Write((uint)submeshesToWrite.Length);

                if (stu?.StructuredData.m_hardPoints != null) {
                    writer.Write(stu.StructuredData.m_hardPoints.Length);
                } else {
                    writer.Write(0);  // hardpoints
                }

                if (skeleton != null) {
                    //Console.Out.WriteLine($"SKELETON {GUID:X16} {skeleton.Header.BonesAbs} {skeleton.Header.BonesSimple} {skeleton.Header.BonesCloth} {skeleton.Header.RemapCount} {skeleton.Header.IDCount}");
                    for (int i = 0; i < skeleton.Header.BonesAbs; ++i) { // todo: CLOTH BONES WHERE GONE.
                        writer.Write(IdToString("bone", i >= skeleton.IDs.Length ? (long) -i : skeleton.IDs[i]));
                        short parent = hierarchy[i];
                        writer.Write(parent);

                        GetRefPoseTransform(i, hierarchy, skeleton, clothNodeMap, out teVec3 scale, out teQuat quat, out teVec3 translation);
                        writer.Write(translation);
                        writer.Write(scale);
                        writer.Write(quat.ToEulerAngles());
                    }
                }

                int submeshIdx = 0;
                foreach (TempSubmesh submeshA in submeshesToWrite) {
                    var submesh = submeshA.m_submesh;
                    
                    writer.Write($"Submesh_{submeshIdx}.{submeshA.m_material:X16}");
                    writer.Write(submeshA.m_material);
                    writer.Write(submesh.UVCount);

                    writer.Write(submesh.Vertices.Length);
                    writer.Write(submesh.Indices.Length / 3);

                    var boneIndicesCount = 0;
                    if (skeleton != null && submesh.BoneIndices[0] != null) {
                        boneIndicesCount = submesh.BoneIndices.Max(x => x.Length);
                    }
                    writer.Write((byte) boneIndicesCount);

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Vertices[j].X);
                        writer.Write(-submesh.Vertices[j].Z);
                        writer.Write(submesh.Vertices[j].Y);
                    }

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Normals[j].X);
                        writer.Write(-submesh.Normals[j].Z);
                        writer.Write(submesh.Normals[j].Y);
                    }

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Tangents[j].X);
                        writer.Write(-submesh.Tangents[j].Z);
                        writer.Write(submesh.Tangents[j].Y);
                        writer.Write(submesh.Tangents[j].W);
                    }

                    for (int u = 0; u < submesh.UVCount; u++) {
                        for (int j = 0; j < submesh.Vertices.Length; j++) {
                            writer.Write(submesh.UV[j][u].X);
                            writer.Write(1-submesh.UV[j][u].Y);
                        }
                    }

                    if (skeleton != null && submesh.BoneIndices[0] != null) {
                        for (int j = 0; j < submesh.Vertices.Length; j++) {
                            var k = 0;
                            for (; k < submesh.BoneIndices[j].Length; ++k) {
                                var ind = submesh.BoneIndices[j][k];
                                if (ind >= skeleton.Lookup.Length) {
                                    writer.Write((ushort) 0xFFFF);
                                } else {
                                    var bone = skeleton.Lookup[ind];
                                    writer.Write(bone);
                                }
                            }

                            for (; k < boneIndicesCount; ++k) {
                                writer.Write((ushort) 0xFFFF);
                            }
                        }

                        for (int j = 0; j < submesh.Vertices.Length; j++) {
                            var k = 0;
                            for (; k < submesh.BoneWeights[j].Length; ++k) {
                                writer.Write(submesh.BoneWeights[j][k]);
                            }

                            for (; k < boneIndicesCount; ++k) {
                                writer.Write(0.0f);
                            }
                        }
                    }

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Color1.ElementAtOrDefault(j));
                    }

                    for (int j = 0; j < submesh.Vertices.Length; j++) {
                        writer.Write(submesh.Color2.ElementAtOrDefault(j));
                    }

                    for (int j = 0; j < submesh.Descriptor.IndicesToDraw; j+=3) {
                        writer.Write((int)submesh.Indices[j]);
                        writer.Write((int)submesh.Indices[j+1]);
                        writer.Write((int)submesh.Indices[j+2]);
                    }

                    submeshIdx++;
                }


                if (stu?.StructuredData.m_hardPoints != null) {
                    foreach (STUModelHardpoint hardPoint in stu.StructuredData.m_hardPoints) {
                        writer.Write(IdToString("hardpoint", teResourceGUID.Index(hardPoint.m_EDF0511C)));
                        writer.Write(IdToString("bone", teResourceGUID.Index(hardPoint.m_FF592924)));

                        Matrix4x4 parentMat = Matrix4x4.Identity;
                        if (hardPoint.m_FF592924 != 0 && skeleton != null) {
                            int? boneIdx = null;
                            var hardPointBoneID = teResourceGUID.Index(hardPoint.m_FF592924);
                            for (int i = 0; i < skeleton.IDs.Length; i++) {
                                var currBoneID = skeleton.IDs[i];
                                if (currBoneID == hardPointBoneID) {
                                    boneIdx = i;
                                    break;
                                }
                            }

                            if (boneIdx == null) {
                                parentMat = Matrix4x4.Identity;
                                Logger.Debug("OverwatchModel",
                                             $"Hardpoint {teResourceGUID.AsString(hardPoint.m_EDF0511C)} is attached to bone {teResourceGUID.AsString(hardPoint.m_FF592924)} which doesn't exist on model {teResourceGUID.AsString(GUID)}");
                            } else {
                                parentMat = skeleton.GetWorldSpace(boneIdx.Value);
                            }
                        }

                        Matrix4x4 hardPointMat = Matrix4x4.CreateFromQuaternion(hardPoint.m_rotation) *
                                                 Matrix4x4.CreateTranslation(hardPoint.m_position);

                        hardPointMat *= parentMat;
                        Matrix4x4.Decompose(hardPointMat, out _, out var worldOri, out var worldTranslation);

                        writer.Write(worldTranslation);
                        writer.Write(worldOri);
                    }
                }
            }
        }

        public static void GetRefPoseTransform(int i, short[] hierarchy, teModelChunk_Skeleton skeleton, Dictionary<int, teModelChunk_Cloth.ClothNode> clothNodeMap, out teVec3 scale, out teQuat quat,
            out teVec3 translation) {
            if (clothNodeMap != null && clothNodeMap.ContainsKey(i)) {
                Matrix4x4 thisMat = skeleton.GetWorldSpace(i);
                Matrix4x4 parentMat = skeleton.GetWorldSpace(hierarchy[i]);

                Matrix4x4.Invert(parentMat, out var invParentMat);
                Matrix4x4 newParentMat = thisMat * invParentMat;

                Matrix4x4.Decompose(newParentMat, out var scl2, out var rot2, out var pos2);

                quat = new teQuat(rot2.X, rot2.Y, rot2.Z, rot2.W);
                scale = new teVec3(scl2.X, scl2.Y, scl2.Z);
                translation = new teVec3(pos2.X, pos2.Y, pos2.Z);
            } else {
                teMtx43 bone = skeleton.Matrices34Inverted[i];

                scale = new teVec3(bone[1, 0], bone[1, 1], bone[1, 2]);
                quat = new teQuat(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                translation = new teVec3(bone[2, 0], bone[2, 1], bone[2, 2]);
            }
        }

        public static string IdToString(string prefix, long id) {  // hmm
            // hack fix.
            return id < 0 ? $"cloth_{-id:X4}" : $"{prefix}_{id:X4}";
        }
    }
}
