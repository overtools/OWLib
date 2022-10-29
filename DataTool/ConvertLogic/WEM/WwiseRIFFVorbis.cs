using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using static DataTool.ConvertLogic.SoundUtils;

namespace DataTool.ConvertLogic.WEM {
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
                byte[] whoknowsbufCheck = { 1, 0, 0, 0, 0, 0, 0x10, 0, 0x80, 0, 0, 0xAA, 0, 0x38, 0x9b, 0x71 };
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

                if (_loopStart > _loopEnd) {
                    (_loopStart, _loopEnd) = (_loopEnd, _loopStart);
                }

                if (_loopStart > _sampleCount || _loopEnd > _sampleCount || _loopStart > _loopEnd) {
                    _loopStart = 0;
                    _loopEnd = 0;
                    _loopCount = 0;
                }
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

        private void GenerateOggHeader(BitOggStream os, out bool[] modeBlockflag, out int modeBits) {
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

                BitUint vendorSize = new BitUint(32, (uint) vendor.Length);
                os.Write(vendorSize);

                foreach (char vendorChar in vendor) {
                    BitUint c = new BitUint(8, (byte) vendorChar);
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

                    BitUint userCommentCount = new BitUint(32, (uint) comments.Count);
                    os.Write(userCommentCount);

                    foreach (string comment in comments) {
                        BitUint commentLength = new BitUint(32, (uint) comment.Length);
                        os.Write(commentLength);

                        foreach (char c in comment) {
                            BitUint charBit = new BitUint(8, (byte) c);
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

                Packet setupPacket = new Packet(_reader, _dataOffset + _setupPacketOffset, _littleEndian, _noGranule);

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
                            throw new Exception("invalid codebook id 0x342, try --full-setup");
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

                        uint[] floor1ClassDimensionsList = new uint[maximumClass + 1];

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

                            for (uint k = 0; k < 1U << classSubclasses.AsInt(); k++) {
                                BitUint subclassBookPlus1 = new BitUint(8);
                                ss.Read(subclassBookPlus1);
                                os.Write(subclassBookPlus1);

                                int subclassBook = subclassBookPlus1.AsInt() - 1;
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
                                if ((residueCascade[j] & (1 << k)) != 0) {
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

                if ((ss.TotalBitsRead + 7) / 8 != setupPacket.Size()) throw new Exception("didn't read exactly setup packet");

                if (setupPacket.NextOffset() != _dataOffset + _firstAudioPacketOffset) throw new Exception("first audio packet doesn't follow setup packet");
            }
        }

        public void ConvertToOgg(Stream outputStream) {
            using (BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true)) { // leave open
                outputStream.SetLength(0);
                BitOggStream os = new BitOggStream(writer);


                bool[] modeBlockflag;
                int modeBits;
                bool prevBlockflag = false;
                if (_headerTriadPresent) {
                    throw new Exception("unsuppored");
                    // generate_ogg_header_with_triad(os);
                } else {
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
                                modeNumberP = new BitUint((uint) modeBits);
                                ss.Read(modeNumberP);
                                os.Write(modeNumberP);

                                // IN: remaining bits of first (input) byte
                                remainderP = new BitUint((uint) (8 - modeBits));
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
                                        BitUint nextModeNumber = new BitUint((uint) modeBits);
                                        ss.Read(nextModeNumber);

                                        nextBlockflag = modeBlockflag[nextModeNumber];
                                    }
                                }

                                BitUint prevWindowType = new BitUint(1, (uint) (prevBlockflag ? 1 : 0));
                                os.Write(prevWindowType);

                                BitUint nextWindowType = new BitUint(1, (uint) (nextBlockflag ? 1 : 0));
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

                            BitUint c = new BitUint(8, (uint) v);
                            os.Write(c);
                        }

                        // remainder of packet
                        for (uint i = 1; i < size; i++) {
                            int v = _reader.ReadByte();
                            if (v < 0) {
                                throw new Exception("file truncated");
                            }

                            BitUint c = new BitUint(8, (uint) v);
                            os.Write(c);
                        }

                        offset = nextOffset;
                        os.FlushPage(false, offset == _dataOffset + _dataSize);
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
}