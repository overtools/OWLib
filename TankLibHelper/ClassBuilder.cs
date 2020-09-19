using System.CodeDom.Compiler;

namespace TankLibHelper {
    public abstract class ClassBuilder {
        public string Name { get; protected set; }
        protected readonly StructuredDataInfo Info;

        public abstract bool HasRealName { get; }

        protected ClassBuilder(StructuredDataInfo info) {
            Info = info;
        }

        public abstract IndentedTextWriter Build(FileWriter file);
    }
}