using System;

namespace DataTool.ConvertLogic.WEM {
    [AttributeUsage(AttributeTargets.Class)]
    public class BankObjectAttribute : Attribute {
        public byte Type;

        public BankObjectAttribute(byte type) {
            Type = type;
        }
    }
}