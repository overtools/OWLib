using System.IO;
using System.Text;

namespace TankLib {
    /// <summary>Tank Texture, type 004</summary>
    public class teTexture {
        public teTexturePayload Payload;
        public bool PayloadRequired;

        // non-payload
        public byte[] Data;
        public TextureTypes.TextureHeader Header;
        public uint Size;
        public TextureTypes.DXGI_PIXEL_FORMAT Format;
        
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
            Header = reader.Read<TextureTypes.TextureHeader>();
            Size = Header.DataSize;
            Format = Header.Format;

            if (Header.DataSize == 0) {
                PayloadRequired = true;
                return;
            }

            reader.Seek(128);
            Data = new byte[Header.DataSize];
            reader.Read(Data, 0, (int)Header.DataSize);
        }

        public teResourceGUID GetPayloadGUID(teResourceGUID textureResource, int region = 1)
        {
            ulong guid = (textureResource & 0xF0FFFFFFFFUL) | ((ulong)((byte)(Header.Indice - 1)) << 32) | 0x0320000000000000UL;
            if(teResourceGUID.Type(textureResource) == 0xF1)
            {
                guid |= ((ulong)region << 40);
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
                Payload.SaveToDDS(stream, keepOpen);
            } else {
                using (BinaryWriter ddsWriter = new BinaryWriter(stream, Encoding.Default, keepOpen)) {
                    TextureTypes.DDSHeader dds = Header.ToDDSHeader();
                    ddsWriter.Write(dds);
                    if (dds.Format.FourCC == 808540228) {
                        TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                            Format = (uint)Header.Format,
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