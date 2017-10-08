using System;
using System.Collections.Generic;
using System.Diagnostics;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Helper.STUHelper;
using OWLib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;

namespace DataTool.FindLogic {
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class TextureInfo : IEquatable<TextureInfo> {
        public Common.STUGUID GUID;
        public Common.STUGUID DataGUID;
        internal string DebuggerDisplay => $"{GUID.ToString()}{(DataGUID != null ? $" - {DataGUID.ToString()}" : "")}";

        public bool Equals(TextureInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GUID, other.GUID) && Equals(DataGUID, other.DataGUID);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TextureInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((GUID != null ? GUID.GetHashCode() : 0) * 397) ^ (DataGUID != null ? DataGUID.GetHashCode() : 0);
            }
        }
    }
    
    public static class Texture {
        public static void AddGUID(Dictionary<ulong, List<TextureInfo>> textures, Common.STUGUID mainKey, Common.STUGUID dataKey, ulong parentKey) {
            if (mainKey == null) return;
            if (!textures.ContainsKey(parentKey)) {
                textures[parentKey] = new List<TextureInfo>();
            }

            TextureInfo newTexture = new TextureInfo {GUID = mainKey, DataGUID = dataKey};

            if (!textures[parentKey].Contains(newTexture)) {
                textures[parentKey].Add(newTexture);
            }
        }
        
        public static Dictionary<ulong, List<TextureInfo>> FindTextures(Dictionary<ulong, List<TextureInfo>> existingTextures, Common.STUGUID textureGUID) {
            if (existingTextures == null) {
                existingTextures = new Dictionary<ulong, List<TextureInfo>>();
            }

            if (textureGUID == null) return existingTextures;
            
            switch (GUID.Type(textureGUID)) {
               case 0xA8:
                   STUDecal decal = GetInstance<STUDecal>(textureGUID);
                   if (decal == null) break;
                   foreach (Common.STUGUID material in decal.Materials) {
                       if (!Files.ContainsKey(material)) continue;
                       ImageDefinition def = new ImageDefinition(OpenFile(material));
                       foreach (ImageLayer layer in def.Layers) {
                           AddGUID(existingTextures, new Common.STUGUID(layer.key),
                               Files.ContainsKey(layer.DataKey) ? new Common.STUGUID(layer.DataKey) : null,
                               textureGUID);
                       }
                   }
                   break;
               case 0x04:
                   ulong dataKey = (textureGUID & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
                   AddGUID(existingTextures, textureGUID,
                       Files.ContainsKey(dataKey) ? new Common.STUGUID(dataKey) : null, 0);
                   break;
            }

            return existingTextures;
        }
    }
}