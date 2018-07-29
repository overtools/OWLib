using System;
using System.Collections.Generic;
using DataTool.Flag;

namespace DataTool.SaveLogic.Unlock {
    public static class SprayAndIcon {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, List<DataModels.Unlock> items) {
            throw new NotImplementedException();
            //foreach (DataModels.Unlock item in items) {
            //    SaveItem(basePath, heroName, containerName, folderName, flags, item);
            //}
        }

        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, unlock.GUID);
            
            // hmm, resaving the default spray over and over again (ref'd by SSCE) is kinda bad.
            
            Combo.SaveLooseTextures(flags, directory, info);
            Combo.SaveAllMaterials(flags, directory, info);
            Combo.Save(flags, directory, info);
        }
    }
}