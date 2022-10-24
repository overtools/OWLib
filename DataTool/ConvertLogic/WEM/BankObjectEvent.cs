using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(4)]
    public class BankObjectEvent : IBankObject {
        public uint[] Actions;

        public void Read(BinaryReader reader) {
            byte numActions = reader.ReadByte();

            Actions = new uint[numActions];
            for (int i = 0; i < numActions; i++) {
                Actions[i] = reader.ReadUInt32();
            }
        }
    }
}