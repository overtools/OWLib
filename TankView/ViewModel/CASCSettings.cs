using System;
using System.ComponentModel;
using TankLib.CASC;
using TankView.Properties;

namespace TankView.ViewModel
{
    public class CASCSettings : INotifyPropertyChanged
    {
        private bool _cdn = Settings.Default.CacheCDN;
        private bool _data = Settings.Default.CacheData;
        private bool _apm = Settings.Default.CacheAPM;
        private bool _loadall = Settings.Default.LoadAllLanguages;
        private bool _threading = Settings.Default.DisableThreading;

        public bool CDN {
            get {
                return _cdn;
            }
            set {
                _cdn = value;
                Settings.Default.CacheCDN = value;
                Settings.Default.Save();
                CASCHandler.Cache.CacheCDN = value;
                NotifyPropertyChanged(nameof(CDN));
            }
        }

        public bool Data {
            get {
                return _data;
            }
            set {
                _data = value;
                Settings.Default.CacheData = value;
                Settings.Default.Save();
                CASCHandler.Cache.CacheCDNData = value;
                NotifyPropertyChanged(nameof(Data));
            }
        }

        public bool APM {
            get {
                return _apm;
            }
            set {
                _apm = value;
                Settings.Default.CacheAPM = value;
                Settings.Default.Save();
                CASCHandler.Cache.CacheAPM = value;
                NotifyPropertyChanged(nameof(APM));
            }
        }

        public bool LoadAllLanguages {
            get {
                return _loadall;
            }
            set {
                _loadall = value;
                Settings.Default.LoadAllLanguages = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(LoadAllLanguages));
            }
        }

        public bool DisableThreading {
            get {
                return _threading;
            }
            set {
                _threading = value;
                Settings.Default.DisableThreading = value;
                Settings.Default.Save();
                CASCConfig.MaxThreads = value ? 1 : Environment.ProcessorCount;
                NotifyPropertyChanged(nameof(DisableThreading));
            }
        }

        public CASCSettings()
        {
            CASCHandler.Cache.CacheCDN = CDN;
            CASCHandler.Cache.CacheCDNData = Data;
            CASCHandler.Cache.CacheAPM = APM;
            CASCConfig.MaxThreads = DisableThreading ? 1 : Environment.ProcessorCount;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
