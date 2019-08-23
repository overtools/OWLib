using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TACTLib;
using TACTLib.Core.Product.Tank;

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
                if (surfaces > 1 || IsMultiSurface) {
                    ret.Caps1 = 0x8 | 0x1000;
                    ret.Format = TextureTypes.TextureType.Unknown.ToPixelFormat();
                }

                if (IsCubemap) ret.Caps2 = 0xFE00;

                if (Mips > 1 && (PayloadCount == 1 || IsCubemap)) {
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

            public bool IsCubemap => HasFlag(Flags.CUBEMAP);

            public bool IsMultiSurface => HasFlag(Flags.MULTISURFACE);

            public bool IsWorld => HasFlag(Flags.WORLD);
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

            reader.Seek(128);
            Data = new byte[Header.DataSize];
            reader.Read(Data, 0, (int)Header.DataSize);
        }

        public teResourceGUID GetPayloadGUID(teResourceGUID textureResource, int region, int offset) {
            if (Header.PayloadCount - offset - 1 < 0) return new teResourceGUID(0);
            ulong guid = (textureResource & 0xFFF0FFFFFFFFUL) | ((ulong)(byte)(Header.PayloadCount - offset - 1) << 32) | 0x0320000000000000UL;
            // so basically: thing | (payloadIdx & 0xF) << 32) | 0x320000000000000i64
            
            if(teResourceGUID.Type(textureResource) == 0xF1)
            {
                guid |= (ulong)region << 40;
            }
            return new teResourceGUID(guid);
        }

        public ulong GetPayloadGUID(ulong guid, int region, int offset) => GetPayloadGUID(new teResourceGUID(guid), region, offset);

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
        public void SaveToDDS(Stream stream, bool keepOpen, int mips, uint? width = null, uint? height = null, uint? surfaces = null) {
            if (PayloadRequired && Payloads[0] == null) throw new Exceptions.TexturePayloadMissingException();
            using (BinaryWriter ddsWriter = new BinaryWriter(stream, Encoding.Default, keepOpen)) {
                TextureTypes.DDSHeader dds = Header.ToDDSHeader(mips, width ?? Header.Width, height ?? Header.Height, surfaces ?? Header.Surfaces);
                ddsWriter.Write(dds);
                if (dds.Format.FourCC == 0x30315844) {
                    TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                        Format = Header.Format,
                        Dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D,
                        Misc = (uint) (Header.IsCubemap ? 0x4 : 0),
                        Size = (uint) (Header.IsCubemap ? 1 : (surfaces ?? Header.Surfaces)),
                        Misc2 = 0
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
            SaveToDDS(stream, true, mips ?? Header.Mips, width, height, surfaces);
            stream.Position = 0;
            return stream;
        }
    }
}