using System;
using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x1DA7C021)]
    public class STUEncryptionKey : STUInstance {
        [STUField(0x1A71903A)]
        public byte[] KeyName;

        [STUField(0x2F709539)]
        public byte[] KeyValue;

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

        public string KeyValueText => BitConverter.ToString(KeyValue).Replace("-", string.Empty);

        public ulong KeyNameLong => ulong.Parse(KeyNameText, System.Globalization.NumberStyles.HexNumber);
    }
}