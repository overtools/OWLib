using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TankLib;
using TankLib.CASC;
using TankLib.Helpers;

namespace VersionManager.Data {
    // this is for per guid stuff, which I'm not doing right now
    /*[StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TACTKeyRecord {
        public ulong Key;
        public bool ValueKnown;
    }
    
    public class Asset : ISerializable {
        public ulong GUID;
        public ulong TACTKey;
        public List<AssetModification> Modifications;
        
        public List<ulong> References;
        public List<ulong> ReferencedBy;
        
        public void Deserialize(BinaryReader reader) {
            GUID = reader.ReadUInt64();
            TACTKey = reader.ReadUInt64();

            Modifications = new List<AssetModification>();
            int modificationCount = reader.ReadInt32();
            for (int i = 0; i < modificationCount; i++) {
                AssetModification assetModification = new AssetModification();
                assetModification.Deserialize(reader);
                Modifications.Add(assetModification);
            }

            References = reader.ReadArray<ulong>().ToList();
            ReferencedBy = reader.ReadArray<ulong>().ToList();
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(GUID);
            writer.Write(TACTKey);
            
            writer.Write(Modifications.Count);
            foreach (AssetModification assetModification in Modifications) {
                assetModification.Serialize(writer);
            }
            
            writer.WriteStructArray(References.ToArray());
            writer.WriteStructArray(ReferencedBy.ToArray());
        }
    }
    
    public class AssetModification : ISerializable {
        public string BuildName;
        public MD5Hash Hash;
        public long Size;
        public AssetModificationType ModificationType;
        
        public void Deserialize(BinaryReader reader) {
            BuildName = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer) {
            //writer.WriteString(BuildName);
        }
    }
    
    public enum AssetModificationType {
        None = 0,
        Added = 1,
        Removed = 2,
        Modified = 3,
        PreExisting = 4
    }

    [Flags]
    public enum AssetModificationFlags {
        None = 0,
        Encrypted = 1
    }*/

    public class AssetData : ISerializable {
        public MD5Hash ContentHash;
        public ulong TACTKey;
        public bool HasUnknownKey;

        public ApplicationPackageManifest.Types.PackageRecord PackageRecord;  // DO NOT WRITE
        
        public void Deserialize(BinaryReader reader) {
            ContentHash = reader.Read<MD5Hash>();
            TACTKey = reader.ReadUInt64();
            HasUnknownKey = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(ContentHash);
            writer.Write(TACTKey);
            writer.Write(HasUnknownKey);
        }
    }

    public class Asset : ISerializable {
        public ulong GUID;
        public MD5Hash ContentHash;
        
        public void Deserialize(BinaryReader reader) {
            GUID = reader.ReadUInt64();
            ContentHash = reader.Read<MD5Hash>();
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(GUID);
            writer.Write(ContentHash);
        }
    }
    
    public class VersionManifest : ISerializable {
        public ulong DataVersion;
        public uint BuildVersion;
        public bool UsedCMF;
        public List<AssetData> AssetData;
        public List<Asset> Assets;

        public VersionManifest() {
            DataVersion = Version;
        }
        
        public void Deserialize(BinaryReader reader) {
            DataVersion = reader.ReadUInt64();
            if (DataVersion != Version) return;
            BuildVersion = reader.ReadUInt32();
            UsedCMF = reader.ReadBoolean();

            long assetDataCount = reader.ReadInt64();
            if (assetDataCount > -1) {
                AssetData = new List<AssetData>();
                for (int i = 0; i < assetDataCount; i++) {
                    AssetData assetData = new AssetData();
                    assetData.Deserialize(reader);
                    AssetData.Add(assetData);
                }
            }

            long assetCount = reader.ReadInt64();
            if (assetCount > -1) {
                Assets = new List<Asset>();
                for (int i = 0; i < assetDataCount; i++) {
                    Asset asset = new Asset();
                    asset.Deserialize(reader);
                    Assets.Add(asset);
                }
            }
        }

        public const ulong Version = 1;

        public void Serialize(BinaryWriter writer) {
            writer.Write(Version);
            writer.Write(BuildVersion);
            writer.Write(UsedCMF);

            if (AssetData == null) {
                writer.Write(-1L);
            } else {
                writer.Write(AssetData.LongCount());

                foreach (AssetData asset in AssetData) {
                    asset.Serialize(writer);
                }
            }
            
            if (Assets == null) {
                writer.Write(-1L);
            } else {
                writer.Write(Assets.LongCount());

                foreach (Asset asset in Assets) {
                    asset.Serialize(writer);
                }
            }
        }
    }
}