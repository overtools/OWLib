using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STULib {
    public class STUAttribute : Attribute {
        public uint Checksum = 0; // if 0: take CRC of name
        public string Name = null; // if null: take struct name
    }
}
