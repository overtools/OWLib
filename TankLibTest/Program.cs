using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using CASCLib;
using DataTool;
using DataTool.Helper;
using STULib;
using TankLib;
using TankLib.STU;
using TankLib.STU.DataTypes;
using TankLib.STU.Types;

namespace TankLibTest {
    internal class Program {
        public static Dictionary<ulong, MD5Hash> Files;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static CASCConfig Config;
        public static CASCHandler CASC;
        
        public static void Main(string[] args) {
            string overwatchDir = args[0];
            const string language = "enUS";

            // casc setup
            Config = CASCConfig.LoadLocalStorageConfig(overwatchDir, false, false);
            Config.Languages = new HashSet<string> {language};
            CASC = CASCHandler.OpenStorage(Config);
            DataTool.Program.Files = new Dictionary<ulong, MD5Hash>();
            DataTool.Program.TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            DataTool.Program.CASC = CASC;
            DataTool.Program.Root = CASC.Root as OwRootHandler;
            DataTool.Program.Flags = new ToolFlags {OverwatchDirectory = overwatchDir, Language = language};
            IO.MapCMF(true);
            Files = DataTool.Program.Files;
            TrackedFiles = DataTool.Program.TrackedFiles;
            
            //TestString();
            //TestMaterial();
            //TestChunked();
            //TestTexture();
            //TestTexturePayload();
            TestSTU();
        }

        public static void TestSTU() {
            var sw = new Stopwatch();

            const int iterateCount = 100000;

            using (Stream stuStream = IO.OpenFile(0x980000000005632)) {  // 000000005632.01A
                sw.Restart();
                for (int i = 0; i < iterateCount; i++) {
                    teStructuredData structuredData = new teStructuredData(stuStream);
                    stuStream.Position = 0;
                }
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed}ms to deserialize the file using TankLib.STU.teStructuredData! ({iterateCount}x)");
                
                sw.Restart();
                for (int i = 0; i < iterateCount; i++) {
                    ISTU structuredData = ISTU.NewInstance(stuStream, UInt32.MaxValue);
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
            using (Stream textureStream = IO.OpenFile((ulong) guid)) {
                texture = new teTexture(textureStream);
            }
            using (Stream texturePayloadStream = IO.OpenFile((ulong) payloadGuid)) {
                texture.LoadPayload(texturePayloadStream);
            }
            
            using (Stream file = File.OpenWrite(guid+".dds")) {
                texture.SaveToDDS(file);
            }
        }

        public static void TestTexture() {
            teResourceGUID guid = (teResourceGUID)0xC0000000000C708;
            using (Stream textureStream = IO.OpenFile((ulong) guid)) {
                teTexture texture = new teTexture(textureStream);
                using (Stream file = File.OpenWrite(guid+".dds")) {
                    texture.SaveToDDS(file);
                }
            }
        }

        public static void TestChunked() {
            // reaper eternal rest:
            using (Stream chunkedStream = IO.OpenFile(0x710000000000E54)) {
                teChunkedData chunked = new teChunkedData(chunkedStream);
            }
            
            // model:
            using (Stream chunkedStream = IO.OpenFile(0xD0000000000432A)) {
                teChunkedData chunked = new teChunkedData(chunkedStream);
            }
        }

        public static void TestString() {
            using (Stream stringStream = IO.OpenFile(0xDE000000000758E)) {
                teString @string = new teString(stringStream);
            }
        }

        public static void TestMaterial() {
            using (Stream matStream = IO.OpenFile(0xE0000000000133E)) {
                teMaterial material = new teMaterial(matStream);

                using (Stream matDataStream = IO.OpenFile((ulong) material.Header.MaterialData)) {
                    teMaterialData materialData = new teMaterialData(matDataStream);
                }
                
                
            }
        }
    }
}