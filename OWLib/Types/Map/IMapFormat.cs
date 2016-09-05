using System.IO;

namespace OWLib.Types.Map {
    public interface IMapFormat {
        ushort Identifier
        {
            get;
        }
    
        string Name
        {
            get;
        }
    
        void Read(Stream data);
    }
}
