using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace DataTool.ConvertLogic.WEM {
    // ported from vgmstream
    public sealed class WwiseRIFFOpus : IDisposable {
        public BinaryReader Reader;

        public struct WWiseHeader {
            /* chunks references */
            public long FmtOffset;
            public int FmtSize;
            public long DataOffset;
            public int DataSize;
            public long TableOffset;
            public int TableSize;
            public int TableCount;
            public ushort Codec;
            public int Channels;
            public int SampleRate;
            public int BlockSize;
            public int AvgBitrate;
            public int BitsPerSample;
            public byte ChannelType;
            public uint ChannelLayout;
            public int ExtraSize;
            public int NumSamples;
            public int Skip;
            public int CoupledCount;
            public int StreamCount;
            public byte[] ChannelMapping;
        }

        public WWiseHeader Header;

        private static byte[][] MappingMatrix = {
            new byte[] { 0 },
            new byte[] { 0, 1 },
            new byte[] { 0, 2, 1 },
            new byte[] { 0, 1, 2, 3 },
            new byte[] { 0, 4, 1, 2, 3 },
            new byte[] { 0, 4, 1, 2, 3, 5 },
            new byte[] { 0, 6, 1, 2, 3, 4, 5 },
            new byte[] { 0, 6, 1, 2, 3, 4, 5, 7 }
        };

        public ushort[] FrameTable;
        public long LogicalOffset = 0;
        public int PageSize = 0;
        public int SamplesDone = 0;

        public WwiseRIFFOpus(Stream stream) {
            Reader = new BinaryReader(stream, Encoding.UTF8, true);

            Header = new WWiseHeader();
            stream.Position = 0;
            if(Reader.ReadUInt32() != 0x46464952) { // "RIFF"
                throw new InvalidDataException("Not a RIFF file");
            }

            Reader.ReadUInt32(); // file size

            if(Reader.ReadUInt32() != 0x45564157) { // "WAVE"
                throw new InvalidDataException("Not a WAVE file");
            }

            while (stream.Position < stream.Length) {
                var type = BinaryPrimitives.ReverseEndianness(Reader.ReadUInt32());
                var size = Reader.ReadInt32();

                switch (type) {
                    case 0x666d7420: // "fmt "
                        Header.FmtOffset = stream.Position;
                        Header.FmtSize = size;
                        break;

                    case 0x64617461: // "data"
                        Header.DataOffset = stream.Position;
                        Header.DataSize = size;
                        break;

                    case 0x7365656B: // "seek"
                        Header.TableOffset = stream.Position;
                        Header.TableSize = size;
                        break;
                }

                stream.Position += size;
            }

            if (Header.FmtSize < 0x10) {
                throw new InvalidDataException("Invalid fmt size");
            }

            if (Header.DataSize == 0 || Header.TableSize == 0) {
                throw new InvalidDataException("Missing data or seek chunks");
            }

            stream.Position = Header.FmtOffset;
            Header.Codec = Reader.ReadUInt16();
            Header.Channels = Reader.ReadUInt16();
            Header.SampleRate = Reader.ReadInt32();
            Header.AvgBitrate = Reader.ReadInt32();
            Header.BlockSize = Reader.ReadUInt16();
            Header.BitsPerSample = Reader.ReadUInt16();
            Header.ExtraSize = Reader.ReadUInt16();
            if (Header.ExtraSize >= 0x6) {
                Reader.BaseStream.Position = Header.FmtOffset + 0x14;
                Header.ChannelLayout = Reader.ReadUInt32();
                if ((Header.ChannelLayout & 0xFF) == Header.Channels) {
                    Header.ChannelType = (byte)((Header.ChannelLayout >> 8) & 0x0F);
                    Header.ChannelLayout >>= 12;
                }
            }

            if (Header.Codec != 0x3041) {
                throw new InvalidDataException("Not a WWise Opus file");
            }

            if (Header.BlockSize != 0 && Header.BitsPerSample != 0) {
                throw new InvalidDataException("Invalid block size or bits per sample");
            }

            if (Header.Channels > 255) {
                throw new InvalidDataException("Too many channels");
            }

            stream.Position = Header.FmtOffset + 0x18;
            Header.NumSamples = Reader.ReadInt32();
            Header.TableCount = Reader.ReadInt32();
            Header.Skip = Reader.ReadInt16();

            var codecVersion = Reader.ReadByte();
            var mapping = Reader.ReadByte();
            if (mapping == 1 && Header.Channels > 8) {
                throw new InvalidDataException("Too many channels for remapping");
            }

            if (codecVersion != 1) {
                throw new InvalidDataException("Invalid codec version");
            }

            if (mapping > 0 && Header.ChannelType == 1) {
                Header.CoupledCount = (WAVEChannelMask) Header.ChannelLayout switch {
                    WAVEChannelMask.STEREO => 1,
                    WAVEChannelMask.TWOPOINT1 => 1,
                    WAVEChannelMask.QUAD_side => 2,
                    WAVEChannelMask.FIVEPOINT1 => 2,
                    WAVEChannelMask.SEVENPOINT1 => 2,
                    _ => 0,
                };
                Header.StreamCount = Header.Channels - Header.CoupledCount;

                if (mapping == 1) {
                    for (var i = 0; i < Header.Channels; i++) {
                        Header.ChannelMapping[i] = MappingMatrix[Header.Channels - 1][i];
                    }
                } else {
                    Header.ChannelMapping = new byte[Header.Channels];
                    for (var i = 0; i < Header.Channels; i++) {
                        Header.ChannelMapping[i] = (byte) i;
                    }
                }
            }

            if (Header.SampleRate == 0) {
                Header.SampleRate = 48000;
            }

            FrameTable = new ushort[Header.TableCount];
            stream.Position = Header.TableOffset;
            for (var i = 0; i < Header.TableCount; i++) {
                FrameTable[i] = Reader.ReadUInt16();
            }
        }

        public void Dispose() {
            Reader.Dispose();
        }

        public void ConvertToOgg(Stream outputStream) {
            using var writer = new BinaryWriter(outputStream, Encoding.UTF8, true);
            using var ogg = new BitOggStream(writer);

            Reader.BaseStream.Position = Header.DataOffset;

            #region Opus Header
            ogg.Write(new BitUint(32, BinaryPrimitives.ReverseEndianness(0x4F707573u))); // "Opus"
            ogg.Write(new BitUint(32, BinaryPrimitives.ReverseEndianness(0x48656164u))); // "Head"
            ogg.Write(new BitUint(8, 1)); // Version
            ogg.Write(new BitUint(8, (uint) Header.Channels));
            ogg.Write(new BitUint(16, (uint) Header.Skip));
            ogg.Write(new BitUint(32, (uint) Header.SampleRate));
            ogg.Write(new BitUint(16, 0));
            var mappingFamily = Header.Channels > 2 || Header.StreamCount > 1 ? 1u : 0;
            ogg.Write(new BitUint(8, mappingFamily));

            if (mappingFamily > 0) {
                ogg.Write(new BitUint(8, (uint)Header.StreamCount));
                ogg.Write(new BitUint(8, (uint)Header.CoupledCount));
                for (var i = 0; i < Header.Channels; i++) {
                    ogg.Write(new BitUint(8, Header.ChannelMapping[i]));
                }
            }

            ogg.FlushPage();
            #endregion

            #region Opus Comment
            ogg.Write(new BitUint(32, BinaryPrimitives.ReverseEndianness(0x4F707573u))); // "Opus"
            ogg.Write(new BitUint(32, BinaryPrimitives.ReverseEndianness(0x54616773u))); // "Tags"
            const string vendor = "Converted from Audiokinetic Wwise by DataTool";

            BitUint vendorSize = new BitUint(32, (uint) vendor.Length);
            ogg.Write(vendorSize);

            foreach (char vendorChar in vendor) {
                BitUint c = new BitUint(8, (byte) vendorChar);
                ogg.Write(c);
            }
            ogg.Write(new BitUint(32, 0)); // User comment list length
            ogg.FlushPage();
            #endregion

            Reader.BaseStream.Position = Header.DataOffset;
            var granule = 0u;
            for (var index = 0; index < FrameTable.Length; index++) {
                var frameSize = FrameTable[index];
                var frame = Reader.ReadBytes(frameSize);
                granule += (uint) (GetNbFrames(frame) * GetSamplesPerFrame(frame, Header.SampleRate));
                ogg.SetGranule(granule);
                ogg.Write(frame);
                ogg.FlushPage();
            }
        }

        private static int GetSamplesPerFrame(byte[] data, int Fs) {
            int size;
            if ((data[0] & 0x80) != 0) {
                size = (data[0] >> 3) & 0x3;
                size = (Fs << size) / 400;
            } else if ((data[0] & 0x60) == 0x60) {
                size = (data[0] & 0x08) != 0 ? Fs / 50 : Fs / 100;
            } else {
                size = (data[0] >> 3) & 0x3;
                if (size == 3) {
                    size = Fs * 60 / 1000;
                } else {
                    size = (Fs << size) / 100;
                }
            }

            return size;
        }

        private static int GetNbFrames(byte[] packet) {
            if (packet.Length < 1) {
                return 0;
            }

            var count = packet[0] & 0x3;
            if (count == 0) {
                return 1;
            }

            if (count != 3) {
                return 2;
            }

            if (packet.Length < 2) {
                return 0;
            }

            return packet[1] & 0x3F;
        }
    }
}