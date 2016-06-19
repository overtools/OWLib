using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
  public class ModelParamRecord : ISTUDInstance {
    public ulong Key
    {
      get
      {
        return 0x0DECF2BA78E343C9;
      }
    }

    public string Name
    {
      get
      {
        return "GameParameter:Model";
      }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ModelParam {
      public STUDInstanceInfo instance;
      public ulong unk1;
      public OWRecord binding;
    }

    private ModelParam param;
    public ModelParam Param => param;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        param = reader.Read<ModelParam>();
      }
    }
  }
}
