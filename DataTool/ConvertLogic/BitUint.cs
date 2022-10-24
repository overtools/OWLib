namespace DataTool.ConvertLogic {
    public class BitUint {
        public uint Value;
        public readonly uint BitSize;

        public BitUint(uint size) {
            BitSize = size;
            Value = 0;
        }

        public BitUint(uint size, uint v) {
            BitSize = size;
            Value = v;
        }

        public static implicit operator uint(BitUint bitUint) {
            return bitUint.Value;
        }

        public int AsInt() {
            return (int) Value;
        }
    }
}