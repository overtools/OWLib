using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.ExportFormats;
using TankLib.STU;
using static TankLib.CASC.ApplicationPackageManifest.Types;

namespace TankLibTest {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TestStruct {
        public ulong R1A;
        public ulong R1B;
        public ulong R1C;
        public ulong R1D;
        
        public ulong R2A;
        public ulong R2B;
        public ulong R2C;
        public ulong R2D;
        
        public ulong R3A;
        public ulong R3B;
        public ulong R3C;
        public ulong R3D;
        
        public ulong R4A;
        public ulong R4B;
        public ulong R4C;
        public ulong R4D;

        //public long A;
        //public sbyte B;
        //public sbyte C;
        //public long D;
        //public int E;
    }
    
    public class Program {
        public static Dictionary<ulong, PackageRecord> Files;
        public static Dictionary<ushort, HashSet<ulong>> Types;
        public static CASCConfig Config;
        public static CASCHandler CASC;
        
        public static string Language = "enUS";

        public static void Setup(string[] args) {
            string overwatchDir = args[0];
            
            Config = CASCConfig.LoadLocalStorageConfig(overwatchDir, false, false);
            Config.SpeechLanguage = Config.TextLanguage = Language;
        }

        public static void LoadCASC() {
            CASC = CASCHandler.Open(Config);
            
            MapCMF(Language);
        }
        
        public static void Main(string[] args) {
            Setup(args);
            //CASCHandler.Cache.CacheAPM = false;
            
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            LoadCASC();
            //stopwatch.Stop();
            //Console.Out.WriteLine(stopwatch.Elapsed);
            
            //TestBinarySpeed();
            TestString();
            //TestMaterial();
            //TestChunked();
            //TestTexture();
            //TestTexturePayload();
            //TestSTU();
            //TestAnimation();
            //TestSTUv1();
        }
        
        public static void TestBinarySpeed() {
            Stopwatch stopwatch = new Stopwatch();
            
            TestStruct testStruct = new TestStruct {
                //A = 34534534534,
                //B = 127,
                //C = -20,
                //D = long.MaxValue,
                //E = 98348344
                R1A = ulong.MaxValue,
                R1B = ulong.MaxValue,
                R1C = ulong.MaxValue,
                R1D = ulong.MaxValue,
                
                R2A = ulong.MaxValue,
                R2B = ulong.MaxValue,
                R2C = ulong.MaxValue,
                R2D = ulong.MaxValue,
                
                R3A = ulong.MaxValue,
                R3B = ulong.MaxValue,
                R3C = ulong.MaxValue,
                R3D = ulong.MaxValue,
                
                R4A = ulong.MaxValue,
                R4B = ulong.MaxValue,
                R4C = ulong.MaxValue,
                R4D = ulong.MaxValue
            };

            double fastWriterTime;
            double fastReaderTime;
            double fastWriteArray;
            double fastReadArray;

            double writerTime;
            double readerTime;

            //const long count = 11184810-1;
            const long count = 999999;  // for matrices
            //const long count = 1000;
            
            TestStruct[] array = new TestStruct[count];
            for (int i = 0; i < count; i++) {
                array[i] = testStruct;
            }

            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true)) {
                    writer.Write(testStruct);  // to setup static fields
                    stream.Position = 0;
                    
                    stopwatch.Restart();
                    for (int i = 0; i < count; i++) {
                        writer.Write(testStruct);
                    }
                    stopwatch.Stop();
                    fastWriterTime = stopwatch.Elapsed.TotalSeconds;
                    
                    stream.Position = 0;
                    stopwatch.Restart();
                    writer.WriteStructArray(array);
                    stopwatch.Stop();
                    fastWriteArray = stopwatch.Elapsed.TotalSeconds;
                }

