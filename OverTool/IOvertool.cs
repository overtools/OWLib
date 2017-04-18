using System.Collections.Generic;
using CASCExplorer;

namespace OverTool {
    public interface IOvertool {
        string Title {
            get;
        }

        char Opt {
            get;
        }

        string Help {
            get;
        }

        uint MinimumArgs {
            get;
        }

        ushort[] Track {
            get;
        }

        void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args);
    }
}
