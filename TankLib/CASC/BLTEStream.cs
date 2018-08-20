using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using CMFLib;

namespace TankLib.CASC {
    /// <summary>Thrown when the BLTE reader encounters invalid data</summary>
    [Serializable]
    public class BLTEDecoderException : Exception {
        public BLTEDecoderException(string message) : base(message) { }

        public BLTEDecoderException(string fmt, params object[] args) : base(string.Format(fmt, args)) { }
    }

    /// <summary>Thrown when the BLTE reader is missing a key</summary>
    [Serializable]
    public class BLTEKeyException : Exception {
        public ulong MissingKey;

        public BLTEKeyException(ulong key) : base($"unknown keyname ${key:X16}") {
            MissingKey = key;
        }
    }

    /// <summary>BLTE block</summary>
    internal class DataBlock {
        public int CompSize;
        public int DecompSize;
        public MD5Hash Hash;
        public byte[] Data;
    }

    /// <summary>BLTE encoded stream</summary>
    public class BLTEStream : Stream {
        public static bool Graceful = false;
        private const byte EncryptionSalsa20 = 0x53;
        private const byte EncryptionArc4 = 0x41;
        public static readonly int BLTEMagic = Util.GetMagicBytesBE('B', 'L', 'T', 'E');

        private BinaryReader _reader;
        private MemoryStream _memStream;
        private DataBlock[] _dataBlocks;
        private Stream _stream;
        private int _blocksIndex;
        private long _length;
        
        public ulong SalsaKey { get; protected set; }
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        
        public override long Position {
            get => _memStream.Position;
            set {
                while (value > _memStream.Length)
                    if (!ProcessNextBlock())
                        break;

                _memStream.Position = value;
            }
        }

        public BLTEStream(Stream src, MD5Hash md5) {
            _stream = src;
            _reader = new BinaryReader(src);

            Parse(md5);
        }

        private void Parse(MD5Hash md5) {
            int size = (int) _reader.BaseStream.Length;

            if (size < 8)
                throw new BLTEDecoderException("not enough data: {0}", 8);

            int magic = _reader.ReadInt32();

            if (magic != BLTEMagic) {
                if (Graceful) {
                    _memStream = new MemoryStream();
                    _stream.Position = 0;
                    _stream.CopyTo(_memStream);
                    _memStream.Position = 0;
                    _length = _memStream.Length;
                    _dataBlocks = new DataBlock[0];
                    return;
                }

                throw new BLTEDecoderException("frame header mismatch (bad BLTE file)");
            }

            int headerSize = _reader.ReadInt32BE();

            /*if (CASCConfig.ValidateData)
            {
                long oldPos = _reader.BaseStream.Position;

                _reader.BaseStream.Position = 0;

                byte[] newHash = _md5.ComputeHash(_reader.ReadBytes(headerSize > 0 ? headerSize : size));

                if (!md5.EqualsTo(newHash))
                    throw new BLTEDecoderException("data corrupted");

                _reader.BaseStream.Position = oldPos;
            }*/

            int numBlocks = 1;

            if (headerSize > 0) {
                if (size < 12)
                    throw new BLTEDecoderException("not enough data: {0}", 12);

                byte[] fcbytes = _reader.ReadBytes(4);

                numBlocks = (fcbytes[1] << 16) | (fcbytes[2] << 8) | (fcbytes[3] << 0);

                if (fcbytes[0] != 0x0F || numBlocks == 0)
                    throw new BLTEDecoderException("bad table format 0x{0:x2}, numBlocks {1}", fcbytes[0], numBlocks);

                int frameHeaderSize = 24 * numBlocks + 12;

                if (headerSize != frameHeaderSize)
                    throw new BLTEDecoderException("header size mismatch");

                if (size < frameHeaderSize)
                    throw new BLTEDecoderException("not enough data: {0}", frameHeaderSize);
            }

            _dataBlocks = new DataBlock[numBlocks];

            for (int i = 0; i < numBlocks; i++) {
                DataBlock block = new DataBlock();

                if (headerSize != 0) {
                    block.CompSize = _reader.ReadInt32BE();
                    block.DecompSize = _reader.ReadInt32BE();
                    block.Hash = _reader.Read<MD5Hash>();
                } else {
                    block.CompSize = size - 8;
                    block.DecompSize = size - 8 - 1;
                    block.Hash = default;
                }

                _dataBlocks[i] = block;
            }

            _memStream = new MemoryStream(_dataBlocks.Sum(b => b.DecompSize));

            ProcessNextBlock();

            _length = headerSize == 0 ? _memStream.Length : _memStream.Capacity;

            //for (int i = 0; i < _dataBlocks.Length; i++)
            //{
            //    ProcessNextBlock();
            //}
        }

