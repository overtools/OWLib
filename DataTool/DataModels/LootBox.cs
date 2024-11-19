using System.Collections.Generic;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    public class LootBox {
        public string NameFormat { get; set; }
        public string Type { get; set; }
        public Enum_BABC4175 LootBoxType { get; set; }
        public LootBoxShopCard[] ShopCards { get; set; }
        public bool HidePucks { get; set; }

        public LootBox(STULootBox lootBox) {
            Init(lootBox);
        }

        private void Init(STULootBox lootBox) {
            NameFormat = GetString(lootBox.m_name);
            Type = GetName(lootBox.m_lootBoxType);
            LootBoxType = lootBox.m_lootBoxType;

            HidePucks = lootBox.m_hidePucks == 1;

            if (lootBox.m_shopCards != null) {
                ShopCards = new LootBoxShopCard[lootBox.m_shopCards.Length];
                for (int i = 0; i < lootBox.m_shopCards.Length; i++) {
                    ShopCards[i] = new LootBoxShopCard(lootBox.m_shopCards[i]);
                }
            }
        }

        public static string GetName(uint type) {
            if (LootBoxNames.TryGetValue(type, out string lootboxName)) {
                return lootboxName;
            }

            return $"Unknown{type}";
        }

        public static string GetName(Enum_BABC4175 lootBoxType) {
            return GetName((uint) lootBoxType);
        }

        public static string GetBasicName(uint type) {
            return GetName(type).Replace(" ", "").ToLowerInvariant();
        }

        public static string GetBasicName(Enum_BABC4175 lootBoxType) {
            return GetBasicName((uint) lootBoxType);
        }

        private static readonly Dictionary<uint, string> LootBoxNames = new Dictionary<uint, string> {
            {0, "Base"},
            {1, "Summer Games"},
            {2, "Halloween"},
            {3, "Winter"},
            {4, "Lunar New Year"},
            {5, "Archives"},
            {6, "Anniversary"},
            {7, "Golden"},
            {8, "Internal"},
            {9, "Legendary Anniversary"},
            {10, "Hammond"},
            {12, "Legendary"}
        };
    }

    public class LootBoxShopCard {
        public string Text { get; set; }
        public teResourceGUID Texture { get; set; }

        public LootBoxShopCard(STULootBoxShopCard shopCard) {
            Text = GetString(shopCard.m_cardText);
            Texture = shopCard.m_cardTexture;
        }
    }
}
