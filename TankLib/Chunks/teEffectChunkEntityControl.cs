using System.IO;

namespace TankLib.Chunks {
    public class teEffectChunkEntityControl : IChunk {
        public string ID => "ECEC";
        
        // todo: these mean something
        // controlComponent->m_ejectInitialVelocity != (0xFFFF)
        // controlComponent->m_ejectRotationAxis != (0xFFFF)

        public void Parse(Stream stream) {
            throw new System.NotImplementedException();
        }
    }
}