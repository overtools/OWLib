using System;

namespace DataTool.ConvertLogic.WEM {
    public class BankObjectTooMuchReadException : Exception {
        public BankObjectTooMuchReadException(string message) : base(message) { }
    }
}