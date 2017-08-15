using System.Collections.Generic;
using System.IO;
using static STULib.Types.Generic.Common;
using static STULib.Types.Generic.Version2;

namespace STULib.Impl {
    public class Version2 : ISTU {
        public override IEnumerator<STUInstance> Instances => null;
        public override uint Version => 2;

        public override void Dispose() {
            return;
        }

        internal static bool IsValidVersion(BinaryReader reader) {
            return reader.BaseStream.Length >= 36;
        }

        public Version2(Stream stream, uint owVersion) {

        }
    }
}
