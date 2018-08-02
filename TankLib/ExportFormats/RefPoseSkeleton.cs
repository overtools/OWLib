using System.Collections.Generic;
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
            teModelChunk_Cloth cloth = ChunkedData.GetChunk<teModelChunk_Cloth>();

            if (skeleton == null) return;

            short[] hierarchy;
            Dictionary<int, teModelChunk_Cloth.ClothNode> clothNodeMap = null;
            if (cloth != null) {
                hierarchy = cloth.CreateFakeHierarchy(skeleton, out clothNodeMap);
            } else {
                hierarchy = skeleton.Hierarchy;
            }

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
                    OverwatchModel.GetRefPoseTransform(i, hierarchy, skeleton, clothNodeMap, out teVec3 scale, out teQuat quat,
                        out teVec3 pos);

                    teVec3 rot = quat.ToEulerAngles();
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}  {1:0.000000} {2:0.000000} {3:0.000000}  {4:0.000000} {5:0.000000} {6:0.000000}  {7:0.000000} {8:0.000000} {9:0.000000}",
                        i, pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z, scale.X, scale.Y, scale.Z));
                }
            }
        }
    }
}