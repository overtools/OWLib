using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static OWLib.Extensions;

namespace DataTool.ConvertLogic {
    public static class Sound {
        private static bool ArraysEqual(IReadOnlyCollection<byte> a1, IReadOnlyList<byte> a2) {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            EqualityComparer<byte> comparer = EqualityComparer<byte>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        public static uint SwapBytes(uint x) {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public static ushort SwapBytes(ushort x) => (ushort) SwapBytes((uint) x);

        /*class Packet {
            long _offset;
            ushort _size;
            uint _absolute_granule;
            bool _no_granule;

            public Packet(BinaryReader reader, long o, bool little_endian, bool no_granule = false) {
                reader.BaseStream.Seek(_offset, SeekOrigin.Current);
                if (little_endian) {
                    _size = reader.ReadUInt16();
                    // _size = read_16_le(i);
                    if (!_no_granule) {
                        _absolute_granule = reader.ReadUInt32();
                    }
                } else {
                    Debugger.Break();
                    _size = SwapBytes(reader.ReadUInt16());
                    if (!_no_granule) {
                        _absolute_granule = SwapBytes(reader.ReadUInt32());
                    }
                }
            }

            public long header_size() {
                return _no_granule ? 2 : 6;
            }

            public long offset() {
                return _offset + header_size();
            }

            public ushort size() {
                return _size;
            }

            public uint granule() {
                return _absolute_granule;
            }

            public long next_offset() {
                return _offset + header_size() + _size;
            }
        }

        /* Old 8 byte header #1#
        class Packet_8 {
            long _offset;
            uint _size;
            uint _absolute_granule;

            public Packet_8(BinaryReader reader, long o, bool little_endian) {
                reader.BaseStream.Seek(_offset, SeekOrigin.Current);
                if (little_endian) {
                    _size = reader.ReadUInt32();
                    _absolute_granule = reader.ReadUInt32();
                } else {
                    _size = SwapBytes(reader.ReadUInt32());
                    _absolute_granule = SwapBytes(reader.ReadUInt32());
                }
            }

            long header_size() {
                return 8;
            }

            long offset() {
                return _offset + header_size();
            }

            uint size() {
                return _size;
            }

            uint granule() {
                return _absolute_granule;
            }

            long next_offset() {
                return _offset + header_size() + _size;
            }
        };*/

        public class VorbisPacketHeader {
            public byte Type;

            public readonly char[] VorbisStr = {'v', 'o', 'r', 'b', 'i', 's'};

            public VorbisPacketHeader(byte type) {
                Type = type;
            }
        }
        
        // ReSharper disable once InconsistentNaming
        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        public class WwiseRIFFVorbis {
            private string _codebooksFile;
            private Stream _stream;
#pragma warning disable 414
            private bool _littleEndian;
            private long _fileSize;
            private uint _riffSize;
            private char[] _waveHead;
            private char[] _riffHead;
            private long _chunkOffset;
            private long _fmtOffset;
            private long _cueOffset;
            private long _listOffset;
            private long _smplOffset;
            private long _vorbOffset;
            private long _dataOffset;

            private uint _fmtSize;
            private uint _cueSize;
            private uint _listSize;
            private uint _smplSize;
            private uint _vorbSize;
            private uint _dataSize;
            
            private ushort _channels;
            private uint _sampleRate;
            private uint _avgBytesPerSecond;
            private uint _subtype;
            private ushort _extUnk;

            private uint _cueCount;

            private uint _loopCount;
            private uint _loopStart;
            private uint _loopEnd;
            
            private uint _setupPacketOffset;
            private uint _firstAudioPacketOffset;
            
            private uint _sampleCount;
            private bool _noGranule;
            private bool _modPackets;
            
            private bool _headerTriadPresent;
            private bool _oldPacketHeaders;
            private uint _uid;
            private byte _blocksize0Pow;
            private byte _blocksize1Pow;
#pragma warning restore 414
            
            public WwiseRIFFVorbis(Stream stream, string codebooksFile) {
                _codebooksFile = codebooksFile;
                _stream = stream;
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                    _fileSize = reader.BaseStream.Length;

                    reader.BaseStream.Seek(0, SeekOrigin.Begin);

                    #region check header
                    _riffHead = reader.ReadChars(4);
                    if (new string(_riffHead) != "RIFX") {
                        if (new string(_riffHead) != "RIFF") {
                            throw new Exception("missing RIFF");
                        }
                        _littleEndian = true;
                    }

                    ushort Read16() => _littleEndian ? reader.ReadUInt16() : SwapBytes(reader.ReadUInt16());
                    uint Read32() => _littleEndian ? reader.ReadUInt32() : SwapBytes(reader.ReadUInt32());

                    _riffSize = Read32() + 8;
                    if (_riffSize > _fileSize) throw new Exception("RIFF truncated");
                    _waveHead = reader.ReadChars(4);

                    if (new string(_waveHead) != "WAVE") throw new Exception("Missing WAVE");
                    #endregion

                    #region read chunks
                    _chunkOffset = 12;
                    _fmtOffset = -1;
                    _cueOffset = -1;
                    _listOffset = -1;
                    _smplOffset = -1;
                    _vorbOffset = -1;
                    _dataOffset = -1;

                    _fmtSize = 0;
                    _cueSize = 0;
                    _listSize = 0;
                    _smplSize = 0;
                    _vorbSize = 0;
                    _dataSize = 0;
                    while (_chunkOffset < _riffSize) {
                        reader.BaseStream.Seek(_chunkOffset, SeekOrigin.Begin);
                        if (_chunkOffset + 8 > _riffSize) throw new Exception("chunk header truncated");
                        
                        char[] chunkType = reader.ReadChars(4);
                        string chunkTypeString = new string(chunkType);
                        uint chunkSize = Read32();

                        if (chunkTypeString == "fmt ") {
                            _fmtOffset = _chunkOffset + 8;
                            _fmtSize = chunkSize;
                        } else if (chunkTypeString == "cue ") {
                            _cueOffset = _chunkOffset + 8;
                            _cueSize = chunkSize;
                        } else if (chunkTypeString == "LIST") {
                            _listOffset = _chunkOffset + 8;
                            _listSize = chunkSize;
                        } else if (chunkTypeString == "smpl") {
                            _smplOffset = _chunkOffset + 8;
                            _smplSize = chunkSize;
                        } else if (chunkTypeString == "vorb") {
                            _vorbOffset = _chunkOffset + 8;
                            _vorbSize = chunkSize;
                        } else if (chunkTypeString == "data") {
                            _dataOffset = _chunkOffset + 8;
                            _dataSize = chunkSize;
                        }

                        _chunkOffset = _chunkOffset + 8 + chunkSize;
                    }
                    #endregion
                    
                    if (_chunkOffset > _riffSize) throw new Exception("chunk truncated");
                    
                    if (-1 == _fmtOffset && -1 == _dataOffset) throw new Exception("expected fmt, data chunks");
                    
                    #region read fmt
                    if (-1 == _vorbOffset && 0x42 != _fmtSize) throw new Exception("expected 0x42 fmt if vorb missing");

                    if (-1 != _vorbOffset && 0x28 != _fmtSize && 0x18 != _fmtSize && 0x12 != _fmtSize) throw new Exception("bad fmt size");

                    if (-1 == _vorbOffset && 0x42 == _fmtSize){
                        // fake it out
                        _vorbOffset = _fmtOffset + 0x18;
                    }

                    reader.BaseStream.Seek(_fmtOffset, SeekOrigin.Begin);
                    if (0xFFFF != Read16()) throw new Exception("bad codec id");
                    
                    _channels = Read16();
                    _sampleRate = Read32();
                    _avgBytesPerSecond = Read32();
                    _subtype = 0;
                    _extUnk = 0;
                    
                    if (0U != Read16()) throw new Exception("bad block align");
                    if (0U != Read16()) throw new Exception("expected 0 bps");
                    if (_fmtSize-0x12 != Read16()) throw new Exception("bad extra fmt length");

                    if (_fmtSize-0x12 >= 2) {
                        // read extra fmt
                        _extUnk = Read16();
                        if (_fmtSize-0x12 >= 6) {
                            _subtype = Read32();
                        }
                    }

                    if (_fmtSize == 0x28) {
                        byte[] whoknowsbufCheck = {1,0,0,0, 0,0,0x10,0, 0x80,0,0,0xAA, 0,0x38,0x9b,0x71};
                        byte[] whoknowsbuf = reader.ReadBytes(16);
                        if (!ArraysEqual(whoknowsbuf, whoknowsbufCheck)) throw new Exception("expected signature in extra fmt?");
                    }
                    #endregion

                    #region read cue
                    _cueCount = 0;
                    if (-1 != _cueOffset) {
                        reader.BaseStream.Seek(_cueOffset, SeekOrigin.Begin);

                        _cueCount = Read32();
                    }
                    #endregion
    
                    #region read LIST
                    if (-1 != _listOffset) {
                        // this is all disabled in ww2ogg
                        
                        //if ( 4 != _LIST_size ) throw Parse_error_str("bad LIST size");
                        //char adtlbuf[4];
                        //const char adtlbuf_check[4] = {'a','d','t','l'};
                        //_infile.seekg(_LIST_offset);
                        //_infile.read(adtlbuf, 4);
                        //if (memcmp(adtlbuf, adtlbuf_check, 4)) throw Parse_error_str("expected only adtl in LIST");
                    }
                    #endregion
                    
                    #region read smpl
                    _loopCount = 0;
                    _loopStart = 0;
                    _loopEnd = 0;
                    if (-1 != _smplOffset) {
                        reader.BaseStream.Seek(_smplOffset+0x1C, SeekOrigin.Begin);
                        _loopCount = Read32();

                        if (1 != _loopCount) throw new Exception("expected one loop");

                        reader.BaseStream.Seek(_smplOffset+0x2c, SeekOrigin.Begin);
                        _loopStart = Read32();
                        _loopEnd = Read32();
                    }
                    #endregion
                    
                    #region read vorb
                    switch (_vorbSize) {
                        case 0:
                        case 0x28:
                        case 0x2A:
                        case 0x2C:
                        case 0x32:
                        case 0x34:
                            reader.BaseStream.Seek(_vorbOffset+0x00, SeekOrigin.Begin);
                            break;

                        default:
                            throw new Exception("bad vorb size");
                    }
                    
                    _sampleCount = Read32();
                    _noGranule = false;
                    _modPackets = false;
                    switch (_vorbSize) {
                        case 0:
                        case 0x2A:
                            _noGranule = true;

                            reader.BaseStream.Seek(_vorbOffset + 0x4, SeekOrigin.Begin);
                            uint modSignal = Read32();

                            // set
                            // D9     11011001
                            // CB     11001011
                            // BC     10111100
                            // B2     10110010
                            // unset
                            // 4A     01001010
                            // 4B     01001011
                            // 69     01101001
                            // 70     01110000
                            // A7     10100111 !!!

                            // seems to be 0xD9 when _mod_packets should be set
                            // also seen 0xCB, 0xBC, 0xB2
                            if (0x4A != modSignal && 0x4B != modSignal && 0x69 != modSignal && 0x70 != modSignal)
                            {
                                _modPackets = true;
                            }
                            reader.BaseStream.Seek(_vorbOffset + 0x10, SeekOrigin.Begin);
                            break;

                        default:
                            reader.BaseStream.Seek(_vorbOffset + 0x18, SeekOrigin.Begin);
                            break;
                    }
                    
                    _setupPacketOffset = Read32();
                    _firstAudioPacketOffset = Read32();
                    
                    switch (_vorbSize)
                    {
                        case 0:
                        case 0x2A:
                            reader.BaseStream.Seek(_vorbOffset + 0x24, SeekOrigin.Begin);
                            break;

                        case 0x32:
                        case 0x34:
                            reader.BaseStream.Seek(_vorbOffset + 0x2C, SeekOrigin.Begin);
                            break;
                    }

                    _headerTriadPresent = false;
                    _oldPacketHeaders = false;
                    _uid = 0;
                    _blocksize0Pow = 0;
                    _blocksize1Pow = 0;
                    switch(_vorbSize)
                    {
                        case 0x28:
                        case 0x2C:
                            // ok to leave _uid, _blocksize_0_pow and _blocksize_1_pow unset
                            _headerTriadPresent = true;
                            _oldPacketHeaders = true;
                            break;

                        case 0:
                        case 0x2A:
                        case 0x32:
                        case 0x34:
                            _uid = Read32();
                            _blocksize0Pow = reader.ReadByte();
                            _blocksize1Pow = reader.ReadByte();
                            break;
                    }
                    
                    #endregion
                    
                    // check/set loops now that we know total sample count
                    if (0 != _loopCount)
                    {
                        if (_loopEnd == 0)
                        {
                            _loopEnd = _sampleCount;
                        }
                        else
                        {
                            _loopEnd = _loopEnd + 1;
                        }

                        if (_loopStart >= _sampleCount || _loopEnd > _sampleCount || _loopStart > _loopEnd)
                            throw new Exception("loops out of range");                    
                    }

                    // check subtype now that we know the vorb info
                    // this is clearly just the channel layout
                    // switch (_subtype)
                    // {
                    //     case 4:     /* 1 channel, no seek table */
                    //     case 3:     /* 2 channels */
                    //     case 0x33:  /* 4 channels */
                    //     case 0x37:  /* 5 channels, seek or not */
                    //     case 0x3b:  /* 5 channels, no seek table */
                    //     case 0x3f:  /* 6 channels, no seek table */
                    //         break;
                    // }
                }
            }

            private void GenerateOggHeader(BitOggstream os, bool[] modeBlockflag, int modeBits) {
                VorbisPacketHeader vhead = new VorbisPacketHeader(1);
                os.Write(vhead);
                
                BitUint version = new BitUint(32, 0);
                os.Write(version);
                
                BitUint srate = new BitUint(32, _sampleRate);
                os.Write(srate);

                BitUint bitrateMax = new BitUint(32, 0);
                os.Write(bitrateMax);

                BitUint bitrateNominal = new BitUint(32, _avgBytesPerSecond * 8);
                os.Write(bitrateNominal);

                BitUint bitrateMinimum= new BitUint(32, 0);
                os.Write(bitrateMinimum);

                BitUint blocksize0 = new BitUint(4, _blocksize0Pow);
                os.Write(blocksize0);

                BitUint blocksize1 = new BitUint(4, _blocksize1Pow);
                os.Write(blocksize1);

                BitUint framing = new BitUint(1, 1);
                os.Write(framing);
                
                os.FlushPage();
            }

            public void ConvertToOgg(Stream outputStream) {
                using (BinaryWriter writer = new BinaryWriter(outputStream)) {
                    BitOggstream oggstream = new BitOggstream(writer);
                    bool[] modeBlockflag = null;
                    int modeBits = 0;
                    bool prevBlockflag = false;
    
                    if (_headerTriadPresent){
                        throw new Exception("unsuppored");
                        // generate_ogg_header_with_triad(os);
                    }
                    else {
                        GenerateOggHeader(oggstream, modeBlockflag, modeBits);
                    }

                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WwiseBankHeader {
            public uint MagicNumber;
            public uint HeaderLength;
            public uint Version;
            public uint ID;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WwiseBankChunkHeader {
            public uint MagicNumber;
            public uint ChunkLength;

            public string Name => Encoding.UTF8.GetString(BitConverter.GetBytes(MagicNumber));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WwiseBankWemDef {
            public uint FileID;
            public uint DataOffset;
            public int FileLength;
        }

        public class WwiseBank {
            // public WwiseBankHeader Header;
            public WwiseBankWemDef[] WemDefs;
            public byte[][] WemData;

            public List<WwiseBankChunkHeader> Chunks;
            public Dictionary<WwiseBankChunkHeader, long> ChunkPositions;
            
            public WwiseBank(Stream stream) {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                    ChunkPositions = new Dictionary<WwiseBankChunkHeader, long>();
                    Chunks = new List<WwiseBankChunkHeader>();

                    while (reader.BaseStream.Position < reader.BaseStream.Length) {
                        WwiseBankChunkHeader chunk = reader.Read<WwiseBankChunkHeader>();
                        Chunks.Add(chunk);
                        ChunkPositions[chunk] = reader.BaseStream.Position;
                        reader.BaseStream.Position += chunk.ChunkLength;
                    }

                    WwiseBankChunkHeader dataHeader = Chunks.FirstOrDefault(x => x.Name == "DATA");
                    WwiseBankChunkHeader didxHeader = Chunks.FirstOrDefault(x => x.Name == "DIDX");
                    
                    if (dataHeader.MagicNumber == 0 || dataHeader.MagicNumber == 0) return;

                    reader.BaseStream.Position = ChunkPositions[didxHeader];
                    if (didxHeader.ChunkLength <= 0) return;
                    
                    WemDefs = new WwiseBankWemDef[didxHeader.ChunkLength / 12];
                    WemData = new byte[didxHeader.ChunkLength / 12][];
                    for (int i = 0; i < didxHeader.ChunkLength / 12; i++) {
                        WemDefs[i] = reader.Read<WwiseBankWemDef>();
                        long temp = reader.BaseStream.Position;

                        reader.BaseStream.Position = ChunkPositions[dataHeader];
                        WemData[i] = reader.ReadBytes(WemDefs[i].FileLength);

                        reader.BaseStream.Position = temp;
                    }
                }
            }

            public void WriteWems(string output) {
                if (WemDefs == null) return;
                for (int i = 0; i < WemDefs.Length; i++) {
                    WwiseBankWemDef wemDef = WemDefs[i];
                    byte[] data = WemData[i];
                    
                    using (Stream outputs = File.Open($"{output}{Path.DirectorySeparatorChar}{wemDef.FileID:X8}.wem", FileMode.OpenOrCreate, FileAccess.Write)) {
                        outputs.SetLength(0);
                        outputs.Write(data, 0, data.Length);
                    }
                }
            }
        }
    }

    
    // todo: this is closer than before but data is written wrong.
    // good place to look is that the "vorbis" string is wrong
    public class BitOggstream : IDisposable {
        private readonly BinaryWriter _os;

        private byte _bitBuffer;
        private uint _bitsStored;
        
        private enum SizeEnum {
            HeaderBytes = 27,
            MaxSegments = 255,
            SegmentSize = 255
        }

        private uint _payloadBytes;
        private bool _first;
        private bool _continued;

        private readonly byte[] _pageBuffer = new byte[(int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments +
                                              (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments];

        private uint _granule;
        private uint _seqno;

        public BitOggstream(BinaryWriter writer) {
            _os = writer;
            _bitBuffer = 0;
            _bitsStored = 0;
            _payloadBytes = 0;
            _first = true;
            _continued = false;
            _granule = 0;
            _seqno = 0;
        }

        public void PutBit(bool bit) {
            if (bit) {
                _bitBuffer |= (byte)(1 << (byte)_bitsStored);
            }

            _bitsStored++;
            if (_bitsStored == 8) {
                FlushBits();
            }
        }

        public void SetGranule(uint g) {
            _granule = g;
        }

        public void FlushBits() {
            if (_bitsStored == 0) return;
            if (_payloadBytes == (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments) {
                throw new Exception("ran out of space in an Ogg packet");
                // flush_page(true);
            }

            _pageBuffer[(int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + _payloadBytes] =
                _bitBuffer;
            _payloadBytes++;

            _bitsStored = 0;
            _bitBuffer = 0;
        }
        
        public static byte[] Int2Le(uint data)
        {
            byte[] b = new byte[4];
            b[0] = (byte)data;
            b[1] = (byte)((data >> 8) & 0xFF);
            b[2] = (byte)((data >> 16) & 0xFF);
            b[3] = (byte)((data >> 24) & 0xFF);
            return b;
        }

        private void Write32Le(IList<byte> bytes, int startIndex, uint val) {
            byte[] valBytes = Int2Le(val);
            for (int i = 0; i < 4; i++) {
                bytes[i + startIndex] = valBytes[i];
                // bytes[i + startIndex] = (byte) (val & 0xFF);
                // val >>= 8;
            }
        }
        
        private static readonly uint[] CRCLookup = {0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b, 0x1a864db2, 0x1e475005, 0x2608edb8, 0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61, 0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd, 0x4c11db70, 0x48d0c6c7, 0x4593e01e, 0x4152fda9, 0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75, 0x6a1936c8, 0x6ed82b7f, 0x639b0da6, 0x675a1011, 0x791d4014, 0x7ddc5da3, 0x709f7b7a, 0x745e66cd, 0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039, 0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5, 0xbe2b5b58, 0xbaea46ef, 0xb7a96036, 0xb3687d81, 0xad2f2d84, 0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d, 0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49, 0xc7361b4c, 0xc3f706fb, 0xceb42022, 0xca753d95, 0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1, 0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d, 0x34867077, 0x30476dc0, 0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072, 0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16, 0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca, 0x7897ab07, 0x7c56b6b0, 0x71159069, 0x75d48dde, 0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02, 0x5e9f46bf, 0x5a5e5b08, 0x571d7dd1, 0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba, 0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e, 0xbfa1b04b, 0xbb60adfc, 0xb6238b25, 0xb2e29692, 0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6, 0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a, 0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e, 0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2, 0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34, 0xdc3abded, 0xd8fba05a, 0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637, 0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb, 0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f, 0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53, 0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5, 0x3f9b762c, 0x3b5a6b9b, 0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff, 0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623, 0xf12f560e, 0xf5ee4bb9, 0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b, 0xd727bbb6, 0xd3e6a601, 0xdea580d8, 0xda649d6f, 0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3, 0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7, 0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b, 0x9b3660c6, 0x9ff77d71, 0x92b45ba8, 0x9675461f, 0x8832161a, 0x8cf30bad, 0x81b02d74, 0x857130c3, 0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640, 0x4e8ee645, 0x4a4ffbf2, 0x470cdd2b, 0x43cdc09c, 0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8, 0x68860bfd, 0x6c47164a, 0x61043093, 0x65c52d24, 0x119b4be9, 0x155a565e, 0x18197087, 0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec, 0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d, 0x2056cd3a, 0x2d15ebe3, 0x29d4f654, 0xc5a92679, 0xc1683bce, 0xcc2b1d17, 0xc8ea00a0, 0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c, 0xe3a1cbc1, 0xe760d676, 0xea23f0af, 0xeee2ed18, 0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4, 0x89b8fd09, 0x8d79e0be, 0x803ac667, 0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662, 0x933eb0bb, 0x97ffad0c, 0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668, 0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4};

        private static uint Checksum(byte[] data, int bytes) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            uint crcReg = 0;
            int i;

            for (i = 0; i < bytes; ++i)
                crcReg = (crcReg << 8) ^ CRCLookup[((crcReg >> 24) & 0xff) ^ data[i]];

            return crcReg;
        }


        public void FlushPage(bool nextContinued = false, bool last = false) {
            if (_payloadBytes != (int) SizeEnum.SegmentSize * (int) SizeEnum.MaxSegments) {
                FlushBits();
            }

            if (_payloadBytes != 0) {
                uint segments = (_payloadBytes + (int) SizeEnum.SegmentSize) /
                                (int) SizeEnum.SegmentSize; // intentionally round up
                if (segments == (int) SizeEnum.MaxSegments + 1) {
                    segments = (int) SizeEnum.MaxSegments; // at max eschews the final 0
                }

                // move payload back
                for (uint i = 0; i < _payloadBytes; i++) {
                    _pageBuffer[(int) SizeEnum.HeaderBytes + segments + i] =
                        _pageBuffer[(int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + i];
                }

                _pageBuffer[0] = (byte) 'O';
                _pageBuffer[1] = (byte) 'g';
                _pageBuffer[2] = (byte) 'g';
                _pageBuffer[3] = (byte) 'S';
                _pageBuffer[4] = 0; // stream_structure_version
                _pageBuffer[5] = (byte) ((_continued ? 1 : 0) | (_first ? 2 : 0) | (last ? 4 : 0)); // header_type_flag


                Write32Le(_pageBuffer, 6, _granule); // granule low bits
                Write32Le(_pageBuffer, 10, 0); // granule high bits
                if (_granule == 0xFFFFFFFF) {
                    Write32Le(_pageBuffer, 10, 0xFFFFFFFF);
                }
                Write32Le(_pageBuffer, 14, 1); // stream serial number
                Write32Le(_pageBuffer, 18, _seqno); // page sequence number
                Write32Le(_pageBuffer, 22, 0); // checksum (0 for now)
                _pageBuffer[26] = (byte) segments; // segment count

                // lacing values
                for (uint i = 0, bytesLeft = _payloadBytes; i < segments; i++) {
                    if (bytesLeft >= (int) SizeEnum.SegmentSize) {
                        bytesLeft -= (int) SizeEnum.SegmentSize;
                        _pageBuffer[27 + i] = (int) SizeEnum.SegmentSize;
                    } else {
                        _pageBuffer[27 + i] = (byte) bytesLeft;
                    }
                }

                // checksum
                Write32Le(_pageBuffer, 22, Checksum(_pageBuffer, (int)((int) SizeEnum.HeaderBytes + segments + _payloadBytes)));

                // output to ostream
                for (uint i = 0; i < (int) SizeEnum.HeaderBytes + segments + _payloadBytes; i++) {
                    _os.Write(_pageBuffer[i]);
                }

                _seqno++;
                _first = false;
                _continued = nextContinued;
                _payloadBytes = 0;
            }
        }
        
        public void Write(BitUint bui) {
            for (int i = 0; i < bui.BitSize; i++)
            {
                // put_bit((bui.Value & (1U << i)) != 0);
                PutBit((bui.Value & (1 << i)) != 0);
            }
        }
        
        public void Write(Sound.VorbisPacketHeader vph) {
            BitUint t = new BitUint(1, vph.Type);
            Write(t);

            for (uint i = 0; i < 6; i++)
            {
                BitUint c = new BitUint(1, (byte)vph.VorbisStr[i]);
                Write(c);
            }
        }

        public void Dispose() {
            FlushPage();
        }
    }

    public class BitUint {
        public uint Value;
        public readonly uint BitSize;

        // public Bit_uint(uint size) {
        //     BitSize = size;
        //     Value = 0;
        // }

        public BitUint(uint size, uint v) {
            BitSize = size;
            Value = v;
        }
    }
}