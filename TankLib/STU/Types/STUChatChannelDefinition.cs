// Generated by TankLibHelper
using TankLib.Math;
using TankLib.STU.Types.Enums;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x62D3E890, 48)]
    public class STUChatChannelDefinition : STUInstance
    {
        [STUField(0x52A8EBBB, 0)] // size: 16
        public teStructuredDataAssetRef<ulong> m_chatChannelName;

        [STUField(0x99745254, 16)] // size: 16
        public teStructuredDataAssetRef<STUSound> m_99745254;

        [STUField(0x2EBF6046, 32)] // size: 12
        public teColorRGB m_chatChannelColor;

        [STUField(0x9CB9B86D, 44)] // size: 4
        public STUChatChannelType m_chatChannelType;
    }
}
