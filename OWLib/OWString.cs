using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;

namespace OWLib {
  public class OWString {
    private OWStringHeader header;
    public OWStringHeader Header => header;
    public string Value;
    public uint References => header.references;

    public OWString(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        header = reader.Read<OWStringHeader>();
        input.Position = (long)header.offset;
        byte[] bytes;
        if(header.size > 0) {
          bytes = reader.ReadBytes((int)header.size);
        } else {
          bytes = reader.ReadBytes((int)(input.Length - input.Position - 1));
        }

        Value = Encoding.UTF8.GetString(bytes).Trim().Trim(new char[] { '\0' });
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
