using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TACTLib;
using TACTLib.Core.Product.Tank;
using TankLib.STU.Types.Enums;

namespace TankLib {
    /// <summary>Tank Texture, type 004</summary>
    public class teTexture {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TextureHeader {
            public Flags Flags;
            public byte MipCount; // 2
            public byte Format; // 3
            public byte Surfaces; // 4
            public Enum_950F7205 UsageCategory; // 5
            public byte PayloadCount; // 6
            public byte Unk7; // 7
            public ushort Width; // 8
            public ushort Height; // 10
            public uint DataSize; // 12
            public ulong Unk16; // 16
            public ulong Unk24; // 24

            public TextureTypes.DDSHeader ToDDSHeader(int mips, uint width, uint height, uint surfaces) {
                TextureTypes.DDSHeader ret = new TextureTypes.DDSHeader {
                    Magic = 0x20534444,
                    Size = 124,
                    Flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x20000,
                    Height = height,
                    Width = width,
                    LinearSize = 0,
                    Depth = 0,
                    MipmapCount = (uint)mips,
                    Format = GetTextureType().ToPixelFormat(),
                    Caps1 = 0x1000,
                    Caps2 = 0,
                    Caps3 = 0,
                    Caps4 = 0,
                    Reserved2 = 0
                };
                if (surfaces > 1 || IsArray) {
                    ret.Caps1 = 0x8 | 0x1000;
                    ret.Format = TextureTypes.TextureType.Unknown.ToPixelFormat();
                }

                if (IsCubemap) ret.Caps2 = 0xFE00;

                // todo: wtf
                if (MipCount > 1 && (PayloadCount == 1 || IsCubemap)) {
                    ret.MipmapCount = MipCount;
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

            public bool IsCubemap => HasFlag(Flags.Cube);

            public bool IsArray => HasFlag(Flags.Array);
        }

        public static readonly int[] DXGI_BC4 = { 79, 80, 91 };
        public static readonly int[] DXGI_BC5 = { 82, 83, 84 };
        public static readonly TextureTypes.TextureType[] ATI2 = {TextureTypes.TextureType.ATI1, TextureTypes.TextureType.ATI2};

        [Flags]
        public enum Flags : short {
            Tex1D = 0x01,
            Tex2D = 0x02,
            Tex3D = 0x04,
            Cube = 0x08,
            Unk16 = 0x10,
            Unk32 = 0x20,
            Array = 0x40,
            Unk128 = 0x80
        }
        
        public teTexturePayload[] Payloads = new teTexturePayload[0];
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

        public bool HasMultipleSurfaces => Header.Surfaces > 1 || Payloads.Any(x => x != null && x.Header.Surfaces > 1);

        private void Read(BinaryReader reader) {
            Header = reader.Read<TextureHeader>();
            if (Header.PayloadCount == 1) Logger.Debug("teTexture", $"texture {((reader.BaseStream is GuidStream gs) ? teResourceGUID.AsString(gs.GUID) : "internal") } is mip");

            if (Header.DataSize == 0 || Header.PayloadCount > 0) {
                PayloadRequired = true;
                Payloads = new teTexturePayload[Header.PayloadCount];
                return;
            }

            reader.Seek(0x20);
            Data = new byte[Header.DataSize];
            reader.Read(Data, 0, (int)Header.DataSize);
        }

        public teResourceGUID GetPayloadGUID(teResourceGUID textureGUID, int offset) {
            if (Header.PayloadCount - offset - 1 < 0) return new teResourceGUID(0);
            ulong payloadGUID = (textureGUID & 0xFFF0FFFFFFFFUL) | ((ulong)(byte)(Header.PayloadCount - offset - 1) << 32) | 0x0320000000000000UL;
            // so basically: thing | (payloadIdx & 0xF) << 32) | 0x320000000000000i64

            var type = teResourceGUID.Type(textureGUID);
            if(type == 0xF1)
            {
                payloadGUID |= (ulong)1 << 40;
            } else if (type != 4) {
                throw new Exception();
            }
            return new teResourceGUID(payloadGUID);
        }

        public ulong GetPayloadGUID(ulong guid, int offset) => GetPayloadGUID(new teResourceGUID(guid), offset);

        /// <summary>Load the texture payload</summary>
        /// <param name="payloadStream">The payload stream</param>
        /// <param name="offset"></param>
        public void LoadPayload(Stream payloadStream, int offset) {
            if (!PayloadRequired || Payloads.Length < offset) throw new Exceptions.TexturePayloadNotRequiredException();
            if (Payloads[offset] != null) throw new Exceptions.TexturePayloadAlreadyExistsException();
            
            Payloads[offset] = new teTexturePayload(this, payloadStream);
        }

        /// <summary>Set the texture payload</summary>
        /// <param name="payload">The texture payload</param>
        /// <param name="offset"></param>
        public void SetPayload(teTexturePayload payload, int offset) {
            if (!PayloadRequired || Payloads.Length < offset) throw new Exceptions.TexturePayloadNotRequiredException();
            if (Payloads[offset] != null) throw new Exceptions.TexturePayloadAlreadyExistsException();
            
            Payloads[offset] = payload;
        }

        /// <summary>Save DDS to stream</summary>
        /// <param name="stream">Stream to be written to</param>
        /// <param name="keepOpen">Keep the stream open after writing</param>
        /// <param name="mips"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="surfaces"></param>
        public void SaveToDDS(Stream stream, bool keepOpen, int? mips, uint? width = null, uint? height = null, uint? surfaces = null) {
            if (PayloadRequired && Payloads[0] == null) throw new Exceptions.TexturePayloadMissingException();
            using (BinaryWriter ddsWriter = new BinaryWriter(stream, Encoding.Default, keepOpen)) {
                TextureTypes.DDSHeader dds = Header.ToDDSHeader(mips ?? Header.MipCount, width ?? Header.Width, height ?? Header.Height, surfaces ?? Header.Surfaces);
                ddsWriter.Write(dds);
                if (dds.Format.FourCC == 0x30315844) {
                    var dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.UNKNOWN;
                    if (Header.HasFlag(Flags.Tex1D)) {
                        dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE1D;
                    } else if (Header.HasFlag(Flags.Tex2D)) {
                        dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D;
                    } else if (Header.HasFlag(Flags.Tex3D)) {
                        dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE3D;
                    } else if (Header.HasFlag(Flags.Cube)) {
                        // cubemaps are just 2d textures
                        dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D;
                    }
                    
                    TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                        Format = Header.Format,
                        Dimension = dimension,
                        Misc = (uint) (Header.IsCubemap ? 0x4 : 0), // 4 = D3D11_RESOURCE_MISC_TEXTURECUBE
                        Size = surfaces ?? (Header.IsCubemap ? Header.Surfaces/6u : Header.Surfaces),
                    };
                    ddsWriter.Write(d10);
                }

                if (PayloadRequired) {
                    foreach (var payload in Payloads.Where(x => x != null)) {
                        payload.SaveToDDSData(Header, ddsWriter);
                    }
                } else {
                    ddsWriter.Write(Data, 0, (int) Header.DataSize);
                }
            }
        }

        /// <summary>Save DDS to stream</summary>
        public Stream SaveToDDS(int? mips = null, uint? width = null, uint? height = null, uint? surfaces = null) {
            MemoryStream stream = new MemoryStream();
            SaveToDDS(stream, true, mips, width, height, surfaces);
            stream.Position = 0;
            return stream;
        }
    }
}
