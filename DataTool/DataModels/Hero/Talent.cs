using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.Hero {
    public class Talent {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public teResourceGUID TextureGUID { get; set; }

        public Talent(STU_BDDF370E stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STU_BDDF370E talent, ulong key = default) {
            GUID = (teResourceGUID) key;
            TextureGUID = talent.m_544A6A4F;

            Name = IO.GetString(talent.m_name)?.TrimEnd();
            // todo: why do the names end in spaces.. it causes a double-space with the chat box in-game
            Description = IO.GetString(talent.m_description);
        }

        public static Talent Load(ulong guid) {
            STU_DF0481B0 baseType = STUHelper.GetInstance<STU_DF0481B0>(guid);
            if (baseType is not STU_BDDF370E talent) {
                // is a perk (or null)
                return null;
            }

            return new Talent(talent, guid);
        }
    }
}
