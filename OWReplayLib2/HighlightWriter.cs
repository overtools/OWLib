using System.Collections.Generic;
using System.IO;
using OWReplayLib.Types;
using Highlight = OWReplayLib2.Types.Highlight;
using static OWReplayLib2.Helper;

namespace OWReplayLib2 {
    public class HighlightWriterDataStore {
        public OWReplayLib.Types.Highlight.HighlightFlags Flags;
        public List<HighlightInfoDataStore> Info;
        public List<Common.HeroInfo> HeroInfo;
        // todo fillers

        public class HighlightInfoDataStore {
            public string Name;
            public Highlight.HighlightInfo Actual;
            
            public HighlightInfoDataStore(Highlight.HighlightInfo info) {
                Actual = info;
            }
        }
    }
    
    public class HighlightWriter {
        public HighlightReader Reader;
        public Highlight.HighlightHeader Header;
        protected HighlightWriterDataStore Data;
        
        public HighlightWriter(HighlightReader reader) {
            Reader = reader;
            Header = reader.Header;  // cloned
            
            Data = new HighlightWriterDataStore();

            // Data.Info = Header.Info.ToList();
        }

        public MemoryStream Write() {
            return null;
        }

        public void SetPlayerName(string name) {
            foreach (HighlightWriterDataStore.HighlightInfoDataStore highlightInfoDataStore in Data.Info) {
                highlightInfoDataStore.Name = name;
            }
        }
    }
}