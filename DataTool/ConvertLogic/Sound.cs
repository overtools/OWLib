using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static OWLib.Extensions;
// using OggVorbisEncoder;

namespace DataTool.ConvertLogic {
    public static class Sound {
        /*private class BitReader {  // todo: is this useful?
            private int _bit;
            private byte _currentByte;
            private readonly Stream _stream;
            public BitReader(Stream stream) { _stream = stream; }

            public bool? ReadBit(bool bigEndian = false) {
                if (_bit == 8 ) 
                {

                    int r = _stream.ReadByte();
                    if (r== -1) return null;
                    _bit = 0; 
                    _currentByte = (byte)r;
                }
                bool value;
                if (!bigEndian)
                    value = (_currentByte & (1 << _bit)) > 0;
                else
                    value = (_currentByte & (1 << (7-_bit))) > 0;

                _bit++;
                return value;
            }
        }
        
        private class OggBitStream {  // todo: this or fix the other thing, probably this though
            private byte _bitBuffer;
            private uint bits_stored;
            public uint Granule;
            public OggBitStream(uint bitsStored, uint granule) {
                bits_stored = bitsStored;
                Granule = granule;
            }

            // public void PutBit(bool bit) {
            //     if (bit) { bit_buffer |= 1<<bits_stored; }
            //     bits_stored++;
            //     if (bits_stored == 8) {
            //         FlushBits();
            //     }
            // }

            public void FlushBits() {
                
            }
        }*/
        
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
        };

        class Vorbis_packet_header {
            private byte type;

            private static readonly char[] vorbis_str = {'v', 'o', 'r', 'b', 'i', 's'};

            public Vorbis_packet_header(byte type) {
                this.type = type;
            }
        }*/
        
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

            public Stream ConvertToOgg() {
                MemoryStream outputStream = new MemoryStream();

                return outputStream;

                // todo: this is just test stuff
                // todo: first packet is _dataOffset + _firstAudioPacketOffset;
                
                // you will need to make assumptions
               
                /*VorbisInfo info = VorbisInfo.InitVariableBitRate(_channels, (int)_sampleRate, 0.1f);
                
                int serial = new Random().Next();
                OggStream oggStream = new OggStream(serial);
                
                HeaderPacketBuilder headerBuilder = new HeaderPacketBuilder();

                Comments comments = new Comments();
                comments.AddTag("ARTIST", "TEST");

                OggPacket infoPacket = headerBuilder.BuildInfoPacket(info);
                OggPacket commentsPacket = headerBuilder.BuildCommentsPacket(comments);
                OggPacket booksPacket = headerBuilder.BuildBooksPacket(info);

                oggStream.PacketIn(infoPacket);
                oggStream.PacketIn(commentsPacket);
                oggStream.PacketIn(booksPacket);
                
                // Flush to force audio data onto its own page per the spec
                OggPage page;
                while (oggStream.PageOut(out page, true)) {
                    outputStream.Write(page.Header, 0, page.Header.Length);
                    outputStream.Write(page.Body, 0, page.Body.Length);
                }
                
                // =========================================================
                // BODY (Audio Data)
                // =========================================================
                ProcessingState processingState = ProcessingState.Create(info);

                float[][] buffer = new float[_channels][];
                buffer[0] = new float[_sampleCount];
                buffer[1] = new float[_sampleCount];

                byte[] readbuffer = new byte[_sampleCount * 4];

                long offset = _dataOffset + _firstAudioPacketOffset;

                using (BinaryReader reader = new BinaryReader(_stream, System.Text.Encoding.Default, true)) {   
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    while (offset < _dataOffset + _dataSize) {                      
                        Packet audioPacket = new Packet(reader, offset, _littleEndian, _noGranule);
                        
                        long packet_payload_offset = audioPacket.offset();
                        long packet_header_size = audioPacket.header_size();
                        long next_offset = audioPacket.next_offset();
                        uint size = audioPacket.size();
                        
                        if (offset + packet_header_size > _dataOffset + _dataSize) {
                            throw new Exception("page header truncated");
                        }
                        
                        offset = packet_payload_offset;
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                        if (_modPackets) {
                            
                        }

                        int v = reader.ReadByte(); // put this in the thing 
                        
                        for (uint i = 1; i < size; i++) {
                            int v2 = reader.ReadByte();
                            if (v < 0)
                            {
                                throw new Exception("file truncated");
                            }
                            //Bit_uint<8> c(v);
                            //os << c;
                        }
                        
                        offset = next_offset;
                        
  
                        // old packets not handled because i can't be bothered
                    }


                    while (!oggStream.Finished) {
                        int bytes = reader.Read(readbuffer, 0, readbuffer.Length);

                        if (bytes == 0) {
                            processingState.WriteEndOfStream();
                        } else {
                            int samples = bytes / 4;

                            for (int i = 0; i < samples; i++) {
                                // uninterleave samples
                                buffer[0][i] = (short) ((readbuffer[i * 4 + 1] << 8) | (0x00ff & readbuffer[i * 4])) /
                                               32768f;
                                buffer[1][i] =
                                    (short) ((readbuffer[i * 4 + 3] << 8) | (0x00ff & readbuffer[i * 4 + 2])) / 32768f;
                            }

                            processingState.WriteData(buffer, samples);
                        }

                        OggPacket packet;
                        while (!oggStream.Finished
                               && processingState.PacketOut(out packet)) {
                            oggStream.PacketIn(packet);

                            while (!oggStream.Finished && oggStream.PageOut(out page, false)) {
                                outputStream.Write(page.Header, 0, page.Header.Length);
                                outputStream.Write(page.Body, 0, page.Body.Length);
                            }
                        }
                    }
                }
                return outputStream;*/
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
}