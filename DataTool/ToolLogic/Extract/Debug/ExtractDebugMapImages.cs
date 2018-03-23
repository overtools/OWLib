using System;
using System.IO;
using System.Runtime.InteropServices;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-map-images", Description = "Extract map images (debug)", TrackTypes = new ushort[] {0x2}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMapImages : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }
        
        public struct Map {
            public Matrix3x4B Matrix3X4a;
            public Matrix4B Matrix4A;
            public Matrix4B Matrix4B;
            public Matrix4B Matrix4C;
            public ulong Entity;
            public ulong Texture;    // 004
            public ulong Lighting;  // 0BD
            public ulong Shadow;     // 0CB
            public ulong TextureA;    // 004
            public ulong Skybox;     // 00C
            public ulong ModelLook;   // 01A
            public ulong TextureB;    // 004
            public ulong TextureC;    // 004
            public ulong String;     // 07C
            public ulong Unknown;    // 0B5
            public Matrix4B Matrix4D;
            public ulong TextureD;    // 004
            public ulong TextureE;    // 004
            public ulong TextureF;    // 004
            public Matrix3x4B Matrix3X4b;
            public Matrix3x4B Matrix3X4c;
            //int[12]   counts;     // ?
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractMapImages(toolFlags);
        }

        public void ExtractMapImages(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            //flags.ConvertTexturesType = "dds";

            const string container = "DebugMapImages";
            
            foreach (ulong key in TrackedFiles[0x2]) {
                string dir = Path.Combine(basePath, container, GetFileName(key));
                Combo.ComboInfo info = new Combo.ComboInfo();
                
                using (Stream stream = OpenFile(key)) {
                    using (Stream file = File.OpenWrite(Path.Combine(dir, "data.002"))) {
                        stream.CopyTo(file);
                        stream.Position = 0;
                    }
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        Map map = reader.Read<Map>();
                        
                        Combo.Find(info, map.Texture);
                        info.SetTextureName(map.Texture, nameof(Map.Texture));
                        
                        Combo.Find(info, map.TextureB);
                        info.SetTextureName(map.TextureB, nameof(Map.TextureB));
                        
                        Combo.Find(info, map.TextureC);
                        info.SetTextureName(map.TextureC, nameof(Map.TextureC));
                        
                        Combo.Find(info, map.TextureD);
                        info.SetTextureName(map.TextureD, nameof(Map.TextureD));
                        
                        Combo.Find(info, map.TextureE);
                        info.SetTextureName(map.TextureE, nameof(Map.TextureE));
                        
                        Combo.Find(info, map.TextureF);
                        info.SetTextureName(map.TextureF, nameof(Map.TextureF));
                    }
                    
                    SaveLogic.Combo.SaveLooseTextures(flags, dir, info);
                }
            }
        }
    }
}