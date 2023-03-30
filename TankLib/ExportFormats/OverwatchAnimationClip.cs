using System.Collections.Generic;
using System.IO;
using TankLib.Math;
using static TankLib.teAnimation;

namespace TankLib.ExportFormats {
    /// <summary>
    /// owanimclip format
    /// </summary>
    public class OverwatchAnimationClip : IExportFormat {
        public enum TrackType {
            Position = 0,
            Rotation = 1,
            Scale = 2
        }

        public string Extension => "owanimclip";
        public readonly teAnimation Animation;

        public OverwatchAnimationClip(teAnimation animation) {
            Animation = animation;
        }

        public void Write(Stream stream) {
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((ushort) 2);
                writer.Write((ushort) 0);

                writer.Write(Animation.BoneList.Length);
                writer.Write(Animation.Header.FPS);
                writer.Write(Animation.InfoTableSize);

                var boneIndex = 0;
                foreach (BoneAnimation boneAnimation in Animation.BoneAnimations) {
                    writer.Write(OverwatchModel.IdToString("bone", (uint) Animation.BoneList[boneIndex]));
                    boneIndex++;
                    var trackCount = 0;
                    if (boneAnimation.Positions.Count > 0) {
                        trackCount++;
                    }

                    if (boneAnimation.Scales.Count > 0) {
                        trackCount++;
                    }

                    if (boneAnimation.Rotations.Count > 0) {
                        trackCount++;
                    }

                    writer.Write(trackCount);

                    if (boneAnimation.Positions.Count > 0) {
                        writer.Write((uint) TrackType.Position);
                        writer.Write(boneAnimation.Positions.Count);
                        writer.Write(3);
                        foreach (KeyValuePair<int, teVec3> pair in boneAnimation.Positions) {
                            writer.Write(pair.Key);
                            writer.Write(pair.Value.X);
                            writer.Write(pair.Value.Y);
                            writer.Write(pair.Value.Z);
                        }
                    }


                    if (boneAnimation.Rotations.Count > 0) {
                        writer.Write((uint) TrackType.Rotation);
                        writer.Write(boneAnimation.Rotations.Count);
                        writer.Write(4);
                        foreach (KeyValuePair<int, teQuat> pair in boneAnimation.Rotations) {
                            writer.Write(pair.Key);
                            writer.Write(pair.Value.X);
                            writer.Write(pair.Value.Y);
                            writer.Write(pair.Value.Z);
                            writer.Write(pair.Value.W);
                        }
                    }


                    if (boneAnimation.Scales.Count > 0) {
                        writer.Write((uint) TrackType.Scale);
                        writer.Write(boneAnimation.Scales.Count);
                        writer.Write(3);
                        foreach (KeyValuePair<int, teVec3> pair in boneAnimation.Scales) {
                            writer.Write(pair.Key);
                            writer.Write(pair.Value.X);
                            writer.Write(pair.Value.Y);
                            writer.Write(pair.Value.Z);
                        }
                    }
                }
            }
        }
    }
}
