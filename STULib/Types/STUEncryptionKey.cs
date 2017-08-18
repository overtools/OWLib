using System;
using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x8F754DFF)]
    public class STUEncryptionKey : STUInstance {
        [STUField(0x413F29AE)]
        public byte[] KeyValue;

        [STUField(0xCD0F2F64)]
        public byte[] KeyName;


        public string KeyNameText {
            get {
                string x = "";
                for (int i = KeyName.Length - 1; i > 0; i -= 2) {
                    char h = (char)KeyName[i];
                    char l = (char)KeyName[i - 1];
                    x += l.ToString() + h.ToString();
                }
                return x.ToUpperInvariant();
            }
        }

        public string KeyValueText {
            get {
                return BitConverter.ToString(KeyValue).Replace("-", string.Empty);
            }
        }
        public ulong KeyNameLong {
            get {
                return ulong.Parse(KeyNameText, System.Globalization.NumberStyles.HexNumber);
            }
        }
    }
}