                stream.Position = 0;
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, true)) {
                    stopwatch.Restart();
                    for (int i = 0; i < count; i++) {
                        reader.Read<TestStruct>();
                    }
                    stopwatch.Stop();
                    fastReaderTime = stopwatch.Elapsed.TotalSeconds;
                    
                    stream.Position = 0;
                    stopwatch.Restart();
                    reader.ReadArray<TestStruct>((int)count);
                    stopwatch.Stop();
                    fastReadArray = stopwatch.Elapsed.TotalSeconds;
                }

            }
            
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.Default, true)) {
                    stopwatch.Restart();
                    for (int i = 0; i < count; i++) {
                        writer.WriteOld(testStruct);
                    }
                    stopwatch.Stop();
                    writerTime = stopwatch.Elapsed.TotalSeconds;
                }

                stream.Position = 0;
                using (BinaryReader reader = new BinaryReader(stream)) {
                    stopwatch.Restart();
                    for (int i = 0; i < count; i++) {
                        reader.ReadOld<TestStruct>();
                    }
                    stopwatch.Stop();
                    readerTime = stopwatch.Elapsed.TotalSeconds;
                }
            }
            
            Console.Out.WriteLine($"Fast Writer: {fastWriterTime:F10} seconds");
            Console.Out.WriteLine($"Fast Reader: {fastReaderTime:F10} seconds");
            Console.Out.WriteLine($"Fast WriterArray: {fastWriteArray:F10} seconds");
            Console.Out.WriteLine($"Fast ReaderArray: {fastReadArray:F10} seconds");
            
            Console.Out.WriteLine($"Writer: {writerTime:F10}");
            Console.Out.WriteLine($"Reader: {readerTime:F10} seconds");
        }

        public static void TestAnimation() {
            using (Stream stream = OpenFile(0xA000000000042D9)) {  // ANCR_base_3p_ohHai_POTG
                teAnimation animation = new teAnimation(stream);
                
                SEAnim seAnim = new SEAnim(animation);

                using (Stream file = File.OpenWrite($"test.{seAnim.Extension}")) {
                    seAnim.Write(file);
                }
            }
        }

        public static void TestSTU() {
            var sw = new Stopwatch();

            const int iterateCount = 100000;

            using (Stream stuStream = OpenFile(0x980000000005632)) {  // 000000005632.01A
                {
                    // generate static fields
                    teStructuredData structuredData = new teStructuredData(stuStream, true);
                    stuStream.Position = 0;
                }
                sw.Restart();
                for (int i = 0; i < iterateCount; i++) {
                    teStructuredData structuredData = new teStructuredData(stuStream, true);
                    stuStream.Position = 0;
                }
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed}ms to deserialize the file using TankLib.STU.teStructuredData! ({iterateCount}x)");
                
                //sw.Restart();
                //for (int i = 0; i < iterateCount; i++) {
                //    ISTU structuredData = ISTU.NewInstance(stuStream, uint.MaxValue);
                //    stuStream.Position = 0;
                //}
                //sw.Stop();
                //Console.WriteLine($"Took {sw.Elapsed}ms to deserialize the file using STULib.V2! ({iterateCount}x)");
                
                
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
            teResourceGUID payloadGuid = (teResourceGUID)((guid & 0xF0FFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL);
            teTexture texture;
            using (Stream textureStream = OpenFile(guid)) {
                texture = new teTexture(textureStream);
            }
            using (Stream texturePayloadStream = OpenFile(payloadGuid)) {
                texture.LoadPayload(texturePayloadStream);
            }
            
            using (Stream file = File.OpenWrite(guid+".dds")) {
                texture.SaveToDDS(file);
            }
        }

        public static void TestTexture() {
            teResourceGUID guid = (teResourceGUID)0xC0000000000C708;
            using (Stream textureStream = OpenFile(guid)) {
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
            //using (Stream chunkedStream = OpenFile(0xD00000000004286)) {
            //    teChunkedData chunked = new teChunkedData(chunkedStream);
            //}
            using (Stream chunkedStream = OpenFile(0xD000000000053AF)) {  // orisa nyxl
                teChunkedData chunked = new teChunkedData(chunkedStream);
            }
            
            //var sw = new Stopwatch();
            //const long count = 100;
            //
            //using (Stream chunkedStream = OpenFile(0xD00000000004286)) {
            //    {
            //        // setup static
            //        teChunkedData chunked = new teChunkedData(chunkedStream, true);
            //        chunkedStream.Position = 0;
            //    }
            //    sw.Start();
            //    for (int i = 0; i < count; i++) {
            //        teChunkedData chunked = new teChunkedData(chunkedStream, true);
            //        chunkedStream.Position = 0;
            //    }
            //    sw.Stop();
            //}
            //Console.Out.WriteLine(sw.Elapsed);
        }

        public static void TestString() {
            using (Stream stringStream = OpenFile(0xDE0000000004A0E)) {
                teString @string = new teString(stringStream);
            }
        }

        public static void TestMaterial() {
            teMaterial material;
            teMaterialData materialData;
            
            LoadMaterial(0xE0000000000133E, out material, out materialData);
            //foreach (ulong guid in Types[0x8]) {
            //    teResourceGUID resourceGUID = (teResourceGUID) guid;
            //    LoadMaterial(guid, out material, out materialData);
            //    if (materialData.Header.Offset4 > 0) {
            //        SaveFile((ulong)material.Header.MaterialData, "MaterialDataUnknown4");
            //    }
            //}
        }

        public static void SaveFile(ulong guid, string category) {
            string directory = Path.Combine("output", category);
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            
            using (Stream stream = OpenFile(guid)) {
                using (Stream file = File.OpenWrite(Path.Combine(directory, teResourceGUID.AsString(guid)))) {
                    stream.CopyTo(file);
                }
            }
        }

        public static void LoadMaterial(ulong guid, out teMaterial material, out teMaterialData materialData) {
            using (Stream matStream = OpenFile(guid)) {
                material = new teMaterial(matStream);

                using (Stream matDataStream = OpenFile(material.Header.MaterialData)) {
                    materialData = new teMaterialData(matDataStream);
                }
            }
        }
        
        public static void MapCMF(string locale) {
            Files = new Dictionary<ulong, PackageRecord>();
            Types = new Dictionary<ushort, HashSet<ulong>>();
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                const string searchString = "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + locale.ToLowerInvariant())) {
                    continue;
                }

                //foreach (KeyValuePair<ulong,ContentManifestFile.HashData> data in apm.CMF.Map) {
                //    Files[data.Key] = new PackageRecord {
                //        Size = data.Value.Size,
                //        Flags = 0,
                //        GUID = data.Key,
                //        Hash = data.Value.HashKey,
                //        Offset = 0
                //    };
                //}
                foreach (KeyValuePair<ulong, PackageRecord> pair in apm.FirstOccurence) {
                    ushort type = teResourceGUID.Type(pair.Key);
                    if (!Types.ContainsKey(type)) {
                        Types[type] = new HashSet<ulong>();
                    }
                    
                    Types[type].Add(pair.Key);
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
            if (!CASC.EncodingHandler.GetEntry(record.LoadHash, out enc)) return null;

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
        
        public static string GetString(ulong guid) {
            try {
                return new teString(OpenFile(guid));
            } catch (Exception) {
                return null;
            }
        }
    }

    public static class Extensions {
        public static T ReadOld<T>(this BinaryReader reader) where T : struct {
            int size = Marshal.SizeOf<T>();
            byte[] buf = reader.ReadBytes(size);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buf, 0, ptr, size);
            T obj = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return obj;
        }
        public static void WriteOld<T>(this BinaryWriter writer, T obj) where T : struct {
            int size = Marshal.SizeOf<T>();
            byte[] buf = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, buf, 0, size);
            Marshal.FreeHGlobal(ptr);
            writer.Write(buf, 0, size);
        }
    }
}