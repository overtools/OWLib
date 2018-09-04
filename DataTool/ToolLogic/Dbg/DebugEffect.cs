using System;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using DataTool.ToolLogic.Extract.Debug;
using TankLib;
using TankLib.Chunks;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Dbg
{
    [Tool("debug-effect", Description = "", IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    class DebugEffect : ITool
    {
        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            var flags = toolFlags as ExtractFlags;
            // foreach (var guid in Program.TrackedFiles[0xA5])
            // {
            //     try {
            //         Unlock unlock = new Unlock(guid);
            //         if (unlock.Name == "Supercharger") {
            //             
            //         }
            //     } catch (NotImplementedException) { }
            // }

            const ulong guid = 0x250000000000F90;
            Unlock unlock = new Unlock(guid);

            STUUnlock_POTGAnimation potgAnim = (STUUnlock_POTGAnimation)unlock.STU;
            SaveAnimation(potgAnim.m_animation);
        }

        private void SaveAnimation(ulong guid) {
            using (Stream animStream = OpenFile(guid)) {
                teAnimation animation = new teAnimation(animStream);

                if (animation.Header.Effect != 0) {
                    SaveEffect(animation.Header.Effect);
                }
            }
        }

        private void SaveEffect(ulong guid) {
            using (Stream stream = OpenFile(guid)) {
                teChunkedData chunkedData = new teChunkedData(stream);

                ulong lastModel = 0;

                foreach (IChunk chunk in chunkedData.Chunks) {
                    if (chunk is teEffectChunkShaderSetup shaderSetup) {
                        if (teResourceGUID.Index(lastModel) != 0x296B) continue;  // the circle
                        
                        ExtractDebugShaders.SaveMaterial(@"C:\ow\dump\1.27\effect", shaderSetup.Header.Material, GetFileName(guid));
                    }
                            
                    if (chunk is teEffectComponentParticle particle) {
                        lastModel = particle.Header.Model;
                    } else {
                        lastModel = 0;
                    }
                }

                foreach (teEffectComponentModel model in chunkedData.GetChunks<teEffectComponentModel>()) {
                    if (model.Header.Animation == 0) continue;
                    
                    SaveAnimation(model.Header.Animation);
                }
            }
        }
    }
}
