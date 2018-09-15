using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TankLib {
    /// <summary>Tank Texture, type 004</summary>
    public class teTexture {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TextureHeader {
            public Flags Flags;
            public byte Unknown1;
            public byte Mips;
            public byte Format;
            public byte Surfaces;
            public byte Unknown2;
            public byte PayloadCount;
            public byte Unknown3;
            public ushort Width;
            public ushort Height;
            public uint DataSize;
            public ulong ReferenceKey;
            public ulong Unknown4;

            public TextureTypes.DDSHeader ToDDSHeader() {
                TextureTypes.DDSHeader ret = new TextureTypes.DDSHeader {
                    Magic = 0x20534444,
                    Size = 124,
                    Flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x20000,
                    Height = Height,
                    Width = Width,
                    LinearSize = 0,
                    Depth = 0,
                    MipmapCount = 1,
                    Format = GetTextureType().ToPixelFormat(),
                    Caps1 = 0x1000,
                    Caps2 = 0,
                    Caps3 = 0,
                    Caps4 = 0,
                    Reserved2 = 0
                };
                if (Surfaces > 1) {
                    ret.Caps1 = 0x8 | 0x1000;
                    ret.Format = TextureTypes.TextureType.Unknown.ToPixelFormat();
                }

                if (IsCubemap()) ret.Caps2 = 0xFE00;

                if (Mips > 1 && (PayloadCount == 1 || IsCubemap())) {
                    ret.MipmapCount = Mips;
                    ret.Caps1 = 0x8 | 0x1000 | 0x400000;
                }

                return ret;
            }

            public TextureTypes.TextureType GetTextureType() {
                return TextureTypeFromHeaderByte(Format);
            }

            public static TextureTypes.TextureType TextureTypeFromHeaderByte(byte type) {
                if (type == 70 || type == 71 || type == 72) return TextureTypes.TextureType.DXT1;

                if (type == 73 || type == 74 || type == 75) return TextureTypes.TextureType.DXT3;

                if (type == 76 || type == 77 || type == 78) return TextureTypes.TextureType.DXT5;

                if (type == 79 || type == 80 || type == 81) return TextureTypes.TextureType.ATI1;

                if (type == 82 || type == 83 || type == 84) return TextureTypes.TextureType.ATI2;

                return TextureTypes.TextureType.Unknown;
            }

            public bool HasFlag(Flags flag) {
                return (Flags & flag) == flag;
            }

            public bool IsCubemap() {
                return HasFlag(Flags.CUBEMAP);
            }

            public bool IsMultisurface() {
                return HasFlag(Flags.MULTISURFACE);
            }

            public bool IsWorld() {
                return HasFlag(Flags.WORLD);
            }
        }

        public static readonly int[] DXGI_BC4 = { 79, 80, 91 };
        public static readonly int[] DXGI_BC5 = { 82, 83, 84 };

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Flags]
        public enum Flags : byte {
            UNKNOWN1 = 0x01,
            DIFFUSE = 0x02,
            UNKNOWN2 = 0x04,
            CUBEMAP = 0x08,
            UNKNOWN4 = 0x10,
            WORLD = 0x20,
            MULTISURFACE = 0x40,
            UNKNOWN5 = 0x80
        }
        
        public teTexturePayload Payload;
        public bool PayloadRequired;

        // non-payload
        public byte[] Data;
        public TextureHeader Header;
        
        /// <summary>Load texture from a stream</summary>
        public teTexture(Stream stream, bool keepOpen=false) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, keepOpen)) {
                Read(reader);
            }
        }
        
        /// <summary>Load texture from a stream</summary>
        public teTexture(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            Header = reader.Read<TextureHeader>();

            if (Header.DataSize == 0 || Header.PayloadCount > 0) {
                PayloadRequired = true;
                return;
            }

            reader.Seek(128);
            Data = new byte[Header.DataSize];
            reader.Read(Data, 0, (int)Header.DataSize);
        }

        public teResourceGUID GetPayloadGUID(teResourceGUID textureResource, int region = 1)
        {
            ulong guid = (textureResource & 0xF0FFFFFFFFUL) | ((ulong)(byte)(Header.PayloadCount - 1) << 32) | 0x0320000000000000UL;
            // so basically: thing | (payloadIdx & 0xF) << 32) | 0x320000000000000i64
            
            if(teResourceGUID.Type(textureResource) == 0xF1)
            {
                guid |= (ulong)region << 40;
            }
            return new teResourceGUID(guid);
        }

        public ulong GetPayloadGUID(ulong guid, int region = 1) => GetPayloadGUID(new teResourceGUID(guid), region);

        /// <summary>Load the texture payload</summary>
        /// <param name="payloadStream">The payload stream</param>
        public void LoadPayload(Stream payloadStream) {
            if (!PayloadRequired) throw new Exceptions.TexturePayloadNotRequiredException();
            if (Payload != null) throw new Exceptions.TexturePayloadAlreadyExistsException();
            
            Payload = new teTexturePayload(this, payloadStream);
        }

        /// <summary>Set the texture payload</summary>
        /// <param name="payload">The texture payload</param>
        public void SetPayload(teTexturePayload payload) {
            if (!PayloadRequired) throw new Exceptions.TexturePayloadNotRequiredException();
            if (Payload != null) throw new Exceptions.TexturePayloadAlreadyExistsException();
            
            Payload = payload;
        }

        /// <summary>Save DDS to stream</summary>
        /// <param name="stream">Stream to be written to</param>
        /// <param name="keepOpen">Keep the stream open after writing</param>
        public void SaveToDDS(Stream stream, bool keepOpen=false) {
            if (PayloadRequired) {
                if (Payload == null) {
                    throw new Exceptions.TexturePayloadMissingException();
                }
                Payload.SaveToDDS(Header, stream, keepOpen);
            } else {
                using (BinaryWriter ddsWriter = new BinaryWriter(stream, Encoding.Default, keepOpen)) {
                    TextureTypes.DDSHeader dds = Header.ToDDSHeader();
                    ddsWriter.Write(dds);
                    if (dds.Format.FourCC == 0x30315844) {
                        TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                            Format = Header.Format,
                            Dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D,
                            Misc = (uint)(Header.IsCubemap() ? 0x4 : 0),
                            Size = (uint)(Header.IsCubemap() ? 1 : Header.Surfaces),
                            Misc2 = 0
                        };
                        ddsWriter.Write(d10);
                    }
                    ddsWriter.Write(Data, 0, (int)Header.DataSize);
                }
            }
        }

        /// <summary>Save DDS to stream</summary>
        public Stream SaveToDDS() {
            MemoryStream stream = new MemoryStream();
            SaveToDDS(stream, true);
            stream.Position = 0;
            return stream;
        }
    }
}