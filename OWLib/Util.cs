using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWLib {
  public static class Util {
    public static string GetEnumName(Type t, object value) {
      string v = Enum.GetName(t, value);
      if(v == null) {
        v = value.ToString();
      }
      return v;
    }
  }
}
