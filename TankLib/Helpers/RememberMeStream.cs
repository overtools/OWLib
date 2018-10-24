using System;
using System.IO;

namespace TankLib.Helpers {
    public class RememberMeStream : IDisposable {
        public long Position = 0;
        private Stream inner;

        public RememberMeStream(Stream input) {
            Position = input.Position;
            inner = input;
        }

        public RememberMeStream(BinaryReader input) : this(input.BaseStream) { }

        public void Dispose() {
            inner.Position = Position;
        }
    }
}
