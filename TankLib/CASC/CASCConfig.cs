using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CMFLib;
using TankLib.CASC.ConfigFiles;
using TankLib.CASC.Handlers;

namespace TankLib.CASC {
    /// <summary>CASC handler config</summary>
    public class CASCConfig {
        private KeyValueConfig _cdnConfig;
        private BarSeparatedConfig _buildInfo;
        private BarSeparatedConfig _cdnData;
        private BarSeparatedConfig _versionsData;
        
        /// <summary>Build configs</summary>
        public List<KeyValueConfig> Builds { get; private set; }
        
        /// <summary>Keyring</summary>
        public KeyValueConfig KeyRing { get; private set; }

        /// <summary>Region</summary>
        public string Region;
        
        /// <summary>Online storage</summary>
        public bool OnlineMode;
        
        /// <summary>Storage root</summary>
        public string BasePath;
        
        /// <summary>Active build number</summary>
        public int ActiveBuild;
        
        /// <summary>Selected languages</summary>
        public HashSet<string> Languages;
        
        /// <summary>CDN URI</summary>
        public string CustomCDNHost;

        public static string GetDataFolder() => "data/casc";
        
        private int _versionsIndex; // todo
        
        #region CDN Properties
        
        public List<string> Archives => _cdnConfig["archives"];
        public string CDNPath => OnlineMode ? _cdnData[0]["Path"] : _buildInfo[0]["CDNPath"];
        public string[] CDNHosts {
            get {
                List<string> hosts = new List<string>();
                if (CustomCDNHost != null) {
                    hosts.Add(CustomCDNHost);
                }
                if (OnlineMode) {
                    for (int i = 0; i < _cdnData.Data.Count; i++) {
                        if (_cdnData[i]["Name"] == Region)
                            hosts.AddRange(_cdnData[i]["Hosts"].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    hosts.AddRange(_cdnData[0]["Hosts"].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                } else {
                    hosts.AddRange(_buildInfo[0]["CDNHosts"].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                }
                return hosts.ToArray();
            }
        }
        public MD5Hash RootMD5 => Builds[ActiveBuild]["root"][0].ToByteArray().ToMD5();
        public MD5Hash InstallMD5 => Builds[ActiveBuild]["install"][0].ToByteArray().ToMD5();
        public MD5Hash PatchMD5 => Builds[ActiveBuild]["patch"][0].ToByteArray().ToMD5();
        public MD5Hash DownloadMD5 => Builds[ActiveBuild]["download"][0].ToByteArray().ToMD5();
        public MD5Hash EncodingMD5 => Builds[ActiveBuild]["encoding"][0].ToByteArray().ToMD5();
        public MD5Hash EncodingKey => Builds[ActiveBuild]["encoding"][1].ToByteArray().ToMD5();
        //public MD5Hash PartialPriorityMD5 => _Builds[ActiveBuild]["partial-priority"][0].ToByteArray().ToMD5();
        
        public string DownloadSize => Builds[ActiveBuild]["download-size"][0];        
        public string InstallSize => Builds[ActiveBuild]["install-size"][0];
        //public string PartialPrioritySize => _Builds[ActiveBuild]["partial-priority-size"][0];        
        public string EncodingSize => Builds[ActiveBuild]["encoding-size"][0];
        public string PatchSize => Builds[ActiveBuild]["patch-size"][0];
        
        public string BuildUID => Builds[ActiveBuild]["build-uid"][0];
        //public static string GlobalCustomCDN;
        public string CDNHost => CDNHosts[0];
        public string CDNUrl {
            get {
                if (!OnlineMode) return $"http://{_buildInfo[0]["CDNHosts"].Split(' ')[0]}{_buildInfo[0]["CDNPath"]}";
                int index = 0;

                for (int i = 0; i < _cdnData.Data.Count; ++i) {
                    if (_cdnData[i]["Name"] != Region) continue;
                    index = i;
                    break;
                }
                return $"http://{_cdnData[index]["Hosts"].Split(' ')[0]}/{_cdnData[index]["Path"]}";
            }
        }
        public string ArchiveGroup => _cdnConfig["archive-group"][0];
        public List<string> PatchArchives => _cdnConfig["patch-archives"];
        public string PatchArchiveGroup => _cdnConfig["patch-archive-group"][0];
        
        public string BuildName => GetActiveBuild()?["Version"] ?? _versionsData[_versionsIndex]["VersionsName"];

        public bool LoadPackageManifest = true;
        public bool LoadContentManifest = true;
        #endregion
        
        
        public static CASCConfig LoadLocalStorageConfig(string basePath, bool useKeyring, bool loadMultipleLangs) {
            CASCConfig config = new CASCConfig {
                OnlineMode = false,
                BasePath = basePath
            };

            string buildInfoPath = Path.Combine(basePath, ".build.info");

            using (Stream buildInfoStream = new FileStream(buildInfoPath, FileMode.Open)) {
                config._buildInfo = BarSeparatedConfig.Read(buildInfoStream);
            }

            Dictionary<string, string> bi = config.GetActiveBuild();

            if (bi == null)
                throw new Exception("Can't find active BuildInfoEntry");

            string dataFolder = GetDataFolder();

            config.ActiveBuild = 0;

            config.Builds = new List<KeyValueConfig>();

            string buildKey = bi["BuildKey"];
            string buildCfgPath = Path.Combine(basePath, dataFolder, "config", buildKey.Substring(0, 2), buildKey.Substring(2, 2), buildKey);
            try {
                using (Stream stream = new FileStream(buildCfgPath, FileMode.Open)) {
                    config.Builds.Add(KeyValueConfig.Read(stream));
                }
            } catch {
                using (Stream stream = CDNIndexHandler.OpenConfigFileDirect(config, buildKey)) {
                    config.Builds.Add(KeyValueConfig.Read(stream));
                }
            }

            string cdnKey = bi["CDNKey"];
            string cdnCfgPath = Path.Combine(basePath, dataFolder, "config", cdnKey.Substring(0, 2), cdnKey.Substring(2, 2), cdnKey);
            try {
                using (Stream stream = new FileStream(cdnCfgPath, FileMode.Open)) {
                    config._cdnConfig = KeyValueConfig.Read(stream);
                }
            } catch {
                using (Stream stream = CDNIndexHandler.OpenConfigFileDirect(config, cdnKey)) {
                    config._cdnConfig = KeyValueConfig.Read(stream);
                }
            }

            if (bi.ContainsKey("Keyring") && bi["Keyring"].Length > 0) {
                string keyringKey = bi["Keyring"];
                string keyringCfgPath = Path.Combine(basePath, dataFolder, "config", keyringKey.Substring(0, 2), keyringKey.Substring(2, 2), keyringKey);
                try {
                    using (Stream stream = new FileStream(keyringCfgPath, FileMode.Open)) {
                        config.KeyRing = KeyValueConfig.Read(stream);
                    }
                } catch {
                    using (Stream stream = CDNIndexHandler.OpenConfigFileDirect(config, keyringKey)) {
                        config.KeyRing = KeyValueConfig.Read(stream);
                    }
                }
                if (useKeyring) {
                    config.LoadKeyringKeys(TACTKeyService.Keys, true);
                }
            }

            if (loadMultipleLangs) {
                config.Languages = new HashSet<string>();
                if (bi.ContainsKey("Tags") && bi["Tags"].Trim().Length > 0) {
                    string[] tags = bi["Tags"].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string tag in tags) {
                        try {
                            Enum.Parse(typeof(LocaleFlags), tag.Substring(0, 4));
                            config.Languages.Add(tag);
                        } catch { }
                    }
                }
            }

            return config;
        }
        
        public void LoadKeyringKeys(Dictionary<ulong, byte[]> existingKeys, bool overwrite = false) {
            if (KeyRing == null) {
                return;
            }

            foreach (KeyValuePair<string, List<string>> pair in KeyRing.KeyValue) {
                if (!pair.Key.StartsWith("key-")) continue;
                string reverseKey = pair.Key.Substring(pair.Key.Length - 16);
                string key = "";
                for (int i = 0; i < 8; ++i) {
                    key = reverseKey.Substring(i * 2, 2) + key;
                }
                ulong keyL = ulong.Parse(key, NumberStyles.HexNumber);
                if (overwrite || !existingKeys.ContainsKey(keyL)) {
                    existingKeys[keyL] = pair.Value[0].ToByteArray();
                }
            }
        }

        public Dictionary<string, string> GetActiveBuild() {
            if (_buildInfo == null)
                return null;

            for (int i = 0; i < _buildInfo.Data.Count; ++i) {
                if (_buildInfo[i]["Active"] == "1") {
                    return _buildInfo[i];
                }
            }

            return null;
        }
    }
}