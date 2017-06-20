using System.IO;

namespace OWLib.Types.Map {
    public interface IMapFormat {
        ushort Identifier {
            get;
        }

        string Name {
            get;
        }

        bool HasSTUD {
            get;
        }

        void Read(Stream data);
    }
}
