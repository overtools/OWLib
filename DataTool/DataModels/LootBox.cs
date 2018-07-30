using System.Collections.Generic;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using Newtonsoft.Json;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class LootBox {
        public string Name;
        public Enum_BABC4175 LootBoxType;

        public LootBoxShopCard[] ShopCards;

        public bool HidePucks;

        public LootBox(STULootBox lootBox) {
            Init(lootBox);
        }

        private void Init(STULootBox lootBox) {
            Name = GetName(lootBox.m_lootboxType);
            LootBoxType = lootBox.m_lootboxType;

            HidePucks = lootBox.m_hidePucks == 1;

            if (lootBox.m_shopCards != null) {
                ShopCards = new LootBoxShopCard[lootBox.m_shopCards.Length];
                for (int i = 0; i < lootBox.m_shopCards.Length; i++) {
                    ShopCards[i] = new LootBoxShopCard(lootBox.m_shopCards[i]);
                }
            }
        }

        public static string GetName(uint type) {
            if (_lootBoxNames.TryGetValue(type, out string lootboxName)) {
                return lootboxName;
            }
            return $"Unknown{type}";
        }

        public static string GetName(Enum_BABC4175 lootBoxType) {
            return GetName((uint)lootBoxType);
        }
        
        private static readonly Dictionary<uint, string> _lootBoxNames = new Dictionary<uint, string> {
            {0, "Base"},
            {1, "Summer Games"},
            {2, "Halloween"},
            {3, "Winter"},
            {4, "Lunar New Year"},
            {5, "Archives"},
            {6, "Anniversary"},
            {7, "Golden"},
            {8, "Legendary Anniversary"},
            {9, "Hammond"}
        };
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class LootBoxShopCard {
        public string Text;
        [JsonConverter(typeof(GUIDConverter))]
        public teResourceGUID Texture;

        public LootBoxShopCard(STULootBoxShopCard shopCard) {
            Text = GetString(shopCard.m_cardText);
            Texture = shopCard.m_cardTexture;
        }
    }
}