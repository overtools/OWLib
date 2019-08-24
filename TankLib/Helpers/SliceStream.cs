using System;
using System.IO;

namespace TankLib.Helpers {
    public class SliceStream : Stream {
        private readonly Stream _baseStream;
        private readonly long _origin;
        private readonly long _length;
        
        public SliceStream(Stream stream, int length) {
            _baseStream = stream;
            _length = length;
            _origin = stream.Position;
        }

        public SliceStream(Stream stream, long origin, long length) {
            _baseStream = stream;
            _origin = origin;
            _length = length;
        }
        
        public override void Flush() {
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
                case SeekOrigin.Begin:
                    return _baseStream.Seek(offset + _origin, SeekOrigin.Begin);
                case SeekOrigin.Current:
                    return _baseStream.Seek(offset, SeekOrigin.Current);
                //case SeekOrigin.End:
                    //return _baseStream.Seek(_length - offset + _origin, SeekOrigin.End);
                default:
                    throw new NotImplementedException();
            }
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _baseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _baseStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _length;

        public override long Position {
            get => _baseStream.Position - _origin;
            set => _baseStream.Position = value + _origin;
        }
    }
}