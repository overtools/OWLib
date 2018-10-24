using System;
using System.IO;

namespace TankLib.Helpers {
    public class RememberMeStream : IDisposable {
        public long Position;
        private Stream inner;

        public RememberMeStream(Stream input, long offset) {
            Position = input.Position + offset;
            inner = input;
        }

        public RememberMeStream(BinaryReader input, long offset) : this(input.BaseStream, offset) { }

        public void Dispose() {
            inner.Position = Position;
        }
    }
}
