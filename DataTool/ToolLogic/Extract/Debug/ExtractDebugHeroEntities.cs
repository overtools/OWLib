using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-heroent", Description = "Extract hero entities (debug)", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugHeroEntities : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ResearchHeroEntities(toolFlags);
        }

        public void ResearchHeroEntities(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                STUEntityDefinition def1 = GetInstance<STUEntityDefinition>(hero.EntityMain);
                STUEntityDefinition def2 = GetInstance<STUEntityDefinition>(hero.EntityHeroSelect);
                STUEntityDefinition def3 = GetInstance<STUEntityDefinition>(hero.EntityHighlightIntro);
                STUEntityDefinition def4 = GetInstance<STUEntityDefinition>(hero.EntityPlayable);
                STUEntityDefinition def5 = GetInstance<STUEntityDefinition>(hero.EntityThirdPerson);
            }
        }
    }
}