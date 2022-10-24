using System.IO;

namespace DataTool.ConvertLogic.WEM {
    public interface IBankObject {
        void Read(BinaryReader reader);
    }
}