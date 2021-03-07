using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace DataTool.SaveLogic {
    public class ScratchDB : IEnumerable<KeyValuePair<ulong, ScratchDB.ScratchPath>> {
        public struct ScratchPath {
            public string AbsolutePath { get; }
            private Uri AbsoluteUri { get; }
            public bool CheckedExistence { get; set; }

            public ScratchPath(string path, bool checkedExistence) {
                AbsolutePath = Path.GetFullPath(path);
                AbsoluteUri = new Uri(AbsolutePath);
                CheckedExistence = checkedExistence;
            }

            public string MakeRelative(string cwd) {
                Uri folder = new Uri(Path.GetFullPath(cwd) + Path.DirectorySeparatorChar);
                return Uri.UnescapeDataString(folder.MakeRelativeUri(AbsoluteUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
        }

        private readonly ConcurrentDictionary<ulong, ScratchPath> Records = new ConcurrentDictionary<ulong, ScratchPath>();

        public IEnumerator<KeyValuePair<ulong, ScratchPath>> GetEnumerator() {
            return Records.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Records.GetEnumerator();
        }

        public bool HasRecord(ulong guid) {
            if (Records.ContainsKey(guid)) {
                if (!Records[guid].CheckedExistence) {
                    if (!Records.TryGetValue(guid, out var record)) return false;

                    if (!File.Exists(record.AbsolutePath) &&
                        !File.Exists(Path.ChangeExtension(record.AbsolutePath, "dds")) &&
                        !File.Exists(Path.ChangeExtension(record.AbsolutePath, "tif")) &&
                        !File.Exists(Path.ChangeExtension(record.AbsolutePath, "png")) &&
                        !File.Exists(Path.ChangeExtension(record.AbsolutePath, "jpg"))) {
                        RemoveRecord(guid);
                        return false;
                    }

                    record.CheckedExistence = true;
                    SetRecord(guid, record);
                }

                return true;
            }

            return false;
        }

        public bool SetRecord(ulong guid, ScratchPath path) {
            return Records.TryAdd(guid, path);
        }

        public ScratchPath? GetRecord(ulong guid) {
            return HasRecord(guid) && Records.TryGetValue(guid, out var record) ? record : default;
        }

        public bool RemoveRecord(ulong guid) {
            return Records.TryRemove(guid, out _);
        }

        public ScratchPath? this[ulong guid] {
            get => GetRecord(guid);
            set => SetRecord(guid, value.GetValueOrDefault());
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
            if (dir != null && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            using (Stream file = File.OpenWrite(dbPath))
            using (BinaryWriter writer = new BinaryWriter(file, Encoding.Unicode)) {
                writer.Write((short) 2);
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

        private delegate bool ScratchDBLogicCallback(ulong guid, ScratchPath scratchPath);

        private delegate void ScratchDBLogicMethod(BinaryReader reader, string dbPath, ScratchDBLogicCallback cb);

        private readonly List<ScratchDBLogicMethod> ScratchDBLogic = new List<ScratchDBLogicMethod>() {
            null,
            (reader, dbPath, cb) => {
                ulong amount = reader.ReadUInt64();
                for (ulong i = 0; i < amount; ++i) {
                    ulong guid = reader.ReadUInt64();
                    string path = reader.ReadString();
                    cb(guid, new ScratchPath(path, false));
                }
            },
            (reader, dbPath, cb) => {
                if (reader.ReadString() != dbPath) {
                    return;
                }

                ulong amount = reader.ReadUInt64();
                for (ulong i = 0; i < amount; ++i) {
                    ulong guid = reader.ReadUInt64();
                    string path = reader.ReadString();
                    cb(guid, new ScratchPath(path, false));
                }
            }
        };
    }
}
