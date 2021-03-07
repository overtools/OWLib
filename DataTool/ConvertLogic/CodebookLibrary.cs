using System;
using System.IO;

namespace DataTool.ConvertLogic {
    public class CodebookLibrary {
        public string m_file;

        public byte[] m_codebookData;
        public long[] m_codebookOffsets;

        private readonly long m_codebookCount;

        public CodebookLibrary(string file) {
            m_file = file;

            using (Stream codebookStream = System.IO.File.OpenRead(file)) {
                using (BinaryReader reader = new BinaryReader(codebookStream)) {
                    long fileSize = codebookStream.Length;

                    codebookStream.Seek(fileSize - 4, SeekOrigin.Begin);
                    long offsetOffset = reader.ReadInt32();

                    m_codebookCount = (fileSize - offsetOffset) / 4;

                    m_codebookData = new byte[offsetOffset];
                    m_codebookOffsets = new long[m_codebookCount];

                    codebookStream.Position = 0;
                    for (int i = 0; i < offsetOffset; i++) {
                        m_codebookData[i] = reader.ReadByte();
                    }

                    for (int i = 0; i < m_codebookCount; i++) {
                        m_codebookOffsets[i] = reader.ReadInt32();
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

            long cbStartIndex = (long) cbIndexStart;
            long unsignedSize = (long) cbSize;

            Stream codebookStream = new MemoryStream();
            for (long i = cbStartIndex; i < unsignedSize + cbStartIndex; i++) {
                codebookStream.WriteByte(m_codebookData[i]);
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
                    BitUint number = new BitUint((uint) Sound.WwiseRIFFVorbis.Ilog((uint) (entries - currentEntry)));
                    bis.Read(number);
                    bos.Write(number);
                    currentEntry = (int) (currentEntry + number);
                }

                if (currentEntry > entries) throw new Exception("current_entry out of range");
            } else {
                /* IN: 3 bit codeword length length, 1 bit sparse flag */
                BitUint codewordLengthLength = new BitUint(3);
                BitUint sparse = new BitUint(1);
                bis.Read(codewordLengthLength);
                bis.Read(sparse);

                if (0 == codewordLengthLength || 5 < codewordLengthLength) {
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
                for (int i = 0; i < entries; i++) {
                    bool presentBool = true;

                    if (sparse == 1) {
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
                for (uint i = 0; i < quantvals; i++) {
                    /* IN/OUT: n bit value */
                    BitUint val = new BitUint(valueLength + 1);
                    bis.Read(val);
                    bos.Write(val);
                }
            }

            /* check that we used exactly all bytes */
            /* note: if all bits are used in the last byte there will be one extra 0 byte */

            if (0 != cbSize && bis.TotalBitsRead / 8 + 1 != (int) cbSize) {
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
            if (i >= m_codebookCount - 1 || i < 0) return null;
            return m_codebookOffsets[i]; // return the offset
            // CodebookData[CodebookOffsets[i]]
        }

        public long GetCodebookSize(int i) {
            if (i >= m_codebookCount - 1 || i < 0) return -1;
            return m_codebookOffsets[i + 1] - m_codebookOffsets[i];
        }

        public class InvalidID : Exception { }
    }
}
