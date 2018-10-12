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
        public void Parse(ICLIFlags toolFlags)
        {
            var flags = toolFlags as ExtractFlags;
            foreach (var guid in Program.TrackedFiles[0xA5])
            {
                SaveUnlock(guid);
                //try {
                //    Unlock unlock = new Unlock(guid);
                //    if (unlock.Name == "Supercharger") {
                //        
                //    }
                //} catch (NotImplementedException) { }
            }

            //const ulong guid = 0x250000000000F90;
            //SaveUnlock(guid);
        }

        private void SaveUnlock(ulong guid) {
            Unlock unlock;
            try {
                unlock = new Unlock(guid);
            } catch (NotImplementedException) {
                return;
            }
            
            STUUnlock_POTGAnimation potgAnim = unlock.STU as STUUnlock_POTGAnimation;
            if (potgAnim == null) return;
            if (potgAnim.m_animation == 0) return;
            //if (unlock.Name != "Selfie") return;
            SaveAnimation(Path.Combine(@"C:\ow\dump\1.28\effect", GetValidFilename(unlock.GetName())), potgAnim.m_animation);
        }

        private void SaveAnimation(string dir, ulong guid) {
            using (Stream animStream = OpenFile(guid)) {
                teAnimation animation = new teAnimation(animStream);

                if (animation.Header.Effect == 0) return;
                SaveEffect(dir, animation.Header.Effect);
            }
        }

        private void SaveEffect(string dir, ulong guid) {
            using (Stream stream = OpenFile(guid)) {
                teChunkedData chunkedData = new teChunkedData(stream);

                ulong lastModel = 0;

                foreach (IChunk chunk in chunkedData.Chunks) {
                    if (chunk is teEffectChunkShaderSetup shaderSetup) {
                        //if (teResourceGUID.Index(lastModel) != 0x296B) continue;  // the circle
                        
                        ExtractDebugShaders.SaveMaterial(dir, shaderSetup.Header.Material, GetFileName(guid));
                    }
                            
                    if (chunk is teEffectComponentParticle particle) {
                        lastModel = particle.Header.Model;
                    } else {
                        lastModel = 0;
                    }
                }

                foreach (teEffectComponentEntityControl entityControl in chunkedData.GetChunks<teEffectComponentEntityControl>()) {
                    if (entityControl.Header.Animation == 0) continue;
                    
                    SaveAnimation(dir, entityControl.Header.Animation);
                }

                foreach (teEffectComponentModel model in chunkedData.GetChunks<teEffectComponentModel>()) {
                    if (model.Header.Animation == 0) continue;
                    
                    SaveAnimation(dir, model.Header.Animation);
                }
            }
        }
    }
}
