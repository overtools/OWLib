using System;
using TankLib.STU.Types;

namespace TankLibHelper {
    // wip testing thing. might become a real thing eventually
    public class CodeWriter {
        public CodeObj Namespace(string name) {
            return null;
        }

        public CodeObj Class(string name) {
            return null;
        }
        
        public CodeObj Enum(string name) {
            return null;
        }
        
        public CodeObj EnumField(string name, object value) {
            return null;
        }
        
        public CodeObj Struct(string name) {
            return null;
        }

        public CodeObj Field(string name, Type type) {
            return null;
        }

        public void Comment(string comment) {
            
        }

        public static void Test() {
            CodeWriter writer = new CodeWriter();

            using (writer.Namespace("TankLib.STU.Types")) {
                writer.Comment("this is bad");
                using (writer.Class("STUModelLook")) {
                    using (writer.Field("m_materials", typeof(STUModelMaterial))) {
                        
                    }
                }

                using (writer.Enum("STUUnlockRarity")) {
                    using (writer.EnumField("Legendary", 100)) {
                        
                    }
                }
            }
        }
    }

    public class CodeObj : IDisposable {
        public void Dispose() { }
    }
}