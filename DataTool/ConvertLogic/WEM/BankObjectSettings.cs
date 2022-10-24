using System.Collections.Generic;
using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(1)]
    public class BankObjectSettings : IBankObject {
        public enum SettingType : byte {
            VoiceVolume = 1,
            VoiceLowPassFilter = 3
        }

        public List<KeyValuePair<SettingType, float>> Settings;

        public void Read(BinaryReader reader) {
            byte numSettings = reader.ReadByte();

            SettingType[] types = new SettingType[numSettings];
            for (int i = 0; i < numSettings; i++) {
                SettingType type = (SettingType) reader.ReadByte();
                types[i] = type;
            }

            Settings = new List<KeyValuePair<SettingType, float>>();

            foreach (SettingType settingType in types) {
                float value = reader.ReadSingle();
                Settings.Add(new KeyValuePair<SettingType, float>(settingType, value));
            }
        }
    }
}