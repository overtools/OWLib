using System;
using System.IO;

namespace TankLib {
    /// <summary>Tank AssetPakage, file type 077</summary>
    public class teAssetPackage {
        public teAssetPackagePayload Payload;

        public teAssetPackage(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        public teAssetPackage(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            throw new NotImplementedException();
        }
    }
}
