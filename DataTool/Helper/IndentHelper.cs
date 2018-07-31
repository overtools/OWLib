using System.Diagnostics;
using System.Linq;

namespace DataTool.Helper {
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class IndentHelper {
        private readonly uint _indentLevel;

        protected static string IndentString = " ";
        protected static uint IndentStringPerLevel = 4;

        protected string DebuggerDisplay => $"Level: {GetLevel()}";
        
        
        public IndentHelper(uint level) {
            _indentLevel = level;
        }
        
        public IndentHelper() {
            _indentLevel = 0;
        }

        public IndentHelper(IndentHelper obj) {
            _indentLevel = obj._indentLevel;
        }

        public IndentHelper(string existingValue) {  // create from existing string
            string find = GetIndentString(IndentStringPerLevel);  // 1 indent
            string s2 = existingValue.Replace(find, "");  // how many times is 1 indent string in existing string
            _indentLevel = ((uint)existingValue.Length - (uint)s2.Length) / (uint)find.Length;
            
            // Debug.Assert(new IndentHelper(1).GetLevel() == new IndentHelper("    ").GetLevel());
            // Debug.Assert(new IndentHelper(2).GetLevel() == new IndentHelper("        ").GetLevel());
            // Debug.Assert(new IndentHelper(3).GetLevel() == new IndentHelper("            ").GetLevel());
            // Debug.Assert(new IndentHelper(4).GetLevel() == new IndentHelper("                ").GetLevel());
        }

        protected string GetIndentString(int count) => string.Concat(Enumerable.Repeat(IndentString, count));
        protected string GetIndentString(uint count) => GetIndentString((int) count);
        
        public override string ToString() {
            return GetIndentString(_indentLevel * IndentStringPerLevel);
        }
        
        public static IndentHelper operator +(IndentHelper c1, uint c2) => new IndentHelper(c1._indentLevel + c2);
        public static IndentHelper operator +(IndentHelper c1, IndentHelper c2) => new IndentHelper(c1._indentLevel + c2._indentLevel);
        
        public static IndentHelper operator -(IndentHelper c1, uint c2) => new IndentHelper(c1._indentLevel - c2);
        public static IndentHelper operator -(IndentHelper c1, IndentHelper c2) => new IndentHelper(c1._indentLevel - c2._indentLevel);
        
        public static IndentHelper operator *(IndentHelper c1, uint c2) => new IndentHelper(c1._indentLevel * c2);
        public static IndentHelper operator *(IndentHelper c1, IndentHelper c2) => new IndentHelper(c1._indentLevel * c2._indentLevel);
        
        public static IndentHelper operator /(IndentHelper c1, uint c2) => new IndentHelper(c1._indentLevel / c2);
        public static IndentHelper operator /(IndentHelper c1, IndentHelper c2) => new IndentHelper(c1._indentLevel / c2._indentLevel);

        public static implicit operator string(IndentHelper obj) => obj.ToString();

        public uint GetLevel() => _indentLevel;
    }
}