        private void HandleDataBlock(byte[] data, int index) {
            switch (data[0]) {
                case 0x45: // E (encrypted)
                    byte[] decrypted = Decrypt(data, index);
                    HandleDataBlock(decrypted, index);
                    break;
                case 0x46: // F (frame, recursive)
                    throw new BLTEDecoderException("DecoderFrame not implemented");
                case 0x4E: // N (not compressed)
                    _memStream.Write(data, 1, data.Length - 1);
                    break;
                case 0x5A: // Z (zlib compressed)
                    Decompress(data, _memStream);
                    break;
                default:
                    throw new BLTEDecoderException("unknown BLTE block type {0} (0x{1:X2})!", (char) data[0], data[0]);
            }
        }

        private byte[] Decrypt(byte[] data, int index) {
            byte keyNameSize = data[1];

            if (keyNameSize == 0 || keyNameSize != 8)
                throw new BLTEDecoderException("keyNameSize == 0 || keyNameSize != 8");

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 2, keyNameBytes, 0, keyNameSize);

            ulong keyName = BitConverter.ToUInt64(keyNameBytes, 0);
            SalsaKey = keyName;

            byte ivSize = data[keyNameSize + 2];

            if (ivSize != 4 || ivSize > 0x10)
                throw new BLTEDecoderException("IVSize != 4 || IVSize > 0x10");

            byte[] ivPart = new byte[ivSize];
            Array.Copy(data, keyNameSize + 3, ivPart, 0, ivSize);

            if (data.Length < ivSize + keyNameSize + 4)
                throw new BLTEDecoderException("data.Length < IVSize + keyNameSize + 4");

            int dataOffset = keyNameSize + ivSize + 3;

            byte encType = data[dataOffset];

            if (encType != EncryptionSalsa20 && encType != EncryptionArc4) // 'S' or 'A'
                throw new BLTEDecoderException("encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4");

            dataOffset++;

            // expand to 8 bytes
            byte[] iv = new byte[8];
            Array.Copy(ivPart, iv, ivPart.Length);

            // magic
            for (int shift = 0, i = 0; i < sizeof(int); shift += 8, i++) iv[i] ^= (byte) ((index >> shift) & 0xFF);

            byte[] key = TACTKeyService.GetKey(keyName);

            if (key == null)
                throw new BLTEKeyException(keyName);

            if (encType == EncryptionSalsa20) {
                ICryptoTransform decryptor = TACTKeyService.SalsaInstance.CreateDecryptor(key, iv);

                return decryptor.TransformFinalBlock(data, dataOffset, data.Length - dataOffset);
            }

            // ARC4 ?
            throw new BLTEDecoderException("encType ENCRYPTION_ARC4 not implemented");
        }

        private static void Decompress(byte[] data, Stream outStream) {
            // skip first 3 bytes (zlib)
            using (MemoryStream ms = new MemoryStream(data, 3, data.Length - 3))
            using (DeflateStream dfltStream = new DeflateStream(ms, CompressionMode.Decompress)) {
                dfltStream.CopyTo(outStream);
            }
        }

        public override void Flush() {
            _stream.Flush();
            _memStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (_memStream.Position + count > _memStream.Length && _blocksIndex < _dataBlocks.Length) {
                if (!ProcessNextBlock())
                    return 0;

                return Read(buffer, offset, count);
            }

            return _memStream.Read(buffer, offset, count);
        }

        private bool ProcessNextBlock() {
            if (_blocksIndex == _dataBlocks.Length)
                return false;

            long oldPos = _memStream.Position;
            _memStream.Position = _memStream.Length;

            DataBlock block = _dataBlocks[_blocksIndex];

            block.Data = _reader.ReadBytes(block.CompSize);

            //            if (!block.Hash.IsZeroed() && CASCConfig.ValidateData)
            //            {
            //                byte[] blockHash = _md5.ComputeHash(block.Data);
            //
            //                if (!block.Hash.EqualsTo(blockHash))
            //                    throw new BLTEDecoderException("MD5 mismatch");
            //            }

            HandleDataBlock(block.Data, _blocksIndex);
            _blocksIndex++;

            _memStream.Position = oldPos;

            return true;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException();
        }

        protected override void Dispose(bool disposing) {
            try {
                if (!disposing) return;
                _stream?.Dispose();
                _reader?.Dispose();
                _memStream?.Dispose();
            } finally {
                _stream = null;
                _reader = null;
                _memStream = null;

                base.Dispose(disposing);
            }
        }
    }
}