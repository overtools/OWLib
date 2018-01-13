using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using BCFF;
using DataTool.Flag;
using DataTool.SaveLogic;
using OWLib;
using OWLib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using Texture = OWLib.Texture;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-thread-demo", Description = "Threading demo (debug)", TrackTypes = new ushort[] {0x4}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebubThreadDemo : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ThreadDemo(toolFlags);
        }

        public class ConvertTextureTaskWithData : ConvertTextureTask {
            public ConvertTextureTaskWithData(string path, ulong headerGUID, ulong dataGUID) : base(path, headerGUID) {
                DataGUID = dataGUID;

                RequiredCASCFiles.Add(HeaderGUID);
                RequiredCASCFiles.Add(DataGUID);
            }
        }

        public class ConvertTextureTask : WorkTask {
            protected ulong HeaderGUID;
            protected ulong DataGUID;
            protected readonly string Path;
            
            public ConvertTextureTask(string path, ulong headerGUID) {
                Path = path;
                HeaderGUID = headerGUID;
                
                RequiredCASCFiles.Add(HeaderGUID);
            }

            public override void Run(WorkerThread thread) {
                const string convertType = "tif";
                
                Stream headerStream = OpenFile(HeaderGUID);
                Stream dataStream = null;
                if (DataGUID != 0) {                
                    dataStream = OpenFile(DataGUID);
                }
                
                string filePath = System.IO.Path.Combine(Path, $"{GUID.Index(HeaderGUID)}");
                
                CreateDirectoryFromFile(filePath);

                TextureHeader header;
                Stream convertedStream;
                if (dataStream != null) {                
                    Texture textObj = new Texture(headerStream, dataStream);
                    convertedStream = textObj.Save();
                    header = textObj.Header;
                    headerStream.Dispose();
                    dataStream.Dispose();
                } else {
                    TextureLinear textObj = new TextureLinear(headerStream);
                    convertedStream = textObj.Save();
                    header = textObj.Header;
                    headerStream.Dispose();
                }
                uint fourCC = header.Format().ToPixelFormat().fourCC;
                bool isBcffValid = Combo.TextureConfig.DXGI_BC4.Contains((int) header.format) ||
                                   Combo.TextureConfig.DXGI_BC5.Contains((int) header.format) ||
                                   fourCC == Combo.TextureConfig.FOURCC_ATI1 || fourCC == Combo.TextureConfig.FOURCC_ATI2;

                ImageFormat imageFormat = null;

                if (convertType == "tif") imageFormat = ImageFormat.Tiff;
                
                convertedStream.Position = 0;
                
                if (isBcffValid && imageFormat != null && convertedStream.Length != 0) {
                    BlockDecompressor decompressor = new BlockDecompressor(convertedStream);
                    decompressor.CreateImage();
                    decompressor.Image.Save($"{filePath}.{convertType}", imageFormat);
                    return;
                }

                convertedStream.Position = 0;
                if (convertType == "tga" || convertType == "tif" || convertType == "dds") {
                    // we need the dds for tif conversion
                    WriteFile(convertedStream, $"{filePath}.dds");
                }

                convertedStream.Close();

                if (convertType != "tif" && convertType != "tga") return;
                Process pProcess = new Process {
                    StartInfo = {
                        FileName = "Third Party\\texconv.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        Arguments =
                            $"\"{filePath}.dds\" -y -wicmulti -nologo -m 1 -ft {convertType} -f R8G8B8A8_UNORM -o \"{Path}"
                    }
                };
                // -wiclossless?

                // erm, so if you add an end quote to this then it breaks.
                // but start one on it's own is fine (we need something for "Winged Victory")
                pProcess.Start();
                // pProcess.WaitForExit(); // not using this is kinda dangerous but I don't care
                // when texconv writes with to the console -nologo is has done/failed conversion
                
                // string line = pProcess.StandardOutput.ReadLine();
                // if (line?.Contains($"{filePath}.dds FAILED") == false) {
                    // fallback if convert fails
                File.Delete($"{filePath}.dds");
                // }
            }
        }

        public class WriteOWMatTaskFake : WorkTask {
            protected string Directory;
            protected readonly int ID;
            
            public WriteOWMatTaskFake(string directory, int id) {
                Directory = directory;
                ID = id;
            }
            
            public override void Run(WorkerThread thread) {
                Directory += "\\";
                CreateDirectoryFromFile(Directory);
                using (Stream stream = File.OpenWrite(Directory + ID + ".txt")) {
                    stream.SetLength(0);
                    for (int i = 0; i < 10000; i++) {
                        stream.WriteByte(0xFF);
                    }
                }
                // sample writer thing
            }
        }

        public class FakeTaskA : WorkTask {
            private const ulong Thing = 684547143360330575;
            public FakeTaskA() {
                RequiredCASCFiles.Add(Thing);
            }
            
            public override void Run(WorkerThread thread) {
                Stream stream = thread.CASCStreams[Thing];
                Thread.Sleep(500);  // do da work
                Console.Out.WriteLine("done A");
            }
        }
        
        public class FakeTaskB : WorkTask {
            public override void Run(WorkerThread thread) {
                Thread.Sleep(500);  // do da work
                Console.Out.WriteLine("done B");
            }
        }

        public void ThreadDemo(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            const string container = "DebugThreadDemo";

            string path = Path.Combine(basePath, container);
            /*ThreadProvider provider = new ThreadProvider();

            for (int i = 0; i < 8; i++) {
                provider.AddTask(new FakeTaskA());
            }
            
            for (int i = 0; i < 8; i++) {
                provider.AddTask(new FakeTaskB());
            }
            
            provider.Run(4);
            provider.Wait();*/
            
            ThreadProvider provider = new ThreadProvider();

            const int max = 2000;
            
            int i = 0;
            
            HashSet<WorkTask> tasks = new HashSet<WorkTask>();
            foreach (ulong key in TrackedFiles[0x4]) {
                ulong dataKey = (key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
                if (Files.ContainsKey(dataKey)) {
                    tasks.Add(new ConvertTextureTaskWithData(path, key, dataKey));
                    tasks.Add(new WriteOWMatTaskFake(path, i));
                } else {
                    tasks.Add(new ConvertTextureTask(path, key));
                    tasks.Add(new WriteOWMatTaskFake(path, i));
                }

                if (i == max) break;
                
                i++;
            }

            foreach (WorkTask workTask in tasks) {
                provider.AddTask(workTask);
            }

            // without threads
            // {
            //     foreach (WorkTask workTask in tasks) {
            //         workTask.DynamicallyAssigned = true;
            //         workTask.CASCStreams = new ConcurrentDictionary<ulong, Stream>();
            //         foreach (ulong requiredCASCFile in workTask.RequiredCASCFiles) {
            //             Stream stream = provider.GetStream(requiredCASCFile);
            //             workTask.CASCStreams[requiredCASCFile] = stream;
            //         }
            //         workTask.Run(null);
            //     }
            // }
            // return;
            
            provider.Run(5);
            provider.Wait();
        }
    }
}