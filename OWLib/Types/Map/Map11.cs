using System.IO;

namespace OWLib.Types.Map {
    public class Map11 {
        public OWLib.STUD main;
        public OWLib.STUD secondary;

        public Map11(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                long offset1 = reader.ReadInt64();
                long offset2 = reader.ReadInt64();

                input.Position = offset1;
                main = new OWLib.STUD(input, true, STUDManager.Instance, true, false);
                input.Position = offset2;
                secondary = new OWLib.STUD(input, true, STUDManager.Instance, true, false);
            }
        }
    }
}
