using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataTool.SaveLogic {
    public class ScratchDB : IEnumerable<KeyValuePair<ulong, ScratchDB.ScratchPath>> {
        public class ScratchPath {
            public string AbsolutePath { get; private set; }
            private Uri AbsoluteUri { get; set; }
            public bool CheckedExistence { get; set; }

            public ScratchPath(string path) {
                AbsolutePath = Path.GetFullPath(path);
                AbsoluteUri = new Uri(AbsolutePath);
            }

            public string MakeRelative(string cwd) {
                Uri folder = new Uri(Path.GetFullPath(cwd) + Path.DirectorySeparatorChar);
                return Uri.UnescapeDataString(folder.MakeRelativeUri(AbsoluteUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
        }

        private Dictionary<ulong, ScratchPath> Records = new Dictionary<ulong, ScratchPath>();

        public IEnumerator<KeyValuePair<ulong, ScratchPath>> GetEnumerator() {
            return Records.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Records.GetEnumerator();
        }

        public bool HasRecord(ulong guid) {
            if (Records.ContainsKey(guid)) {
                if (!Records[guid].CheckedExistence) {
                    if (!File.Exists(Records[guid].AbsolutePath) &&
                        !File.Exists(Path.ChangeExtension(Records[guid].AbsolutePath, "dds")) &&
                        !File.Exists(Path.ChangeExtension(Records[guid].AbsolutePath, "tif")) &&
                        !File.Exists(Path.ChangeExtension(Records[guid].AbsolutePath, "png")) &&
                        !File.Exists(Path.ChangeExtension(Records[guid].AbsolutePath, "jpg"))) {
                        RemoveRecord(guid);
                        return false;
                    } else {
                        Records[guid].CheckedExistence = true;
                    }
                }
                return true;
            }
            return false;
        }

        public void SetRecord(ulong guid, ScratchPath path) {
            Records[guid] = path;
        }

        public ScratchPath GetRecord(ulong guid) {
            if (HasRecord(guid)) {
                return Records[guid];
            }
            return null;
        }

        public bool RemoveRecord(ulong guid) {
            return Records.Remove(guid);
        }

        public ScratchPath this[ulong guid] {
            get {
                return GetRecord(guid);
            }
            set {
                SetRecord(guid, value);
            }
        }

        public int Count => Records.Count;
        public long LongCount => Records.LongCount();

        public void Save(string dbPath) {
            if (Count == 0) {
                return;
            }
            if (File.Exists(dbPath)) {
                File.Delete(dbPath);
            }

            string dir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            using (Stream file = File.OpenWrite(dbPath))
            using (BinaryWriter writer = new BinaryWriter(file, Encoding.Unicode)) {
                writer.Write((short)2);
                writer.Write(dbPath);
                writer.Write(LongCount);
                foreach (KeyValuePair<ulong, ScratchPath> pair in this) {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.AbsolutePath);
                }
            }
        }

        public void Load(string dbPath) {
            if (!File.Exists(dbPath)) {
                TankLib.Helpers.Logger.Error("ScratchDB", $"Database {dbPath} does not exist");
                return;
            }

            using (Stream file = File.OpenRead(dbPath))
            using (BinaryReader reader = new BinaryReader(file, Encoding.Unicode)) {
                if (file.Length - file.Position < 4) {
                    TankLib.Helpers.Logger.Error("ScratchDB", "File is not long enough");
                }
                short version = reader.ReadInt16();
                ScratchDBLogicMethod method = ScratchDBLogic.ElementAtOrDefault(version);
                if (method == null) {
                    TankLib.Helpers.Logger.Error("ScratchDB", $"Database is version {version} which is not supported");
                    return;
                }
                try {
                    method(reader, dbPath, SetRecord);
                } catch (Exception e) {
                    TankLib.Helpers.Logger.Error("ScratchDB", e.ToString());
                }
            }
        }

        private delegate void ScratchDBLogicCallback(ulong guid, ScratchPath scratchPath);
        private delegate void ScratchDBLogicMethod(BinaryReader reader, string dbPath, ScratchDBLogicCallback cb);

        private List<ScratchDBLogicMethod> ScratchDBLogic = new List<ScratchDBLogicMethod>() {
            null,
            (reader, dbPath, cb) => {
                ulong amount = reader.ReadUInt64();
                for(ulong i = 0; i < amount; ++i) {
                    ulong guid = reader.ReadUInt64();
                    string path = reader.ReadString();
                    cb(guid, new ScratchPath(path));
                }
            },
            (reader, dbPath, cb) => {
                if (reader.ReadString() != dbPath) {
                    return;
                }
                ulong amount = reader.ReadUInt64();
                for(ulong i = 0; i < amount; ++i) {
                    ulong guid = reader.ReadUInt64();
                    string path = reader.ReadString();
                    cb(guid, new ScratchPath(path));
                }
            }
        };
    }
}
