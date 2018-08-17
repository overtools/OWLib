using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static TankLib.Extensions;

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

        public class Packet {
            private readonly long _offset;
            private readonly ushort _size;
            private readonly uint _absoluteGranule;
            private readonly bool _noGranule;

            public Packet(BinaryReader reader, long offset, bool littleEndian, bool noGranule = false) {
                _noGranule = noGranule;
                _offset = offset;
                reader.BaseStream.Seek(_offset, SeekOrigin.Begin);
                if (littleEndian) {
                    _size = reader.ReadUInt16();
                    // _size = read_16_le(i);
                    if (!_noGranule) {
                        _absoluteGranule = reader.ReadUInt32();
                    }
                } else {
                    Debugger.Break();
                    _size = SwapBytes(reader.ReadUInt16());
                    if (!_noGranule) {
                        _absoluteGranule = SwapBytes(reader.ReadUInt32());
                    }
                }
            }

            public long HeaderSize() {
                return _noGranule ? 2 : 6;
            }

            public long Offset() {
                return _offset + HeaderSize();
            }

            public ushort Size() {
                return _size;
            }

            public uint Granule() {
                return _absoluteGranule;
            }

            public long NextOffset() {
                return _offset + HeaderSize() + _size;
            }
        }
        
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
        // ported from ww2ogg
        public class WwiseRIFFVorbis : IDisposable {
            private string _codebooksFile;
            private Stream _stream;
            private BinaryReader _reader;
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
            
            public WwiseRIFFVorbis(Stream stream, string codebooksFile) {
                _codebooksFile = codebooksFile;
                _stream = stream;
                _reader = new BinaryReader(stream, Encoding.Default, true);
                _fileSize = _reader.BaseStream.Length;

                _reader.BaseStream.Seek(0, SeekOrigin.Begin);

                #region check header

                _riffHead = _reader.ReadChars(4);
                if (new string(_riffHead) != "RIFX") {
                    if (new string(_riffHead) != "RIFF") {
                        throw new Exception("missing RIFF");
                    }

                    _littleEndian = true;
                }

                ushort Read16() => _littleEndian ? _reader.ReadUInt16() : SwapBytes(_reader.ReadUInt16());
                uint Read32() => _littleEndian ? _reader.ReadUInt32() : SwapBytes(_reader.ReadUInt32());

                _riffSize = Read32() + 8;
                if (_riffSize > _fileSize) throw new Exception("RIFF truncated");
                _waveHead = _reader.ReadChars(4);

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
                    _reader.BaseStream.Seek(_chunkOffset, SeekOrigin.Begin);
                    if (_chunkOffset + 8 > _riffSize) throw new Exception("chunk header truncated");

                    char[] chunkType = _reader.ReadChars(4);
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

                if (-1 != _vorbOffset && 0x28 != _fmtSize && 0x18 != _fmtSize && 0x12 != _fmtSize)
                    throw new Exception("bad fmt size");

                if (-1 == _vorbOffset && 0x42 == _fmtSize) {
                    // fake it out
                    _vorbOffset = _fmtOffset + 0x18;
                }

                _reader.BaseStream.Seek(_fmtOffset, SeekOrigin.Begin);
                if (0xFFFF != Read16()) throw new Exception("bad codec id");

                _channels = Read16();
                _sampleRate = Read32();
                _avgBytesPerSecond = Read32();
                _subtype = 0;
                _extUnk = 0;

                if (0U != Read16()) throw new Exception("bad block align");
                if (0U != Read16()) throw new Exception("expected 0 bps");
                if (_fmtSize - 0x12 != Read16()) throw new Exception("bad extra fmt length");

                if (_fmtSize - 0x12 >= 2) {
                    // read extra fmt
                    _extUnk = Read16();
                    if (_fmtSize - 0x12 >= 6) {
                        _subtype = Read32();
                    }
                }

                if (_fmtSize == 0x28) {
                    byte[] whoknowsbufCheck = {1, 0, 0, 0, 0, 0, 0x10, 0, 0x80, 0, 0, 0xAA, 0, 0x38, 0x9b, 0x71};
                    byte[] whoknowsbuf = _reader.ReadBytes(16);
                    if (!ArraysEqual(whoknowsbuf, whoknowsbufCheck))
                        throw new Exception("expected signature in extra fmt?");
                }

                #endregion

                #region read cue

                _cueCount = 0;
                if (-1 != _cueOffset) {
                    _reader.BaseStream.Seek(_cueOffset, SeekOrigin.Begin);

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
                    _reader.BaseStream.Seek(_smplOffset + 0x1C, SeekOrigin.Begin);
                    _loopCount = Read32();

                    if (1 != _loopCount) throw new Exception("expected one loop");

                    _reader.BaseStream.Seek(_smplOffset + 0x2c, SeekOrigin.Begin);
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
                        _reader.BaseStream.Seek(_vorbOffset + 0x00, SeekOrigin.Begin);
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

                        _reader.BaseStream.Seek(_vorbOffset + 0x4, SeekOrigin.Begin);
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
                        if (0x4A != modSignal && 0x4B != modSignal && 0x69 != modSignal && 0x70 != modSignal) {
                            _modPackets = true;
                        }

                        _reader.BaseStream.Seek(_vorbOffset + 0x10, SeekOrigin.Begin);
                        break;

                    default:
                        _reader.BaseStream.Seek(_vorbOffset + 0x18, SeekOrigin.Begin);
                        break;
                }

                _setupPacketOffset = Read32();
                _firstAudioPacketOffset = Read32();

                switch (_vorbSize) {
                    case 0:
                    case 0x2A:
                        _reader.BaseStream.Seek(_vorbOffset + 0x24, SeekOrigin.Begin);
                        break;

                    case 0x32:
                    case 0x34:
                        _reader.BaseStream.Seek(_vorbOffset + 0x2C, SeekOrigin.Begin);
                        break;
                }

                _headerTriadPresent = false;
                _oldPacketHeaders = false;
                _uid = 0;
                _blocksize0Pow = 0;
                _blocksize1Pow = 0;
                switch (_vorbSize) {
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
                        _blocksize0Pow = _reader.ReadByte();
                        _blocksize1Pow = _reader.ReadByte();
                        break;
                }

                #endregion

                // check/set loops now that we know total sample count
                if (0 != _loopCount) {
                    if (_loopEnd == 0) {
                        _loopEnd = _sampleCount;
                    } else {
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
            
            public static int Ilog(uint v) {
                int ret = 0;
                while (v != 0) {
                    ret++;
                    v >>= 1;
                }

                return ret;
            }

            private void GenerateOggHeader(BitOggstream os, out bool[] modeBlockflag, out int modeBits) {
                // Identification packet
                {
                    VorbisPacketHeader vhead = new VorbisPacketHeader(1);
                    os.Write(vhead);

                    BitUint version = new BitUint(32, 0);
                    os.Write(version);

                    BitUint ch = new BitUint(8, _channels);
                    os.Write(ch);

                    BitUint srate = new BitUint(32, _sampleRate);
                    os.Write(srate);

                    BitUint bitrateMax = new BitUint(32, 0);
                    os.Write(bitrateMax);

                    BitUint bitrateNominal = new BitUint(32, _avgBytesPerSecond * 8);
                    os.Write(bitrateNominal);

                    BitUint bitrateMinimum = new BitUint(32, 0);
                    os.Write(bitrateMinimum);

                    BitUint blocksize0 = new BitUint(4, _blocksize0Pow);
                    os.Write(blocksize0);

                    BitUint blocksize1 = new BitUint(4, _blocksize1Pow);
                    os.Write(blocksize1);

                    BitUint framing = new BitUint(1, 1);
                    os.Write(framing);

                    os.FlushPage();
                }

                // Comment packet
                {
                    
                    // erm, the ww2ogg adds 14 bytes extra here, and I don't know why
                    VorbisPacketHeader vhead = new VorbisPacketHeader(3);
                    os.Write(vhead);
                    
                    const string vendor = "Converted from Audiokinetic Wwise by DataTool";
                    //const string vendor = "converted from Audiokinetic Wwise by ww2ogg 0.24"; // I want the correct checksums for now
                    
                    BitUint vendorSize = new BitUint(32, (uint)vendor.Length);
                    os.Write(vendorSize);

                    foreach (char vendorChar in vendor) {
                        BitUint c = new BitUint(8, (byte)vendorChar);
                        os.Write(c);
                    }

                    if (_loopCount == 0) {
                        BitUint userCommentCount = new BitUint(32, 0);
                        os.Write(userCommentCount);
                    } else {
                        List<string> comments = new List<string> {
                            $"LoopStart={_loopStart}",
                            $"LoopEnd={_loopEnd}"
                        };
                        BitUint userCommentCount = new BitUint(32, (uint)comments.Count);
                        os.Write(userCommentCount);

                        foreach (string comment in comments) {
                            BitUint commentLength = new BitUint(32, (uint)comment.Length);
                            os.Write(commentLength);

                            foreach (char c in comment) {
                                BitUint charBit = new BitUint(8, (byte)c);
                                os.Write(charBit);
                            }
                        }
                    }
                    
                    
                    BitUint framing = new BitUint(1, 1);
                    os.Write(framing);
                    
                    os.FlushPage();
                }

                {
                    VorbisPacketHeader vhead = new VorbisPacketHeader(5);
                    os.Write(vhead);
                    
                    Packet setupPacket = new Packet(_reader, _dataOffset+_setupPacketOffset, _littleEndian, _noGranule);

                    _reader.BaseStream.Position = setupPacket.Offset();
                    if (setupPacket.Granule() != 0) throw new Exception("setup packet granule != 0");
                    
                    BitStream ss = new BitStream(_reader);
                    
                    BitUint codebookCountLess1 = new BitUint(8);
                    ss.Read(codebookCountLess1);

                    uint codebookCount = codebookCountLess1 + 1;
                    os.Write(codebookCountLess1);
                    
                    // if (inline codebooks) // not used here, always external
                    
                    CodebookLibrary library = new CodebookLibrary(_codebooksFile);

                    for (int i = 0; i < codebookCount; i++) {
                        BitUint codebookID = new BitUint(10);
                        ss.Read(codebookID);
                        try {
                            library.Rebuild(codebookID.AsInt(), os); // todo: build once and just reuse data.
                        } catch (CodebookLibrary.InvalidID) {
                            //         B         C         V
                            //    4    2    4    3    5    6
                            // 0100 0010 0100 0011 0101 0110
                            // \_______|____ ___|/
                            //              X
                            //            11 0100 0010

                            if (codebookID != 0x342) throw;
                            BitUint codebookIdentifier = new BitUint(14);
                            ss.Read(codebookIdentifier);

                            //         B         C         V
                            //    4    2    4    3    5    6
                            // 0100 0010 0100 0011 0101 0110
                            //           \_____|_ _|_______/
                            //                   X
                            //         01 0101 10 01 0000
                            if (codebookIdentifier == 0x159) {
                                // starts with BCV, probably --full-setup
                                throw new Exception( "invalid codebook id 0x342, try --full-setup");
                            }

                            // just an invalid codebook
                            throw;
                        }
                    }
                    
                    BitUint timeCountLess1 = new BitUint(6, 0);
                    os.Write(timeCountLess1);
                    BitUint dummyTimeValue = new BitUint(16, 0);
                    os.Write(dummyTimeValue);
                    
                    // if (_fullSetup) not used here, always false

                    {
                        // floor count
                        BitUint floorCountLess1 = new BitUint(6);
                        ss.Read(floorCountLess1);
                        uint floorCount = floorCountLess1 + 1;
                        os.Write(floorCountLess1);
                        
                        // rebuild floors
                        for (uint i = 0; i < floorCount; i++) {
                            BitUint floorType = new BitUint(16, 1);
                            os.Write(floorType);
                            
                            BitUint floor1Partitions = new BitUint(5);
                            ss.Read(floor1Partitions);
                            os.Write(floor1Partitions);
                            
                            
                            uint[] floor1PartitionClassList = new uint[floor1Partitions];

                            uint maximumClass = 0;
                            for (int j = 0; j < floor1Partitions; j++) {
                                BitUint floor1PartitionClass = new BitUint(4);
                                ss.Read(floor1PartitionClass);
                                os.Write(floor1PartitionClass);

                                floor1PartitionClassList[j] = floor1PartitionClass;

                                if (floor1PartitionClass > maximumClass) {
                                    maximumClass = floor1PartitionClass;
                                }
                            }
                            uint[] floor1ClassDimensionsList = new uint[maximumClass+1];

                            for (int j = 0; j <= maximumClass; j++) {
                                BitUint classDimensionsLess1 = new BitUint(3);
                                ss.Read(classDimensionsLess1);
                                os.Write(classDimensionsLess1);

                                floor1ClassDimensionsList[j] = classDimensionsLess1 + 1;
                                
                                BitUint classSubclasses = new BitUint(2);
                                ss.Read(classSubclasses);
                                os.Write(classSubclasses);

                                if (classSubclasses != 0) {
                                    BitUint masterBook = new BitUint(8);
                                    ss.Read(masterBook);
                                    os.Write(masterBook);
                                    
                                    if (masterBook >= codebookCount)
                                        throw new Exception("invalid floor1 masterbook");
                                }
                                
                                for (uint k = 0; k < 1U<<classSubclasses.AsInt(); k++) {
                                    BitUint subclassBookPlus1 = new BitUint(8);
                                    ss.Read(subclassBookPlus1);
                                    os.Write(subclassBookPlus1);

                                    int subclassBook = subclassBookPlus1.AsInt()-1;
                                    if (subclassBook >= 0 && subclassBook >= codebookCount)
                                        throw new Exception("invalid floor1 subclass book");
                                }
                            }
                            
                            BitUint floor1MultiplierLess1 = new BitUint(2);
                            ss.Read(floor1MultiplierLess1);
                            os.Write(floor1MultiplierLess1);
                                
                            BitUint rangebits = new BitUint(4);
                            ss.Read(rangebits);
                            os.Write(rangebits);
                                
                            for (uint j = 0; j < floor1Partitions; j++) {
                                uint currentClassNumber = floor1PartitionClassList[j];
                                for (uint k = 0; k < floor1ClassDimensionsList[currentClassNumber]; k++) {
                                    BitUint x = new BitUint(rangebits);
                                    ss.Read(x);
                                    os.Write(x);
                                }
                            }
                        }
                        
                        // residue count
                        BitUint residueCountLess1 = new BitUint(6);
                        ss.Read(residueCountLess1);
                        uint residueCount = residueCountLess1 + 1;
                        os.Write(residueCountLess1);

                        for (uint i = 0; i < residueCount; i++) {
                            BitUint residueType = new BitUint(2);
                            ss.Read(residueType);
                            os.Write(new BitUint(16, residueType));
                            
                            if (residueType > 2) throw new Exception("invalid residue type");
                            
                            BitUint residueBegin = new BitUint(24);
                            BitUint residueEnd = new BitUint(24);
                            BitUint residuePartitionSizeLess1 = new BitUint(24);
                            BitUint residueClassificationsLess1 = new BitUint(6);
                            BitUint residueClassbook = new BitUint(8);
                            
                            ss.Read(residueBegin);
                            ss.Read(residueEnd);
                            ss.Read(residuePartitionSizeLess1);
                            ss.Read(residueClassificationsLess1);
                            ss.Read(residueClassbook);
                            uint residueClassifications = residueClassificationsLess1 + 1;
                            os.Write(residueBegin);
                            os.Write(residueEnd);
                            os.Write(residuePartitionSizeLess1);
                            os.Write(residueClassificationsLess1);
                            os.Write(residueClassbook);
                            
                            if (residueClassbook >= codebookCount) throw new Exception("invalid residue classbook");

                            uint[] residueCascade = new uint[residueClassifications];
                            for (uint j = 0; j < residueClassifications; j++) {
                                BitUint highBits = new BitUint(5, 0);
                                BitUint lowBits = new BitUint(3);

                                ss.Read(lowBits);
                                os.Write(lowBits);
                                
                                BitUint bitFlag = new BitUint(1);
                                ss.Read(bitFlag);
                                os.Write(bitFlag);
                                if (bitFlag == 1) {
                                    ss.Read(highBits);
                                    os.Write(highBits);
                                }

                                residueCascade[j] = highBits * 8 + lowBits;
                            }

                            for (uint j = 0; j < residueClassifications; j++) {
                                for (int k = 0; k < 8; k++) {
                                    if ((residueCascade[j] & (1 << k)) != 0)
                                    {
                                        BitUint residueBook = new BitUint(8);
                                        ss.Read(residueBook);
                                        os.Write(residueBook);

                                        if (residueBook >= codebookCount) throw new Exception("invalid residue book");
                                    }
                                }
                            }
                        }
                        
                        BitUint mappingCountLess1 = new BitUint(6);
                        ss.Read(mappingCountLess1);
                        uint mappingCount = mappingCountLess1 + 1;
                        os.Write(mappingCountLess1);

                        for (uint i = 0; i < mappingCount; i++) {
                            // always mapping type 0, the only one
                            BitUint mappingType = new BitUint(16, 0);
                            os.Write(mappingType);
                            
                            BitUint submapsFlag = new BitUint(1);
                            ss.Read(submapsFlag);
                            os.Write(submapsFlag);

                            uint submaps = 1;
                            if (submapsFlag == 1) {
                                BitUint submapsLess1 = new BitUint(4);
                                ss.Read(submapsLess1);
                                submaps = submapsLess1 + 1;
                                os.Write(submapsLess1);
                            }
                            
                            BitUint squarePolarFlag = new BitUint(1);
                            ss.Read(squarePolarFlag);
                            os.Write(squarePolarFlag);

                            if (squarePolarFlag == 1) {
                                BitUint couplingStepsLess1 = new BitUint(8);
                                ss.Read(couplingStepsLess1);
                                uint couplingSteps = couplingStepsLess1 + 1;
                                os.Write(couplingStepsLess1);

                                for (uint j = 0; j < couplingSteps; j++) {
                                    BitUint magnitude = new BitUint((uint) Ilog((uint) (_channels - 1)));
                                    BitUint angle = new BitUint((uint) Ilog((uint) (_channels - 1)));
                                    
                                    ss.Read(magnitude);
                                    ss.Read(angle);
                                    
                                    os.Write(magnitude);
                                    os.Write(angle);
                                    
                                    if (angle == magnitude || magnitude >= _channels || angle >= _channels) throw new Exception("invalid coupling");
                                }
                            }
                            
                            // a rare reserved field not removed by Ak!
                            BitUint mappingReserved = new BitUint(2);
                            ss.Read(mappingReserved);
                            os.Write(mappingReserved);
                            if (0 != mappingReserved) throw new Exception("mapping reserved field nonzero");

                            if (submaps > 1) {
                                for (uint j = 0; j < _channels; j++) {
                                    BitUint mappingMux = new BitUint(4);
                                    ss.Read(mappingMux);
                                    os.Write(mappingMux);
                                    
                                    if (mappingMux >= submaps) throw new Exception("mapping_mux >= submaps");
                                }
                            }

                            for (uint j = 0; j < submaps; j++) {
                                BitUint timeConfig = new BitUint(8);
                                ss.Read(timeConfig);
                                os.Write(timeConfig);
                                
                                BitUint floorNumber = new BitUint(8);
                                ss.Read(floorNumber);
                                os.Write(floorNumber);
                                
                                if (floorNumber >= floorCount) throw new Exception("invalid floor mapping");
                                
                                BitUint residueNumber = new BitUint(8);
                                ss.Read(residueNumber);
                                os.Write(residueNumber);
                                
                                if (residueNumber >= residueCount) throw new Exception("invalid residue mapping");
                            }
                        }
                        
                        // mode count
                        BitUint modeCountLess1 = new BitUint(6);
                        ss.Read(modeCountLess1);
                        uint modeCount = modeCountLess1 + 1;
                        os.Write(modeCountLess1);
                        
                        modeBlockflag = new bool[modeCount];
                        modeBits = Ilog(modeCount - 1);

                        for (uint i = 0; i < modeCount; i++) {
                            BitUint blockFlag = new BitUint(1);
                            ss.Read(blockFlag);
                            os.Write(blockFlag);

                            modeBlockflag[i] = blockFlag != 0;
                            
                            // only 0 valid for windowtype and transformtype
                            BitUint windowType = new BitUint(16, 0);
                            BitUint transformType = new BitUint(16, 0);
                            os.Write(windowType);
                            os.Write(transformType);
                            
                            BitUint mapping = new BitUint(8);
                            ss.Read(mapping);
                            os.Write(mapping);
                            if (mapping >= mappingCount) throw new Exception("invalid mode mapping");
                        }
                    }
                    
                    BitUint framing = new BitUint(1, 1);
                    os.Write(framing);
                    
                    os.FlushPage();
                
                    if ((ss.TotalBitsRead+7)/8 != setupPacket.Size()) throw new Exception("didn't read exactly setup packet");

                    if (setupPacket.NextOffset() != _dataOffset + _firstAudioPacketOffset) throw new Exception("first audio packet doesn't follow setup packet");
                }
            }

            public void ConvertToOgg(Stream outputStream) {
                using (BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true)) {  // leave open
                    outputStream.SetLength(0);
                    BitOggstream os = new BitOggstream(writer);
                    
    
                    bool[] modeBlockflag;
                    int modeBits;
                    bool prevBlockflag = false;
                    if (_headerTriadPresent){
                        throw new Exception("unsuppored");
                        // generate_ogg_header_with_triad(os);
                    }
                    else {
                        GenerateOggHeader(os, out modeBlockflag, out modeBits);
                    }

                    // audio pages
                    {
                        long offset = _dataOffset + _firstAudioPacketOffset;
                        
                        while (offset < _dataOffset + _dataSize) {
                            uint size;
                            uint granule;
                            long packetHeaderSize;
                            long packetPayloadOffset;
                            long nextOffset;

                            if (_oldPacketHeaders) {
                                throw new Exception("unsupported (if this happens please message the devs)");
                            } else {
                                Packet audioPacket = new Packet(_reader, offset, _littleEndian, _noGranule);
                                packetHeaderSize = audioPacket.HeaderSize();
                                size = audioPacket.Size();
                                packetPayloadOffset = audioPacket.Offset();
                                granule = audioPacket.Granule();
                                nextOffset = audioPacket.NextOffset();
                            }
                            
                            if (offset + packetHeaderSize > _dataOffset + _dataSize) {
                                throw new Exception("page header truncated");
                            }

                            offset = packetPayloadOffset;

                            _reader.BaseStream.Position = packetPayloadOffset;

                            if (granule == 0xFFFFFFFF) {
                                os.SetGranule(1);
                            } else {
                                os.SetGranule(granule);
                            }

                            if (_modPackets) {
                                if (modeBlockflag == null) {
                                    throw new Exception("didn't load blockflag");
                                }
                                
                                BitUint packetType = new BitUint(1, 0);
                                os.Write(packetType);

                                BitUint modeNumberP;
                                BitUint remainderP;

                                {
                                    // collect mode number from first byte
                                    BitStream ss = new BitStream(_reader);
                                    
                                    // IN/OUT: N bit mode number (max 6 bits)
                                    modeNumberP = new BitUint((uint)modeBits);
                                    ss.Read(modeNumberP);
                                    os.Write(modeNumberP);
                                    
                                    // IN: remaining bits of first (input) byte
                                    remainderP = new BitUint((uint)(8-modeBits));
                                    ss.Read(remainderP);
                                }

                                if (modeBlockflag[modeNumberP]) {
                                    // long window, peek at next frame
                                    _reader.BaseStream.Position = nextOffset;
                                    bool nextBlockflag = false;
                                    if (nextOffset + packetHeaderSize <= _dataOffset + _dataSize) {
                                        // mod_packets always goes with 6-byte headers
                                        Packet audioPacket = new Packet(_reader, nextOffset, _littleEndian, _noGranule);
                                        uint nextPacketSize = audioPacket.Size();

                                        if (nextPacketSize > 0) {
                                            _reader.BaseStream.Position = audioPacket.Offset();
                                            
                                            BitStream ss = new BitStream(_reader);
                                            BitUint nextModeNumber = new BitUint((uint)modeBits);
                                            ss.Read(nextModeNumber);

                                            nextBlockflag = modeBlockflag[nextModeNumber];
                                        }
                                    }
                                    
                                    BitUint prevWindowType = new BitUint(1, (uint)(prevBlockflag ? 1 : 0));
                                    os.Write(prevWindowType);
                                    
                                    BitUint nextWindowType = new BitUint(1, (uint)(nextBlockflag ? 1 : 0));
                                    os.Write(nextWindowType);

                                    _reader.BaseStream.Position = offset + 1;
                                }

                                prevBlockflag = modeBlockflag[modeNumberP];
                                
                                os.Write(remainderP);
                            } else {
                                // nothing unusual for first byte
                                int v = _reader.ReadByte();
                                if (v < 0) {
                                    throw new Exception("file truncated");
                                }
                                BitUint c = new BitUint(8, (uint)v);
                                os.Write(c);
                            }
                            
                            // remainder of packet
                            for (uint i = 1; i < size; i++) {
                                int v = _reader.ReadByte();
                                if (v < 0)
                                {
                                    throw new Exception("file truncated");
                                }
                                BitUint c = new BitUint(8, (uint)v);
                                os.Write(c);
                            }

                            offset = nextOffset;
                            os.FlushPage(false, offset == _dataOffset+_dataSize);
                        }

                        if (offset > _dataOffset + _dataSize) {
                            throw new Exception("page truncated");
                        }
                    }
                }
            }

            public void Dispose() {
                _stream?.Dispose();
                _reader?.Dispose();
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

        [AttributeUsage(AttributeTargets.Class)]
        public class BankObjectAttribute : Attribute {
            public byte Type;
            
            public BankObjectAttribute(byte type) {
                Type = type;
            }
        }

        public interface IBankObject {
            void Read(BinaryReader reader);
        }

        [BankObject(3)]
        public class BankObjectEventAction : IBankObject {
            public enum EventActionScope : byte {
                GameObjectSwitchOrTrigger = 1, // Switch or Trigger
                Global = 2,
                GameObjectReference = 3, // see referenced object id
                GameObjectState = 4,
                All = 5,
                AllExceptReference = 5,  // see referenced object id
            }

            public enum EventActionType : byte {
                Stop = 0x1,
                Pause = 0x2,
                Resume = 0x3,
                Play = 0x4,
                Trigger = 0x5,
                Mute = 0x6,
                UnMute = 0x7,
                SetVoicePitch = 0x8,
                ResetVoicePitch = 0x9,
                SetVoiceVolume = 0xA,
                ResetVoiceVolume = 0xB,
                SetBusVolume = 0xC,
                ResetBusVolume = 0xD,
                SetVoiceLowpassFilter = 0xE,
                ResetVoiceLowpassFilter = 0xF,
                EnableState = 0x10,
                DisableState = 0x11,
                SetState = 0x12,
                SetGameParameter = 0x13,
                ResetGameParameter = 0x14,
                SetSwitch = 0x19,
                EnableBypassOrDisableBypass = 0x1A,
                ResetBypassEffect = 0x1B,
                Break = 0x1C,
                Seek = 0x1E
            }

            public enum EventActionParameterType : byte {
                Delay = 0xE, // Delay, given as uint32 in milliseconds
                Play = 0xF,  // Play: Fade in time, given as uint32 in milliseconds
                // may not be fade in time, but start time. (wouldn't that be a delay though?)
                Probability = 0x10  // Probability, given as float
            }

            public EventActionScope Scope;
            public EventActionType Type;
            public uint ReferenceObjectID;

            public List<KeyValuePair<EventActionParameterType, object>> Parameters;
            
            public void Read(BinaryReader reader) {
                Scope = (EventActionScope) reader.ReadByte();
                Type = (EventActionType) reader.ReadByte();
                ReferenceObjectID = reader.ReadUInt32();
                byte zero = reader.ReadByte();
                byte parameterCount = reader.ReadByte();
                Parameters = new List<KeyValuePair<EventActionParameterType, object>>(parameterCount);
                EventActionParameterType[] tempTypes = new EventActionParameterType[parameterCount];
                for (int i = parameterCount - 1; i >= 0; i--) {
                    EventActionParameterType parameterType = (EventActionParameterType)reader.ReadByte();
                    tempTypes[i] = parameterType;
                }

                foreach (EventActionParameterType parameterType in tempTypes) {
                    object val;
                    switch (parameterType) {
                        case EventActionParameterType.Probability:
                            val = reader.ReadSingle();
                            break;
                        case EventActionParameterType.Delay:
                        case EventActionParameterType.Play:
                            val = reader.ReadUInt32();
                            break;
                        default:
                            Debugger.Log(0, "[DataTool.Convertlogic.Sound]", $"Unhandled EventActionParameterTyp: {parameterType}\r\n");
                            // throw new ArgumentOutOfRangeException();
                            continue;
                    }
                    Parameters.Add(new KeyValuePair<EventActionParameterType, object>(parameterType, val));
                }
            }
        }

        [BankObject(2)]
        public class BankObjectSoundSFX : IBankObject {
            public enum SoundLocation : byte {
                Embedded = 0,
                Streamed = 1,
                StreamedZeroLatency = 2
            }

            public uint SoundID;
            public SoundLocation Location;
            
            public void Read(BinaryReader reader) {
                // using a different structure to the wiki :thinking:
                Location = (SoundLocation)reader.ReadByte();

                ushort u1 = reader.ReadUInt16();
                ushort u2 = reader.ReadUInt16();

                SoundID = reader.ReadUInt32();
            }
        }

        [BankObject(1)]
        public class BankObjectSettings : IBankObject {
            public enum SettingType : byte {
                VoiceVolume = 1,
                VoiceLowPassFilter = 3
            }

            public List<KeyValuePair<SettingType, float>> Settings;
            
            public void Read(BinaryReader reader) {
                byte numSettings = reader.ReadByte();
                
                SettingType[] types = new SettingType[numSettings];
                for (int i = 0; i < numSettings; i++) {
                    SettingType type = (SettingType)reader.ReadByte();
                    types[i] = type;
                }
                
                Settings = new List<KeyValuePair<SettingType, float>>();

                foreach (SettingType settingType in types) {
                    float value = reader.ReadSingle();
                    Settings.Add(new KeyValuePair<SettingType, float>(settingType, value));
                }
            }
        }

        [BankObject(4)]
        public class BankObjectEvent : IBankObject {
            public uint[] Actions;
            
            public void Read(BinaryReader reader) {
                byte numActions = reader.ReadByte();

                Actions = new uint[numActions];
                for (int i = 0; i < numActions; i++) {
                    Actions[i] = reader.ReadUInt32();
                }
            }
        }

        public class BankSoundStructure : IBankObject {  // might as well use interface
            public enum AdditionalParameterType : byte {
                VoiceVolume = 0x0,  // General Settings: Voice: Volume, float
                VoicePitch = 0x2,  // General Settings: Voice: Pitch, float
                VoiceLowPassFilter = 0x3,  // General Settings: Voice: Low-pass filter, float
                PlaybackPriority = 0x5,  // Advanced Settings: Playback Priority: Priority, float
                PlaybackPriortyOffset = 0x6,  // Advanced Settings: Playback Priority: Offset priority by ... at max distance, float
                Loop = 0x7,  // whether to Loop, given as uint32 = number of loops, or infinite if the value is 0
                MotionVolumeOffset = 0x8,  // Motion: Audio to Motion Settings: Motion Volume Offset, float
                PositioningPannerX = 0xB,  // Positioning: 2D: Panner X-coordinate, float
                PositioningPannerX2 = 0xC,  // todo: erm, wiki?
                PositioningCenter = 0xD,  // Positioning: Center %, float
                Bus0Volume = 0x12,  // General Settings: User-Defined Auxiliary Sends: Bus #0 Volume, float
                Bus1Volume = 0x13,  // General Settings: User-Defined Auxiliary Sends: Bus #1 Volume, float
                Bus2Volume = 0x14,  // General Settings: User-Defined Auxiliary Sends: Bus #2 Volume, float
                Bus3Volume = 0x15,  // General Settings: User-Defined Auxiliary Sends: Bus #3 Volume, float
                AuxiliarySendsVolume = 0x16,  // General Settings: Game-Defined Auxiliary Sends: Volume, float
                OutputBusVolume = 0x17,  // General Settings: Output Bus: Volume, float
                OutputBusLowPassFilter = 0x18  // General Settings: Output Bus: Low-pass filter, float
            }
            
            public void Read(BinaryReader reader) {
                // untested but you can try if you are brave
#if I_CAN_SIMPLY_SNAP_MY_FINGERS
                bool overrideParentSettingsEffect = reader.ReadBoolean();  // whether to override parent settings for Effects section
                byte numEffects = reader.ReadByte();

                if (numEffects > 0) {
                    byte mask = reader.ReadByte(); // bit mask specifying which effects should be bypassed (see wiki)

                    for (int i = 0; i < numEffects; i++) {
                        byte effectIndex = reader.ReadByte();  // effect index (00 to 03)
                        uint effectID = reader.ReadUInt32();  // id of Effect object
                        short zero = reader.ReadInt16();  // two zero bytes
                        Debug.Assert(zero == 0);
                    }
                }
                
                uint outputBus = reader.ReadUInt32();
                uint parentObject = reader.ReadUInt32();
                bool overrideParentSettingsPlaybackPriority = reader.ReadBoolean();  // whether to override parent settings for Playback Priority section
                bool offsetPriorityBy = reader.ReadBoolean();  // whether the "Offset priority by ... at max distance" setting is activated
                
                byte numAdditionalParameters = reader.ReadByte();
                AdditionalParameterType[] parameterTypes = new AdditionalParameterType[numAdditionalParameters];
                for (int i = 0; i < numAdditionalParameters; i++) {
                    AdditionalParameterType type = (AdditionalParameterType)reader.ReadByte();
                    parameterTypes[i] = type;
                }
                
                // byte zero2 = reader.ReadByte();
                // Debug.Assert(zero2 == 0);
#else
                throw new NotImplementedException();
#endif
            }
        }
        
        public class BankNotReadyException : Exception {}

        public class BankObjectTooMuchReadException : Exception {
            public BankObjectTooMuchReadException(string message) : base(message) {}
        }

        public class WwiseBank {
            public WwiseBankWemDef[] WemDefs { get; }
            public byte[][] WemData { get; }

            public Dictionary<uint, IBankObject> Objects { get; }
            public List<WwiseBankChunkHeader> Chunks { get; }
            public Dictionary<WwiseBankChunkHeader, long> ChunkPositions { get; }

            public static bool Ready { get; private set; }
            public static Dictionary<byte, Type> Types { get; private set; }
            
            public static void GetReady() {
                Types = new Dictionary<byte, Type>();
                
                Assembly assembly = typeof(WwiseBank).Assembly;
                Type baseType = typeof(IBankObject);
                List<Type> types = assembly.GetTypes().Where(type => type != baseType && baseType.IsAssignableFrom(type)).ToList();

                foreach (Type type in types) {
                    BankObjectAttribute bankObjectAttribute = type.GetCustomAttribute<BankObjectAttribute>();
                    if (bankObjectAttribute == null) continue;
                    Types[bankObjectAttribute.Type] = type;
                }
                Ready = true;
            }
            
            public WwiseBank(Stream stream) {
                if (!Ready) throw new BankNotReadyException();
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                    // reference: http://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)
                    
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

                    if (dataHeader.MagicNumber != 0 && dataHeader.MagicNumber != 0) {
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

                    WwiseBankChunkHeader hircHeader = Chunks.FirstOrDefault(x => x.Name == "HIRC");

                    if (hircHeader.MagicNumber != 0) {
                        reader.BaseStream.Position = ChunkPositions[hircHeader];
                        uint objectCount = reader.ReadUInt32();
                        Objects = new Dictionary<uint, IBankObject>((int)objectCount);
                        for (int o = 0; o < objectCount; o++) {
                            byte objectType = reader.ReadByte();
                            uint objectLength = reader.ReadUInt32();

                            long beforeObject = reader.BaseStream.Position;

                            uint objectID = reader.ReadUInt32();

                            if (Types.ContainsKey(objectType)) {
                                if (!(Activator.CreateInstance(Types[objectType]) is IBankObject bankObject)) continue;
                                bankObject.Read(reader);
                                Objects[objectID] = bankObject;
                            } else {
                                Debugger.Log(0, "[DataTool.Convertlogic.Sound]", $"Unhandled Bank object type: {objectType}\r\n");
                                Objects[objectID] = null;
                            }

                            long newPos = beforeObject + objectLength;
                            if (newPos < reader.BaseStream.Position) throw new BankObjectTooMuchReadException($"Bank object of type {objectType} read too much data");
                            reader.BaseStream.Position = newPos;
                        }
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

            public IEnumerable<T> ObjectsOfType<T>() {
                foreach (KeyValuePair<uint,IBankObject> bankObject in Objects) {
                    if (bankObject.Value != null && bankObject.Value.GetType() == typeof(T)) {
                        yield return (T)bankObject.Value;
                    }
                }
            }
        }
    }

    
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
                // bytes[i + startIndex] = 255;
                // bytes[i + startIndex] = (byte) (val & 0xFF);
                // val >>= 8;
            }
        }

        private static readonly uint[] CRCLookup = {
            0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b, 0x1a864db2, 0x1e475005, 0x2608edb8,
            0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61, 0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd, 0x4c11db70, 0x48d0c6c7,
            0x4593e01e, 0x4152fda9, 0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75, 0x6a1936c8, 0x6ed82b7f, 0x639b0da6,
            0x675a1011, 0x791d4014, 0x7ddc5da3, 0x709f7b7a, 0x745e66cd, 0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039,
            0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5, 0xbe2b5b58, 0xbaea46ef, 0xb7a96036, 0xb3687d81, 0xad2f2d84,
            0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d, 0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49, 0xc7361b4c, 0xc3f706fb,
            0xceb42022, 0xca753d95, 0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1, 0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a,
            0xec7dd02d, 0x34867077, 0x30476dc0, 0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072,
            0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16, 0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca, 0x7897ab07,
            0x7c56b6b0, 0x71159069, 0x75d48dde, 0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02, 0x5e9f46bf, 0x5a5e5b08,
            0x571d7dd1, 0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba, 0xaca5c697, 0xa864db20, 0xa527fdf9,
            0xa1e6e04e, 0xbfa1b04b, 0xbb60adfc, 0xb6238b25, 0xb2e29692, 0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6,
            0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a, 0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e, 0xf3b06b3b,
            0xf771768c, 0xfa325055, 0xfef34de2, 0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34,
            0xdc3abded, 0xd8fba05a, 0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637, 0x7a089632, 0x7ec98b85, 0x738aad5c,
            0x774bb0eb, 0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f, 0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53,
            0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5, 0x3f9b762c, 0x3b5a6b9b, 0x0315d626,
            0x07d4cb91, 0x0a97ed48, 0x0e56f0ff, 0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623, 0xf12f560e, 0xf5ee4bb9,
            0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b, 0xd727bbb6, 0xd3e6a601, 0xdea580d8,
            0xda649d6f, 0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3, 0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7,
            0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b, 0x9b3660c6, 0x9ff77d71, 0x92b45ba8, 0x9675461f, 0x8832161a,
            0x8cf30bad, 0x81b02d74, 0x857130c3, 0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640, 0x4e8ee645, 0x4a4ffbf2,
            0x470cdd2b, 0x43cdc09c, 0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8, 0x68860bfd, 0x6c47164a, 0x61043093,
            0x65c52d24, 0x119b4be9, 0x155a565e, 0x18197087, 0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
            0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d, 0x2056cd3a, 0x2d15ebe3, 0x29d4f654, 0xc5a92679,
            0xc1683bce, 0xcc2b1d17, 0xc8ea00a0, 0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c, 0xe3a1cbc1, 0xe760d676,
            0xea23f0af, 0xeee2ed18, 0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4, 0x89b8fd09, 0x8d79e0be, 0x803ac667,
            0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662, 0x933eb0bb, 0x97ffad0c, 0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668,
            0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4
        };

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
                    // if ((int) SizeEnum.HeaderBytes + (int) SizeEnum.MaxSegments + i == 4155) {
                    //     uint test = (int) SizeEnum.HeaderBytes + segments + i;
                    //     Debugger.Break();
                    // }
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
                PutBit((bui.Value & (1U << i)) != 0);
            }
        }
        
        public void Write(Sound.VorbisPacketHeader vph) {
            BitUint t = new BitUint(8, vph.Type);
            Write(t);

            for (uint i = 0; i < 6; i++)
            {
                BitUint c = new BitUint(8, (byte)vph.VorbisStr[i]);
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
            return (int)Value;
        }
    }

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
            BitsLeft --;
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

    public class CodebookLibrary {
        public string File;

        public byte[] CodebookData;
        public long[] CodebookOffsets;

        private readonly long _codebookCount;
        
        public CodebookLibrary(string file) {
            File = file;

            using (Stream codebookStream = System.IO.File.OpenRead(file)) {
                using (BinaryReader reader = new BinaryReader(codebookStream)) {
                    long fileSize = codebookStream.Length;

                    codebookStream.Seek(fileSize - 4, SeekOrigin.Begin);
                    long offsetOffset = reader.ReadInt32();

                    _codebookCount = (fileSize - offsetOffset) / 4;
                    
                    CodebookData = new byte[offsetOffset];
                    CodebookOffsets = new long[_codebookCount];

                    codebookStream.Position = 0;
                    for (int i = 0; i < offsetOffset; i++) {
                        CodebookData[i] = reader.ReadByte();
                    }

                    for (int i = 0; i < _codebookCount; i++) {
                        CodebookOffsets[i] = reader.ReadInt32();
                    }
                }
            }
        }

        public void Rebuild(int codebookID, BitOggstream os) {
            long? cbIndexStart = GetCodebook(codebookID);
            ulong cbSize;

            {
                long signedCbSize = GetCodebookSize(codebookID);
                if (cbIndexStart == null || -1 == signedCbSize) throw new InvalidID();
                cbSize = (ulong) signedCbSize;
            }

            long cbStartIndex = (long)cbIndexStart;
            long unsignedSize = (long) cbSize;
            
            Stream codebookStream = new MemoryStream();
            for (long i = cbStartIndex; i < unsignedSize+cbStartIndex; i++) {
                codebookStream.WriteByte(CodebookData[i]);
            }
            
            BinaryReader reader = new BinaryReader(codebookStream);
            BitStream bitStream = new BitStream(reader);
            reader.BaseStream.Position = 0;
            
            
            // todo: the rest of the stuff
            Rebuild(bitStream, cbSize, os);
        }

        public void Rebuild(BitStream bis, ulong cbSize, BitOggstream bos) {
            /* IN: 4 bit dimensions, 14 bit entry count */
            BitUint dimensions = new BitUint(4);
            BitUint entries = new BitUint(14);
            bis.Read(dimensions);
            bis.Read(entries);
            
            /* OUT: 24 bit identifier, 16 bit dimensions, 24 bit entry count */
            bos.Write(new BitUint(24, 0x564342));
            bos.Write(new BitUint(16, dimensions));
            bos.Write(new BitUint(24, entries));
            
            /* IN/OUT: 1 bit ordered flag */
            BitUint ordered = new BitUint(1);
            bis.Read(ordered);
            bos.Write(ordered);

            if (ordered == 1) {
                /* IN/OUT: 5 bit initial length */
                BitUint initialLength = new BitUint(5);
                bis.Read(initialLength);
                bos.Write(initialLength);

                int currentEntry = 0;
                while (currentEntry < entries) {
                    /* IN/OUT: ilog(entries-current_entry) bit count w/ given length */
                    BitUint number = new BitUint((uint)Sound.WwiseRIFFVorbis.Ilog((uint)(entries-currentEntry)));
                    bis.Read(number);
                    bos.Write(number);
                    currentEntry = (int)(currentEntry+number); 
                }
                if (currentEntry > entries) throw new Exception("current_entry out of range");
            } else {
                /* IN: 3 bit codeword length length, 1 bit sparse flag */
                BitUint codewordLengthLength = new BitUint(3);
                BitUint sparse = new BitUint(1);
                bis.Read(codewordLengthLength);
                bis.Read(sparse);
                
                if (0 == codewordLengthLength || 5 < codewordLengthLength)
                {
                    throw new Exception("nonsense codeword length");
                }
                
                /* OUT: 1 bit sparse flag */
                bos.Write(sparse);
                //if (sparse)
                //{
                //    cout << "Sparse" << endl;
                //}
                //else
                //{
                //    cout << "Nonsparse" << endl;
                //}
                for (int i = 0; i < entries; i++)
                {
                    bool presentBool = true;

                    if (sparse == 1)
                    {
                        /* IN/OUT 1 bit sparse presence flag */
                        BitUint present = new BitUint(1);
                        bis.Read(present);
                        bos.Write(present);

                        presentBool = 0 != present;
                    }

                    if (presentBool) {
                        /* IN: n bit codeword length-1 */
                        BitUint codewordLength = new BitUint(codewordLengthLength);
                        bis.Read(codewordLength);

                        /* OUT: 5 bit codeword length-1 */
                        bos.Write(new BitUint(5, codewordLength));
                    }
                }
            } // done with lengths
            
            // lookup table
            
            /* IN: 1 bit lookup type */
            BitUint lookupType = new BitUint(1);
            bis.Read(lookupType);
            /* OUT: 4 bit lookup type */
            bos.Write(new BitUint(4, lookupType));
            
            if (lookupType == 0) {
                //cout << "no lookup table" << endl;
            } else if (lookupType == 1) {
                //cout << "lookup type 1" << endl;

                /* IN/OUT: 32 bit minimum length, 32 bit maximum length, 4 bit value length-1, 1 bit sequence flag */
                BitUint min = new BitUint(32);
                BitUint max = new BitUint(32);
                BitUint valueLength = new BitUint(4);
                BitUint sequenceFlag = new BitUint(1);
                bis.Read(min);
                bis.Read(max);
                bis.Read(valueLength);
                bis.Read(sequenceFlag);
                
                bos.Write(min);
                bos.Write(max);
                bos.Write(valueLength);
                bos.Write(sequenceFlag);

                uint quantvals = _bookMaptype1Quantvals(entries, dimensions);
                for (uint i = 0; i < quantvals; i++)
                {
                    /* IN/OUT: n bit value */
                    BitUint val = new BitUint(valueLength+1);
                    bis.Read(val);
                    bos.Write(val);
                }
            }
            
            /* check that we used exactly all bytes */
            /* note: if all bits are used in the last byte there will be one extra 0 byte */
            
            if (0 != cbSize && bis.TotalBitsRead / 8 + 1 != (int)cbSize) {
                throw new Exception($"{cbSize}, {bis.TotalBitsRead / 8 + 1}");
            }
        }

        private uint _bookMaptype1Quantvals(uint entries, uint dimensions) {
            /* get us a starting hint, we'll polish it below */
            int bits = Sound.WwiseRIFFVorbis.Ilog(entries);
            int vals = (int) (entries >> (int) ((bits - 1) * (dimensions - 1) / dimensions));
            while (true) {
                uint acc = 1;
                uint acc1 = 1;
                uint i;
                for (i = 0; i < dimensions; i++) {
                    acc = (uint) (acc * vals);
                    acc1 = (uint) (acc * vals + 1);
                }

                if (acc <= entries && acc1 > entries) {
                    return (uint) vals;
                } else {
                    if (acc > entries) vals--;
                    else vals++;
                }
            }
        }

        public long? GetCodebook(int i) {
            if (i >= _codebookCount-1 || i < 0) return null;
            return CodebookOffsets[i];  // return the offset
            // CodebookData[CodebookOffsets[i]]
        }

        public long GetCodebookSize(int i) {
            if (i >= _codebookCount-1 || i < 0) return -1;
            return CodebookOffsets[i+1]-CodebookOffsets[i];
        }

        public class InvalidID : Exception {}
    }
}