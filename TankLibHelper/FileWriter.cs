using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace TankLibHelper {
    public class FileWriter {
        public readonly string m_filename;
        public readonly string m_namespace;
        public HashSet<string> m_includes = new HashSet<string>();
        public List<IndentedTextWriter> m_children = new List<IndentedTextWriter>();
        
        public FileWriter(string fn, string ns) {
            m_filename = fn;
            m_namespace = ns;
        }
        
        public void Finish() {
            var builder = new IndentedTextWriter(new StringWriter(), "    ");
            
            builder.WriteLine("// Generated by TankLibHelper");

            foreach (string include in m_includes) {
                builder.WriteLine($"using {include};");
            }
            builder.WriteLine();

            builder.WriteLine(@"// ReSharper disable All");
            builder.WriteLine($"namespace {m_namespace}");
            builder.WriteLine("{");
            builder.Indent++;
            
            string[] stringSeparators = new string[] { "\r\n" };

            bool first = true;

            foreach (IndentedTextWriter child in m_children) {
                if (first) first = false;
                else BlankLine(builder);
                
                foreach (var line in child.InnerWriter.ToString().Split(stringSeparators, StringSplitOptions.None)) {
                    if (string.IsNullOrWhiteSpace(line)) {
                        BlankLine(builder);
                        continue;
                    }
                    builder.WriteLine(line);
                }
            }

            builder.Indent--;
            builder.WriteLine("}");
            
            File.WriteAllText(m_filename, builder.InnerWriter.ToString());
        }
        
        private static void BlankLine(IndentedTextWriter writer) {
            var oldIndent = writer.Indent;
            writer.Indent = 0;
            writer.WriteLine();
            writer.Indent = oldIndent;
        }
    }
}
