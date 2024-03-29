// Generated by TankLibHelper
using TankLib.Math;

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0x64100592, 48)]
    public class STUTargetTagInstanceData : STUComponentInstanceData
    {
        [STUField(0xAA8E1BB0, 8)] // size: 16
        public teStructuredDataAssetRef<STUTargetTag>[] m_targetTags;

        [STUField(0x2746D7E4, 24)] // size: 16
        public teUUID m_2746D7E4;
    }
}
