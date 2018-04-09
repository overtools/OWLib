using System.IO;
using System.Linq;
using TankLib.Chunks;
using TankLib.Math;

namespace TankLib.ExportFormats {
    /// <summary>
    /// Reference Pose (SMD) format
    /// </summary>
    public class SkeletonRefpose : IExportFormat {
        public string Extension => "smd";

        private readonly teChunkedData _data;
        
        public SkeletonRefpose(teChunkedData chunkedData) {
            _data = chunkedData;
        }

        
        public void Write(Stream stream) {
            teModelChunk_Skeleton skeleton = _data.GetChunk<teModelChunk_Skeleton>();
            teModelChunk_Hardpoint hardpoints = _data.GetChunk<teModelChunk_Hardpoint>();
            teModelChunk_Cloth cloth = _data.GetChunk<teModelChunk_Cloth>();
            
            using (BinaryWriter writer = new BinaryWriter(stream)) {
            }
        }
    }
}