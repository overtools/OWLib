using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TankLib.CASC;
using TankLib.CASC.Handlers;

namespace TankLibTestCASC {
    internal class Program {
        public static void Main(string[] args) {
            const string locale = "enUS";
            
            CASCConfig config = CASCConfig.LoadLocalStorageConfig(args[0], true, false);
            config.Languages = new HashSet<string> {locale};
            CASCHandler handler = CASCHandler.Open(config);
            
            Dictionary<ulong, MD5Hash> files = new Dictionary<ulong, MD5Hash>();
            foreach (ApplicationPackageManifest apm in handler.RootHandler.APMFiles) {
                const string searchString = "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + locale.ToLowerInvariant())) {
                    continue;
                }
                foreach (KeyValuePair<ulong, CMFHashData> pair in apm.CMF.Map) {
                    files[pair.Value.id] = pair.Value.HashKey;
                }
            }

            using (Stream stream = OpenFile(handler, files[0x980000000005632])) {
                
            }
        }
        
        public static Stream OpenFile(CASCHandler casc, MD5Hash hash) {
            try {
                return casc.EncodingHandler.GetEntry(hash, out EncodingEntry enc) ? casc.OpenFile(enc.Key) : null;
            }
            catch (Exception e) {
                if (e is BLTEKeyException exception) {
                    Debugger.Log(0, "DataTool", $"[DataTool:CASC]: Missing key: {exception.MissingKey:X16}\r\n");
                }
                return null;
            }
        }
    }
}