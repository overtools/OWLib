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
            Name = ExtractHeroUnlocks.GetLootBoxName(lootBox.m_lootboxType);
            LootBoxType = lootBox.m_lootboxType;

            HidePucks = lootBox.m_hidePucks == 1;

            if (lootBox.m_shopCards != null) {
                ShopCards = new LootBoxShopCard[lootBox.m_shopCards.Length];
                for (int i = 0; i < lootBox.m_shopCards.Length; i++) {
                    ShopCards[i] = new LootBoxShopCard(lootBox.m_shopCards[i]);
                }
            }
        }
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