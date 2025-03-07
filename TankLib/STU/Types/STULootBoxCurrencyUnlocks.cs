// Generated by TankLibHelper
using TankLib.STU.Types.Enums;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x16F7C0EA, 24)]
    public class STULootBoxCurrencyUnlocks : STUInstance
    {
        [STUField(0xDB803F2F, 0, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STULootBoxCurrencyRarityUnlock[] m_unlocks;

        [STUField(0x7AB4E3F8, 16)] // size: 4
        public Enum_BABC4175 m_lootBoxType;
    }
}
