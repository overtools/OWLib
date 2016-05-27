using System;
using System.IO;

namespace OWLib {
  public unsafe struct MD5Hash {
    public fixed byte Value[16];
  }

  public delegate Stream LookupContentByKeyDelegate(ulong key);
  public delegate Stream LookupContentByHashDelegate(MD5Hash hash);
}
