using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWLib {
  public class OWString {
    public string Value;
    private ulong offset;
    private uint size;
    public uint References;

    public OWString(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        offset = reader.ReadUInt64();
        size = reader.ReadUInt32();
        References = reader.ReadUInt32();
        input.Position = (long)offset;
        if(size > 0) {
          Value = Encoding.UTF8.GetString(reader.ReadBytes((int)size));
        } else {
          Value = Encoding.UTF8.GetString(reader.ReadBytes((int)(input.Length - input.Position - 1)));
        }
      }
    }

    public bool Equals(OWString other) {
      return other.Value == Value;
    }

    public static bool operator ==(OWString a, string b) {
      return a.Value == b;
    }

    public  static bool operator ==(OWString a, OWString b) {
      return a.Value == b.Value;
    }

    public static bool operator !=(OWString a, string b) {
      return a.Value != b;
    }

    public  static bool operator !=(OWString a, OWString b) {
      return a.Value != b.Value;
    }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      return base.Equals(obj);
    }

    public override string ToString() {
      return Value;
    }
  }
}
