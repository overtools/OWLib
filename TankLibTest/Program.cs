using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using STULib;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.STU;
using static TankLib.CASC.ApplicationPackageManifest.Types;

namespace TankLibTest {
    internal class Program {
        public static Dictionary<ulong, PackageRecord> Files;
        public static CASCConfig Config;
        public static CASCHandler CASC;
        
        public static void Main(string[] args) {
            string overwatchDir = args[0];
            const string language = "enUS";

            // casc setup
            Config = CASCConfig.LoadLocalStorageConfig(overwatchDir, false, false);
            Config.Languages = new HashSet<string> {language};
            //ApplicationPackageManifest.USE_CACHE = false;
            CASC = CASCHandler.Open(Config);
            
            MapCMF(language);
            
            Console.Out.WriteLine(TelemetryHandler.GetDeviceModel());
            Console.Out.WriteLine(TelemetryHandler.GetWindowsFriendlyName());
            Console.Out.WriteLine(TelemetryHandler.GetDeviceManufacturer());
            Console.Out.WriteLine(TelemetryHandler.GetComponentVersion());
            
            //Telemetry.Init("0", "0");
            //Telemetry.SetTelementryEnabled(true);
            //Telemetry.TrackEvent("hello");
            //Telemetry.Flush();
            
            TestString();
            //TestMaterial();
            //TestChunked();
            //TestTexture();
            //TestTexturePayload();
            //TestSTU();
            //TestAnimation();
        }

        public static void TestAnimation() {
            using (Stream stream = OpenFile(0xA000000000042D9)) {
                teAnimation animation = new teAnimation(stream);
            }
        }

        public static void TestSTU() {
            var sw = new Stopwatch();

            const int iterateCount = 100000;

            using (Stream stuStream = OpenFile(0x980000000005632)) {  // 000000005632.01A
                sw.Restart();
                for (int i = 0; i < iterateCount; i++) {
                    teStructuredData structuredData = new teStructuredData(stuStream);
                    stuStream.Position = 0;
                }
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed}ms to deserialize the file using TankLib.STU.teStructuredData! ({iterateCount}x)");
                
                sw.Restart();
                for (int i = 0; i < iterateCount; i++) {
                    ISTU structuredData = ISTU.NewInstance(stuStream, uint.MaxValue);
                    stuStream.Position = 0;
                }
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed}ms to deserialize the file using STULib.V2! ({iterateCount}x)");
                
                
                //STUModelLook look = structuredData.GetMainInstance<STUModelLook>();
                //foreach (STUModelMaterial lookMaterial in look.Materials) {
                //    ulong guid = lookMaterial.Material;
                //}
            }
            Console.Out.Write("done");
            Console.ReadLine();
        }
        
        public static void TestTexturePayload() {
            teResourceGUID guid = (teResourceGUID)0xC00000000000A00;
            teResourceGUID payloadGuid = (teResourceGUID)(((ulong)guid & 0xF0FFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL);
            teTexture texture;
            using (Stream textureStream = OpenFile((ulong) guid)) {
                texture = new teTexture(textureStream);
            }
            using (Stream texturePayloadStream = OpenFile((ulong) payloadGuid)) {
                texture.LoadPayload(texturePayloadStream);
            }
            
            using (Stream file = File.OpenWrite(guid+".dds")) {
                texture.SaveToDDS(file);
            }
        }

        public static void TestTexture() {
            teResourceGUID guid = (teResourceGUID)0xC0000000000C708;
            using (Stream textureStream = OpenFile((ulong) guid)) {
                teTexture texture = new teTexture(textureStream);
                using (Stream file = File.OpenWrite(guid+".dds")) {
                    texture.SaveToDDS(file);
                }
            }
        }

        public static void TestChunked() {
            // reaper eternal rest:
            //using (Stream chunkedStream = IO.OpenFile(0x710000000000E54)) {
            //    teChunkedData chunked = new teChunkedData(chunkedStream);
            //}
            
            // model:
            using (Stream chunkedStream = OpenFile(0xD00000000004286)) {
                teChunkedData chunked = new teChunkedData(chunkedStream);
            }
        }

        public static void TestString() {
            using (Stream stringStream = OpenFile(0xDE000000000758E)) {
                teString @string = new teString(stringStream);
            }
        }

        public static void TestMaterial() {
            using (Stream matStream = OpenFile(0xE0000000000133E)) {
                teMaterial material = new teMaterial(matStream);

                using (Stream matDataStream = OpenFile((ulong) material.Header.MaterialData)) {
                    teMaterialData materialData = new teMaterialData(matDataStream);
                }
            }
        }
        
        
        public static void MapCMF(string locale) {
            Files = new Dictionary<ulong, PackageRecord>();
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                const string searchString = "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + locale.ToLowerInvariant())) {
                    continue;
                }
                foreach (KeyValuePair<ulong, PackageRecord> pair in apm.FirstOccurence) {
                    Files[pair.Value.GUID] = pair.Value;
                }
            }
        }
        
        public static Stream OpenFile(ulong guid) {
            return OpenFile(Files[guid]);
        }

        public static Stream OpenFile(PackageRecord record) {
            long offset = 0;
            EncodingEntry enc;
            if (record.Flags.HasFlag(ContentFlags.Bundle)) offset = record.Offset;
            if (!CASC.EncodingHandler.GetEntry(record.Hash, out enc)) return null;

            MemoryStream ms = new MemoryStream((int) record.Size);
            try {
                Stream fstream = CASC.OpenFile(enc.Key);
                fstream.Position = offset;
                fstream.CopyBytes(ms, (int) record.Size);
                ms.Position = 0;
            } catch (Exception e) {
                if (e is BLTEKeyException exception) {
                    Debugger.Log(0, "DataTool", $"[DataTool:CASC]: Missing key: {exception.MissingKey:X16}\r\n");
                }

                return null;
            }

            return ms;
        }
    }
}