using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace CMFLib.Crypto {
    /// <summary>
    ///     Implements the Salsa20 stream encryption cipher, as defined at http://cr.yp.to/snuffle.html.
    /// </summary>
    /// <remarks>
    ///     See
    ///     <a href="http://code.logos.com/blog/2008/06/salsa20_implementation_in_c_1.html">Salsa20 Implementation in C#</a>.
    /// </remarks>
    public sealed class Salsa20 : SymmetricAlgorithm {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Salsa20" /> class.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///     The implementation of the class derived from the symmetric algorithm is not
        ///     valid.
        /// </exception>
        public Salsa20() {
            // set legal values
            LegalBlockSizesValue = new[] {new KeySizes(512, 512, 0)};
            LegalKeySizesValue = new[] {new KeySizes(128, 256, 128)};

            // set default values
            BlockSizeValue = 512;
            KeySizeValue = 256;
            m_rounds = 20;
        }

        /// <summary>
        ///     Creates a symmetric decryptor object with the specified <see cref="SymmetricAlgorithm.Key" /> property
        ///     and initialization vector (<see cref="SymmetricAlgorithm.IV" />).
        /// </summary>
        /// <param name="rgbKey">The secret key to use for the symmetric algorithm.</param>
        /// <param name="rgbIV">The initialization vector to use for the symmetric algorithm.</param>
        /// <returns>A symmetric decryptor object.</returns>
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV) {
            // decryption and encryption are symmetrical
            return CreateEncryptor(rgbKey, rgbIV);
        }

        /// <summary>
        ///     Creates a symmetric encryptor object with the specified <see cref="SymmetricAlgorithm.Key" /> property
        ///     and initialization vector (<see cref="SymmetricAlgorithm.IV" />).
        /// </summary>
        /// <param name="rgbKey">The secret key to use for the symmetric algorithm.</param>
        /// <param name="rgbIV">The initialization vector to use for the symmetric algorithm.</param>
        /// <returns>A symmetric encryptor object.</returns>
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV) {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            if (!ValidKeySize(rgbKey.Length * 8))
                throw new CryptographicException("Invalid key size; it must be 128 or 256 bits.");
            CheckValidIV(rgbIV, "rgbIV");

            return new Salsa20CryptoTransform(rgbKey, rgbIV, m_rounds);
        }

        private new bool ValidKeySize(int size) {
            return size == 128 || size == 256;
        }

        /// <summary>
        ///     Generates a random initialization vector (<see cref="SymmetricAlgorithm.IV" />) to use for the algorithm.
        /// </summary>
        public override void GenerateIV() {
            // generate a random 8-byte IV
            IVValue = GetRandomBytes(8);
        }

        /// <summary>
        ///     Generates a random key (<see cref="SymmetricAlgorithm.Key" />) to use for the algorithm.
        /// </summary>
        public override void GenerateKey() {
            // generate a random key
            KeyValue = GetRandomBytes(KeySize / 8);
        }

        /// <summary>
        ///     Gets or sets the initialization vector (<see cref="SymmetricAlgorithm.IV" />) for the symmetric algorithm.
        /// </summary>
        /// <value>The initialization vector.</value>
        /// <exception cref="ArgumentNullException">An attempt was made to set the initialization vector to null. </exception>
        /// <exception cref="CryptographicException">An attempt was made to set the initialization vector to an invalid size. </exception>
        public override byte[] IV {
            get => base.IV;
            set {
                CheckValidIV(value, "value");
                IVValue = (byte[]) value.Clone();
            }
        }

        /// <summary>
        ///     Gets or sets the number of rounds used by the Salsa20 algorithm.
        /// </summary>
        /// <value>The number of rounds.</value>
        public int Rounds {
            get => m_rounds;
            set {
                if (value != 8 && value != 12 && value != 20)
                    throw new ArgumentOutOfRangeException("value", "The number of rounds must be 8, 12, or 20.");
                m_rounds = value;
            }
        }

        // Verifies that iv is a legal value for a Salsa20 IV.
        private static void CheckValidIV(byte[] iv, string paramName) {
            if (iv == null)
                throw new ArgumentNullException(paramName);
            if (iv.Length != 8)
                throw new CryptographicException("Invalid IV size; it must be 8 bytes.");
        }

        private static readonly Random rnd = new Random();

        // Returns a new byte array containing the specified number of random bytes.
        private static byte[] GetRandomBytes(int byteCount) {
            byte[] bytes = new byte[byteCount];
            rnd.NextBytes(bytes);
            //using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            //    rng.GetBytes(bytes);
            return bytes;
        }

        private int m_rounds;

        /// <summary>
        ///     Salsa20Impl is an implementation of <see cref="ICryptoTransform" /> that uses the Salsa20 algorithm.
        /// </summary>
        private sealed class Salsa20CryptoTransform : ICryptoTransform {
            public Salsa20CryptoTransform(byte[] key, byte[] iv, int rounds) {
                Debug.Assert(key.Length == 16 || key.Length == 32, "abyKey.Length == 16 || abyKey.Length == 32",
                    "Invalid key size.");
                Debug.Assert(iv.Length == 8, "abyIV.Length == 8", "Invalid IV size.");
                Debug.Assert(rounds == 8 || rounds == 12 || rounds == 20, "rounds == 8 || rounds == 12 || rounds == 20",
                    "Invalid number of rounds.");

                Initialize(key, iv);
                m_rounds = rounds;
            }

            public bool CanReuseTransform => false;

            public bool CanTransformMultipleBlocks => true;

            public int InputBlockSize => 64;

            public int OutputBlockSize => 64;

            public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer,
                int outputOffset) {
                // check arguments
                if (inputBuffer == null)
                    throw new ArgumentNullException("inputBuffer");
                if (inputOffset < 0 || inputOffset >= inputBuffer.Length)
                    throw new ArgumentOutOfRangeException("inputOffset");
                if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
                    throw new ArgumentOutOfRangeException("inputCount");
                if (outputBuffer == null)
                    throw new ArgumentNullException("outputBuffer");
                if (outputOffset < 0 || outputOffset + inputCount > outputBuffer.Length)
                    throw new ArgumentOutOfRangeException("outputOffset");
                if (m_state == null)
                    throw new ObjectDisposedException(GetType().Name);

                byte[] output = new byte[64];
                int bytesTransformed = 0;

                while (inputCount > 0) {
                    Hash(output, m_state);
                    m_state[8] = AddOne(m_state[8]);
                    if (m_state[8] == 0) m_state[9] = AddOne(m_state[9]);

                    int blockSize = Math.Min(64, inputCount);
                    for (int i = 0; i < blockSize; i++)
                        outputBuffer[outputOffset + i] = (byte) (inputBuffer[inputOffset + i] ^ output[i]);
                    bytesTransformed += blockSize;

                    inputCount -= 64;
                    outputOffset += 64;
                    inputOffset += 64;
                }

                return bytesTransformed;
            }

            public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
                if (inputCount < 0)
                    throw new ArgumentOutOfRangeException("inputCount");

                byte[] output = new byte[inputCount];
                TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
                return output;
            }

            public void Dispose() {
                if (m_state != null)
                    Array.Clear(m_state, 0, m_state.Length);
                m_state = null;
            }

            private static uint Rotate(uint v, int c) {
                return (v << c) | (v >> (32 - c));
            }

            private static uint Add(uint v, uint w) {
                return unchecked(v + w);
            }

            private static uint AddOne(uint v) {
                return unchecked(v + 1);
            }

            private void Hash(byte[] output, uint[] input) {
                uint[] state = (uint[]) input.Clone();

                for (int round = m_rounds; round > 0; round -= 2) {
                    state[4] ^= Rotate(Add(state[0], state[12]), 7);
                    state[8] ^= Rotate(Add(state[4], state[0]), 9);
                    state[12] ^= Rotate(Add(state[8], state[4]), 13);
                    state[0] ^= Rotate(Add(state[12], state[8]), 18);
                    state[9] ^= Rotate(Add(state[5], state[1]), 7);
                    state[13] ^= Rotate(Add(state[9], state[5]), 9);
                    state[1] ^= Rotate(Add(state[13], state[9]), 13);
                    state[5] ^= Rotate(Add(state[1], state[13]), 18);
                    state[14] ^= Rotate(Add(state[10], state[6]), 7);
                    state[2] ^= Rotate(Add(state[14], state[10]), 9);
                    state[6] ^= Rotate(Add(state[2], state[14]), 13);
                    state[10] ^= Rotate(Add(state[6], state[2]), 18);
                    state[3] ^= Rotate(Add(state[15], state[11]), 7);
                    state[7] ^= Rotate(Add(state[3], state[15]), 9);
                    state[11] ^= Rotate(Add(state[7], state[3]), 13);
                    state[15] ^= Rotate(Add(state[11], state[7]), 18);
                    state[1] ^= Rotate(Add(state[0], state[3]), 7);
                    state[2] ^= Rotate(Add(state[1], state[0]), 9);
                    state[3] ^= Rotate(Add(state[2], state[1]), 13);
                    state[0] ^= Rotate(Add(state[3], state[2]), 18);
                    state[6] ^= Rotate(Add(state[5], state[4]), 7);
                    state[7] ^= Rotate(Add(state[6], state[5]), 9);
                    state[4] ^= Rotate(Add(state[7], state[6]), 13);
                    state[5] ^= Rotate(Add(state[4], state[7]), 18);
                    state[11] ^= Rotate(Add(state[10], state[9]), 7);
                    state[8] ^= Rotate(Add(state[11], state[10]), 9);
                    state[9] ^= Rotate(Add(state[8], state[11]), 13);
                    state[10] ^= Rotate(Add(state[9], state[8]), 18);
                    state[12] ^= Rotate(Add(state[15], state[14]), 7);
                    state[13] ^= Rotate(Add(state[12], state[15]), 9);
                    state[14] ^= Rotate(Add(state[13], state[12]), 13);
                    state[15] ^= Rotate(Add(state[14], state[13]), 18);
                }

                for (int index = 0; index < 16; index++)
                    ToBytes(Add(state[index], input[index]), output, 4 * index);
            }

            private void Initialize(byte[] key, byte[] iv) {
                m_state = new uint[16];
                m_state[1] = ToUInt32(key, 0);
                m_state[2] = ToUInt32(key, 4);
                m_state[3] = ToUInt32(key, 8);
                m_state[4] = ToUInt32(key, 12);

                byte[] constants = key.Length == 32 ? c_sigma : c_tau;
                int keyIndex = key.Length - 16;

                m_state[11] = ToUInt32(key, keyIndex + 0);
                m_state[12] = ToUInt32(key, keyIndex + 4);
                m_state[13] = ToUInt32(key, keyIndex + 8);
                m_state[14] = ToUInt32(key, keyIndex + 12);
                m_state[0] = ToUInt32(constants, 0);
                m_state[5] = ToUInt32(constants, 4);
                m_state[10] = ToUInt32(constants, 8);
                m_state[15] = ToUInt32(constants, 12);

                m_state[6] = ToUInt32(iv, 0);
                m_state[7] = ToUInt32(iv, 4);
                m_state[8] = 0;
                m_state[9] = 0;
            }

            private static uint ToUInt32(byte[] input, int inputOffset) {
                return unchecked((uint) (input[inputOffset] | (input[inputOffset + 1] << 8) |
                                         (input[inputOffset + 2] << 16) | (input[inputOffset + 3] << 24)));
            }

            private static void ToBytes(uint input, byte[] output, int outputOffset) {
                unchecked {
                    output[outputOffset] = (byte) input;
                    output[outputOffset + 1] = (byte) (input >> 8);
                    output[outputOffset + 2] = (byte) (input >> 16);
                    output[outputOffset + 3] = (byte) (input >> 24);
                }
            }

            private static readonly byte[] c_sigma = Encoding.ASCII.GetBytes("expand 32-byte k");
            private static readonly byte[] c_tau = Encoding.ASCII.GetBytes("expand 16-byte k");

            private uint[] m_state;
            private readonly int m_rounds;
        }
    }
}