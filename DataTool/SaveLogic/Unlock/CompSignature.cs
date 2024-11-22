using DataTool.Flag;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock {
    public static class CompSignature {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();

            var signatureUnlock = (STU_FA317B7D)unlock.STU;

            FindLogic.Combo.Find(info, signatureUnlock.m_83574299);
            FindLogic.Combo.Find(info, signatureUnlock.m_76820345);
            FindLogic.Combo.Find(info, signatureUnlock.m_9A95F791);
            FindLogic.Combo.Find(info, signatureUnlock.m_2F03889F); // todo: vector image.. not hooked up to combo
            FindLogic.Combo.Find(info, signatureUnlock.m_effect);
            FindLogic.Combo.Find(info, signatureUnlock.m_BFE64B3D);

            var context = new Combo.SaveContext(info);
            Combo.SaveLooseTextures(flags, directory, context);
            Combo.SaveAllMaterials(flags, directory, context);
            Combo.Save(flags, directory, context);
            
            // todo: need some way to split based off color schemes.. currently they all end up in the same folders
            // i still don't understand what each means
        }
    }
}
