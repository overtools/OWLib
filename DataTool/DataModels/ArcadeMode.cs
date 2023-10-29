using System.Linq;
using TankLib;
using TankLib.STU.Types;
using DataTool.Helper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    public class ArcadeMode {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public teResourceGUID Image { get; set; }
        public teResourceGUID Brawl { get; set; }
        public teResourceGUID[] Children { get; set; }
        public string[] About { get; set; }

        public ArcadeMode(ulong key) {
            STU_E3594B8E stu = STUHelper.GetInstance<STU_E3594B8E>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public ArcadeMode(STU_E3594B8E stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STU_E3594B8E arcade, ulong key = default) {
            if (arcade == null) return;

            GUID = (teResourceGUID) key;
            Name = GetString(arcade.m_name);
            Description = GetString(arcade.m_description);
            Image = arcade.m_21EB3E73;

            switch (arcade) {
                case STU_598579A3 a1:
                    Brawl = a1.m_5DC61E59;
                    break;
                case STU_19C05237 a2:
                    Children = a2.m_children?.Select(x => x.GUID).ToArray();
                    break;
                default:
                    break;
            }

            About = arcade.m_5797DE13?.Select(x => {
                var aboutStuff = STUHelper.GetInstance<STU_56830926>(x);

                var name = GetString(aboutStuff.m_name);
                string desc = null;

                if (aboutStuff is STU_F31D4F9C ye)
                    desc = GetString(ye.m_description);

                return new string[] {name, desc};
            }).SelectMany(x => x).Where(x => x != null).ToArray();
        }
    }
}
