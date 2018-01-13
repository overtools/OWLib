// using STULib.Types.prehash;
using System;
using System.Collections.Generic;
using DataTool.Flag;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.Statescript.Components;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
// using STUAnimBlendTreeSet = STULib.Types.prehash.STUAnimBlendTreeSet;
// using STUAnimBlendTreeSet_BlendTreeItem = STULib.Types.prehash.STUAnimBlendTreeSet_BlendTreeItem;

namespace DataTool.ToolLogic.List.Debug {
    [Tool("list-debug-anim-blend-trees", Description = "List anim blend trees (debug)", TrackTypes = new ushort[] {0x21}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListDebugAnimBlendTrees : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ListAnimBlendTrees();
        }

        public void ListAnimBlendTrees() {
            List<ulong> blendTreeSets = new List<ulong>();
            
            // this is posthash
            foreach (ulong heroKey in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(heroKey);
                if (GetString(hero?.Name) != "Tracer") continue;
                foreach (Common.STUGUID heroEntity in new [] {hero.EntityHeroSelect, hero.EntityHighlightIntro, hero.EntityMain, hero.EntityPlayable, hero.EntityThirdPerson}) {
                    STUModelComponent modelComponent = GetInstance<STUModelComponent>(heroEntity);
                    if (modelComponent?.AnimBlendTreeSet == null) continue;
                    blendTreeSets.Add(modelComponent.AnimBlendTreeSet);
                }
            }
            
            // add breakpoint and copy from posthash thing (yes I'm sorry)
            // blendTreeSets = new List<ulong> {
            //     18014398509483732, 18014398509483760, 18014398509483769, 18014398509483562, 18014398509483771  // orisa
            // };
            blendTreeSets = new List<ulong> {
                18014398509482072, 18014398509483458, 18014398509483479, 18014398509481986, 18014398509483575  // tracer
            };
            
            // this is prehash
            // foreach (ulong key in TrackedFiles[0x21]) {
            //     if (!blendTreeSets.Contains(key)) continue;
            //     STUAnimBlendTreeSet animBlendTreeSet = GetInstance<STUAnimBlendTreeSet>(key);
            //     
            //     if (animBlendTreeSet?.BlendTreeItems == null) continue;
            //     foreach (STUAnimBlendTreeSet_BlendTreeItem blendTreeItem in animBlendTreeSet.BlendTreeItems) {
            //         if (blendTreeItem.m_45C6E995 == null) continue;
            //         STUAnimNode_Strafe8Way strafe8Way = GetInstance<STUAnimNode_Strafe8Way>(blendTreeItem.m_45C6E995);
            //         if (strafe8Way != null) Debugger.Break();
            //     }
            // }
        }
    }
}