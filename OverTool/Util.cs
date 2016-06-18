using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;

namespace OverTool {
  public class Util {
    public static void CopyBytes(Stream i, Stream o, int sz) {
      byte[] buffer = new byte[sz];
      i.Read(buffer, 0, sz);
      o.Write(buffer, 0, sz);
      buffer = null;
    }

    public static Stream OpenFile(Record record, CASCHandler handler) {
      MemoryStream ms = new MemoryStream(record.record.Size);

      long offset = 0;
      EncodingEntry enc;
      if(((ContentFlags)record.record.Flags & ContentFlags.Bundle) == ContentFlags.Bundle) {
        offset = record.record.Offset;
        handler.Encoding.GetEntry(record.index.bundleContentKey, out enc);
      } else {
        handler.Encoding.GetEntry(record.record.ContentKey, out enc);
      }

      Stream fstream = handler.OpenFile(enc.Key);
      fstream.Position = offset;
      CopyBytes(fstream, ms, record.record.Size);
      ms.Position = 0;
      return ms;
    }

    public static string GetString(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return null;
      }

      Stream str = OpenFile(map[key], handler);
      OWString ows = new OWString(str);
      return ows.Value;
    }
  }
}
