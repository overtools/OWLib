using System;
using System.IO;

namespace TankLib.Helpers {
    public class RememberMeStream : IDisposable {
        public long BasePosition;
        public long Position;
        private Stream inner;

        public RememberMeStream(Stream input, long offset) {
            BasePosition = input.Position;
            Position = BasePosition + offset;
            inner = input;
        }

        public RememberMeStream(BinaryReader input, long offset) : this(input.BaseStream, offset) { }

        public void Dispose() {
            inner.Position = Position;
        }
    }
}
