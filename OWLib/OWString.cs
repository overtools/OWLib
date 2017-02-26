using System.IO;
using System.Text;
using OWLib.Types;

namespace OWLib {
  public class OWString {
    private OWStringHeader header;
    public OWStringHeader Header => header;
    public string Value;
    public uint References => header.references;

    public OWString(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, Encoding.UTF8)) {
        header = reader.Read<OWStringHeader>();
        input.Position = (long)header.offset;
        char[] bytes;
        if(header.size > 0) {
          bytes = reader.ReadChars((int)header.size);
        } else {
          bytes = reader.ReadChars((int)(input.Length - input.Position));
        }

        Value = new string(bytes).TrimEnd('\0');
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

    public string Format(params object[] format) {
      if(header.references == 0) {
        return Value;
      }
      object[] r = new object[header.references];
      for(int i = 0; i < r.Length; ++i) {
        if(i < format.Length) {
          r[i] = format[i];
        } else {
          r[i] = 0;
        }
      }
      return string.Format(Value, format);
    }
  }
}
