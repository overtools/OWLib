using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.Hero {
    public class SkinTheme {
        public teResourceGUID GUID { get; set; }
        public teResourceGUID Skin { get; set; }

        public SkinTheme(STU_63172E83 skinTheme) {
            GUID = skinTheme.m_5E9665E3;
            Skin = skinTheme.m_0029461B;
        }
    }
}