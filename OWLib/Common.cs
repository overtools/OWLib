using System.IO;
using System.Runtime.InteropServices;

namespace OWLib {
  public unsafe struct MD5Hash {
    public fixed byte Value[16];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct Matrix4B {
    public fixed float Value[16];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct Matrix3x4B {
    public fixed float Value[12];
  }

  public delegate Stream LookupContentByKeyDelegate(ulong key);
  public delegate Stream LookupContentByHashDelegate(MD5Hash hash);
}
