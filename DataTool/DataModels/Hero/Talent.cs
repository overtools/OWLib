using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.Hero {
    public class Talent {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public teResourceGUID TextureGUID { get; set; }

        public Talent(ulong key) {
            STU_BDDF370E stu = STUHelper.GetInstance<STU_BDDF370E>(key);
            Init(stu, key);
        }

        public Talent(STU_BDDF370E stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STU_BDDF370E talent, ulong key = default) {
            if (talent == null) return;

            GUID = (teResourceGUID) key;
            TextureGUID = talent.m_544A6A4F;

            Name = IO.GetString(talent.m_name)?.TrimEnd();
            // todo: why do the names end in spaces.. it causes a double-space with the chat box in-game
            Description = IO.GetString(talent.m_description);
        }
    }
}
