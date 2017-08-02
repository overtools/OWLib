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

        string FullOpt {
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

        bool Display {
            get;
        }

        void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags);
    }
}
