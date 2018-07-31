using System.IO;
using DataTool.Flag;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock {
    public static class PortraitFrame {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            STUUnlock_PortraitFrame portraitFrame = (STUUnlock_PortraitFrame) unlock.STU;
            
            string tier = portraitFrame.m_rank.ToString();
                
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();

            if (portraitFrame.m_rankTexture != null) {
                FindLogic.Combo.Find(info, portraitFrame.m_rankTexture);
                info.SetTextureName(portraitFrame.m_rankTexture, $"Star - {portraitFrame.m_stars}");
            }

            if (portraitFrame.m_949D9C2A != null) {
                FindLogic.Combo.Find(info, portraitFrame.m_949D9C2A);
                int borderNum = portraitFrame.m_level - portraitFrame.m_stars * 10 - (int)portraitFrame.m_rank * 10;

                if ((int) portraitFrame.m_rank > 1) {
                    borderNum -= 50 * ((int) portraitFrame.m_rank - 1);
                }
                borderNum -= 1;
                    
                info.SetTextureName(portraitFrame.m_949D9C2A, $"Border - {borderNum}");
            }
                
            Combo.SaveLooseTextures(flags, Path.Combine(directory, tier), info);
        }
    }
}