using ProtoBuf;
using System.Collections.Generic;

namespace TankLib.Agent.Protobuf
{
    [ProtoContract]
    public class LanguageSetting
    {
        [ProtoMember(1)]
        public string Language { get; set; }

        [ProtoMember(2)]
        public LanguageOption Option { get; set; }
    }

    [ProtoContract]
    public class UserSettings
    {
        [ProtoMember(1)]
        public string InstallPath { get; set; }

        [ProtoMember(2)]
        public string PlayRegion { get; set; }

        [ProtoMember(3)]
        public ShortcutOption DesktopShortcut { get; set; }

        [ProtoMember(4)]
        public ShortcutOption StartmenuShortcut { get; set; }

        [ProtoMember(5)]
        public LanguageSettingType LanguageSettings { get; set; }

        [ProtoMember(6)]
        public string SelectedTextLanguage { get; set; }

        [ProtoMember(7)]
        public string SelectedSpeechLanguage { get; set; }

        [ProtoMember(8)]
        public List<LanguageSetting> Languages { get; set; } = new List<LanguageSetting>();

        [ProtoMember(9)]
        public string GfxOverrideTags { get; set; }

        [ProtoMember(10)]
        public string VersionBranch { get; set; }
    }

    [ProtoContract]
    public class InstallHandshake
    {
        [ProtoMember(1)]
        public string Product { get; set; }

        [ProtoMember(2)]
        public string Uid { get; set; }

        [ProtoMember(3)]
        public UserSettings Settings { get; set; }

    }

    [ProtoContract]
    public class BuildConfig
    {
        [ProtoMember(1)]
        public string Region { get; set; }

        [ProtoMember(2)]
        public string Config { get; set; }
    }

    [ProtoContract]
    public class BaseProductState
    {
        [ProtoMember(1)]
        public bool Installed { get; set; }

        [ProtoMember(2)]
        public bool Playable { get; set; }

        [ProtoMember(3)]
        public bool UpdateComplete { get; set; }

        [ProtoMember(4)]
        public bool BackgroundDownloadAvailable { get; set; }

        [ProtoMember(5)]
        public bool BackgroundDownloadComplete { get; set; }

        [ProtoMember(6)]
        public string CurrentVersion { get; set; }

        [ProtoMember(7)]
        public string CurrentVersionStr { get; set; }

        [ProtoMember(8)]
        public List<BuildConfig> InstalledBuildConfigs { get; set; } = new List<BuildConfig>();

        [ProtoMember(9)]
        public List<BuildConfig> BackgroundDownloadBuildConfigs { get; set; } = new List<BuildConfig>();

        [ProtoMember(10)]
        public string DecryptionKey { get; set; }

        [ProtoMember(11)]
        public List<string> CompletedInstallActions { get; set; } = new List<string>();

    }

    [ProtoContract]
    public class BackfillProgress
    {
        [ProtoMember(1)]
        public double Progress { get; set; }

        [ProtoMember(2)]
        public bool Backgrounddownload { get; set; }

        [ProtoMember(3)]
        public bool Paused { get; set; }

        [ProtoMember(4)]
        public ulong DownloadLimit { get; set; }
    }

    [ProtoContract]
    public class RepairProgress
    {
        [ProtoMember(1)]
        public double Progress { get; set; }
    }

    [ProtoContract]
    public class UpdateProgress
    {
        [ProtoMember(1)]
        public string LastDiscSetUsed { get; set; }

        [ProtoMember(2)]
        public double Progress { get; set; }

        [ProtoMember(3)]
        public bool DiscIgnored { get; set; }

        [ProtoMember(4)]
        public ulong TotalToDownload { get; set; }

        [ProtoMember(5)]
        public ulong DownloadRemaining { get; set; }
    }

    [ProtoContract]
    public class CachedProductState
    {
        [ProtoMember(1)]
        public BaseProductState BaseProductState { get; set; }

        [ProtoMember(2)]
        public BackfillProgress BackfillProgress { get; set; }

        [ProtoMember(3)]
        public RepairProgress RepairProgress { get; set; }

        [ProtoMember(4)]
        public UpdateProgress UpdateProgress { get; set; }
    }

    [ProtoContract]
    public class ProductOperations
    {
        [ProtoMember(1)]
        public Operation ActiveOperation { get; set; }

        [ProtoMember(2)]
        public ulong Priority { get; set; }
    }

    [ProtoContract]
    public class ProductInstall
    {
        [ProtoMember(1)]
        public string Uid { get; set; }

        [ProtoMember(2)]
        public string ProductCode { get; set; }

        [ProtoMember(3)]
        public UserSettings Settings { get; set; }

        [ProtoMember(4)]
        public CachedProductState CachedProductState { get; set; }

        [ProtoMember(5)]
        public ProductOperations ProductOperations { get; set; }
    }

    [ProtoContract]
    public class ProductConfig
    {
        [ProtoMember(1)]
        public string ProductCode { get; set; }

        [ProtoMember(2)]
        public string MetadataHash { get; set; }

        [ProtoMember(3)]
        public string Timestamp { get; set; }
    }

    [ProtoContract]
    public class ActiveProcess
    {
        [ProtoMember(1)]
        public string ProcessName { get; set; }

        [ProtoMember(2)]
        public int Pid { get; set; }

        [ProtoMember(3)]
        public List<string> Uris { get; set; } = new List<string>();
    }

    [ProtoContract]
    public class DownloadSettings
    {
        [ProtoMember(1)]
        public int DownloadLimit { get; set; }

        [ProtoMember(2)]
        public int BackfillLimit { get; set; }
    }

    [ProtoContract]
    public class Database
    {
        [ProtoMember(1)]
        public List<ProductInstall> ProductInstalls { get; set; } = new List<ProductInstall>();

        [ProtoMember(2)]
        public List<InstallHandshake> ActiveInstalls { get; set; } = new List<InstallHandshake>();

        [ProtoMember(3)]
        public List<ActiveProcess> ActiveProcesses { get; set; } = new List<ActiveProcess>();

        [ProtoMember(4)]
        public List<ProductConfig> ProductConfigs { get; set; } = new List<ProductConfig>();

        [ProtoMember(5)]
        public DownloadSettings DownloadSettings { get; set; }
    }

    [ProtoContract]
    public enum LanguageOption
    {
        LangoptionNone = 0,
        LangoptionText = 1,
        LangoptionSpeech = 2,
        LangoptionTextAndSpeech = 3,
    }

    [ProtoContract]
    public enum LanguageSettingType
    {
        LangsettingNone = 0,
        LangsettingSingle = 1,
        LangsettingSimple = 2,
        LangsettingAdvanced = 3,
    }

    [ProtoContract]
    public enum ShortcutOption
    {
        ShortcutNone = 0,
        ShortcutUser = 1,
        ShortcutAllUsers = 2,
    }

    [ProtoContract]
    public enum Operation
    {
        OpNone = -1,
        OpUpdate = 0,
        OpBackfill = 1,
        OpRepair = 2,
    }
}
