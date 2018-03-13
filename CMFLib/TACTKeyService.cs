using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using CMFLib.Crypto;

namespace CMFLib {
    public class TACTKeyService {
        private static Dictionary<ulong, byte[]> LoadKeys() {
            Dictionary<ulong, byte[]> dict = new Dictionary<ulong, byte[]> {
                // todo: reorder and stuff
                // hardcoded:
                [0xFB680CB6A8BF81F3] = "62D90EFA7F36D71C398AE2F1FE37BDB9".ToByteArray(),
                [0x402CD9D8D6BFED98] = "AEB0EADEA47612FE6C041A03958DF241".ToByteArray(),
                // unk usage:
                [0xDBD3371554F60306] = "34E397ACE6DD30EEFDC98A2AB093CD3C".ToByteArray(),
                [0x11A9203C9881710A] = "2E2CB8C397C2F24ED0B5E452F18DC267".ToByteArray(),
                [0xA19C4F859F6EFA54] = "0196CB6F5ECBAD7CB5283891B9712B4B".ToByteArray(),
                [0x87AEBBC9C4E6B601] = "685E86C6063DFDA6C9E85298076B3D42".ToByteArray(),
                [0xDEE3A0521EFF6F03] = "AD740CE3FFFF9231468126985708E1B9".ToByteArray(),
                [0x8C9106108AA84F07] = "53D859DDA2635A38DC32E72B11B32F29".ToByteArray(),
                [0x49166D358A34D815] = "667868CD94EA0135B9B16C93B1124ABA".ToByteArray(),
                [0x1463A87356778D14] = "69BD2A78D05C503E93994959B30E5AEC".ToByteArray(),
                [0x5E152DE44DFBEE01] = "E45A1793B37EE31A8EB85CEE0EEE1B68".ToByteArray(),
                [0x9B1F39EE592CA415] = "54A99F081CAD0D08F7E336F4368E894C".ToByteArray(),
                [0x24C8B75890AD5917] = "31100C00FDE0CE18BBB33F3AC15B309F".ToByteArray(),
                [0xEA658B75FDD4890F] = "DEC7A4E721F425D133039895C36036F8".ToByteArray(),
                [0x026FDCDF8C5C7105] = "8F41809DA55366AD416D3C337459EEE3".ToByteArray(),
                [0xCAE3FAC925F20402] = "98B78E8774BF275093CB1B5FC714511B".ToByteArray(),
                
                // known:
                [0x57A5A33B226B8E0A] = "FDFC35C99B9DB11A326260CA246ACB41".ToByteArray(),  // Ana
                [0x42B9AB1AF5015920] = "C68778823C964C6F247ACC0F4A2584F8".ToByteArray(),  // Summer Games 2016
                [0x061581CA8496C80C] = "DA2EF5052DB917380B8AA6EF7A5F8E6A".ToByteArray(),  // < 1.1.0.0
                [0xBE2CB0FAD3698123] = "902A1285836CE6DA5895020DD603B065".ToByteArray(),  // < 1.1.0.0
                [0x4F0FE18E9FA1AC1A] = "89381C748F6531BBFCD97753D06CC3CD".ToByteArray(),  // 1.2.0.1
                [0x7758B2CF1E4E3E1B] = "3DE60D37C664723595F27C5CDBF08BFA".ToByteArray(),  // 1.2.0.1
                [0xE5317801B3561125] = "7DD051199F8401F95E4C03C884DCEA33".ToByteArray(),  // Halloween Terror
                [0x16B866D7BA3A8036] = "1395E882BF25B481F61A4D621141DA6E".ToByteArray(),  // Bastion Blizzcon 2016
                [0x11131FFDA0D18D30] = "C32AD1B82528E0A456897B3CE1C2D27E".ToByteArray(),  // Sombra
                [0xCAC6B95B2724144A] = "73E4BEA145DF2B89B65AEF02F83FA260".ToByteArray(),  // Ecopoint: Antarctica
                [0xB7DBC693758A5C36] = "BC3A92BFE302518D91CC30790671BF10".ToByteArray(),  // Genji Oni
                [0x90CA73B2CDE3164B] = "5CBFF11F22720BACC2AE6AAD8FE53317".ToByteArray(),  // Oasis
                [0x6DD3212FB942714A] = "E02C1643602EC16C3AE2A4D254A08FD9".ToByteArray(),  // 1.6.1.0
                [0x11DDB470ABCBA130] = "66198766B1C4AF7589EFD13AD4DD667A".ToByteArray(),  // Winter Wonderland 2016
                [0x5BEF27EEE95E0B4B] = "36BCD2B551FF1C84AA3A3994CCEB033E".ToByteArray(),  // 1.6.1.0
                [0x9359B46E49D2DA42] = "173D65E7FCAE298A9363BD6AA189F200".ToByteArray(),  // Diablo 20th Anniversary
                [0x1A46302EF8896F34] = "8029AD5451D4BC18E9D0F5AC449DC055".ToByteArray(),  // Lunar New Year (Rooster)
                [0x693529F7D40A064C] = "CE54873C62DAA48EFF27FCC032BD07E3".ToByteArray(),  // CTF
                [0xE218F69AAC6C104D] = "F43D12C94A9A528497971F1CBE41AD4D".ToByteArray(),  // Orisa
                [0xF432F0425363F250] = "BA69F2B33C2768F5F29BFE78A5A1FAD5".ToByteArray(),  // Uprising
                [0xBD4E42661A432951] = "6DE8E28C8511644D5595FC45E5351472".ToByteArray(),  // Anniversary 2017
                [0xC43CB14355249451] = "0EA2B44F96A269A386856D049A3DEC86".ToByteArray(),  // Horizon Lunar Colony
                [0x388B85AEEDCB685D] = "D926E659D04A096B24C19151076D379A".ToByteArray(),  // Doomfist teaser Numbani map 
                [0x061D52F86830B35D] = "D779F9C6CC9A4BE103A4E90A7338F793".ToByteArray(),  // D.Va officer
                [0x1275C84CF113EF65] = "CF58B6933EAF98AF53E76F8426CC7E6C".ToByteArray(),  // < 1.11.0.0
                [0xD9C7C7AC0F14C868] = "3AFDF68E3A5D63BABA1E6821883F067D".ToByteArray(),  // < 1.11.0.0
                [0xE6D914F8E4744953] = "C8477C289DCE66D9136507A33AA33301".ToByteArray(),  // Doomfist
                [0x5694C503F8C80178] = "7F4CF1C1FBBAD92B184336D677EBF937".ToByteArray(),  // Doomfist Cosmetics
                [0x21DBFD65F3E54269] = "AB580C3837CAF8A461F243A566B2AE4D".ToByteArray(),  // Summer Games 2017
                [0x21E1F90E71D33C71] = "328742339162B32676C803C2255931A6".ToByteArray(),  // (Team) Deathmatch
                [0xD9CB055BCDD40B6E] = "49FB4477A4A0825327E9A73682BECD0C".ToByteArray(),  // Junkertown
                [0x8175CE3C694C6659] = "E3F3FA7726C70D26AE130D969DDDF399".ToByteArray(),  // Halloween 2017
                [0xB8DE51690075435A] = "C07E9260BB711217E7DE6FED911F4296".ToByteArray(),  // Blizzcon 2017 Winston
                [0xF6CF23955B5D437D] = "AEBA227328A5B0AA9F51DAE3F6A7DFE4".ToByteArray(),  // Moira
                [0x0E4D9426F2891F5C] = "9FF064C38BE52CCDF73748180F628205".ToByteArray(),  // Winter Wonderland 2017
                [0x9240BA6A2A0CF684] = "DF2E37D78B43108FA6242068B70D1F65".ToByteArray(),  // Overwatch League
                [0x9ADF00AA1A174A69] = "9A4AC899261A2F1C6969F39397C358E7".ToByteArray(),  // Blizzard World
                [0x82297FBAB7F5EB80] = "B534C20965852FB15AECAC17E381B417".ToByteArray(),  // Jan 2018 cosmetics
                [0xCFA05AA76B49F881] = "526DDDEF19BF373C25B629A334CD7237".ToByteArray(),  // WoW Battle for Azeroth preorder
                [0x8162E5313A9C135D] = "F407834D9521587C5012B0A59D7E064B".ToByteArray(),  // Lunar New Year (Dog) + Ayutthaya
                [0x493455579DA0B18E] = "C0BABF72AD2C05DFC14017D1ADBF5977".ToByteArray(),  // OWL Inaugral Season Spray+Icon
                [0x6362C5AD65DAE686] = "62F603D5390F763ED51773F0164FEDB5".ToByteArray(),  // OWL Twitch Cheering skins
                [0xF412C6327C4BF091] = "6FAFC648CBF1C2115B769593C170E732".ToByteArray(),  // Kerrigan Widowmaker
            };

            if (!File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.keys"))
                return dict;
            using (Stream f = File.OpenRead(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ow.keys")) {
                using (TextReader r = new StreamReader(f)) {
                    string line;
                    while ((line = r.ReadLine()) != null) {
                        line = line.Trim().Split(new[] {'#'}, StringSplitOptions.None)[0].Trim();
                        if (string.IsNullOrWhiteSpace(line)) {
                            continue;
                        }
                        string[] c = line.Split(new char[1] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        if (c.Length < 2) {
                            continue;
                        }
                        bool enabled = true;
                        if (c.Length >= 3) {
                            enabled = c[2] == "1";
                        }

                        ulong v;
                        try {
                            v = ulong.Parse(c[0], NumberStyles.HexNumber);
                        } catch {
                            continue;
                        }
                        if (enabled && !dict.ContainsKey(v)) {
                            dict.Add(v, c[1].ToByteArray());
                        } else {
                            if (dict.ContainsKey(v)) {
                                dict.Remove(v);
                            }
                        }
                            
                    }
                }
            }

            return dict;
        }

        public static Dictionary<ulong, byte[]> Keys = LoadKeys();

        public static byte[] GetKey(ulong keyName) {
            Keys.TryGetValue(keyName, out byte[] key);
            return key;
        }

        public static Salsa20 SalsaInstance = new Salsa20();
    }
}