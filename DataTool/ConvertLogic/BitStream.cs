using System;
using System.IO;

namespace DataTool.ConvertLogic {
    public class BitStream : IDisposable {
        private readonly BinaryReader _reader;
        private byte _current;
        public byte BitsLeft;
        public int TotalBitsRead;

        public BitStream(BinaryReader reader) {
            _reader = reader;
        }

        public bool GetBit() {
            if (BitsLeft == 0) {
                _current = _reader.ReadByte();
                // if (c == EOF) throw Out_of_bits();
                // bit_buffer = c;
                BitsLeft = 8;
            }

            TotalBitsRead++;
            BitsLeft--;
            return (_current & (0x80 >> BitsLeft)) != 0;
        }

        public void Read(BitUint bitUint) {
            bitUint.Value = 0;
            for (int i = 0; i < bitUint.BitSize; i++) {
                if (GetBit()) bitUint.Value |= (1U << i);
            }
        }

        public void Dispose() {
            _reader?.Dispose();
        }
    }
}
