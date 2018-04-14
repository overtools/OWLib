using System.ComponentModel;
using TankLib.CASC;

namespace TankView.ViewResources
{
    public class RsrcCacheInfo : INotifyPropertyChanged
    {
        private bool _cdn = Properties.Settings.Default.CacheCDN;
        private bool _data = Properties.Settings.Default.CacheData;
        private bool _apm = Properties.Settings.Default.CacheAPM;

        public bool CDN {
            get {
                return _cdn;
            }
            set {
                _cdn = value;
                Properties.Settings.Default.CacheCDN = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.CacheData = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.CacheAPM = value;
                Properties.Settings.Default.Save();
                CASCHandler.Cache.CacheAPM = value;
                NotifyPropertyChanged(nameof(APM));
            }
        }
        
        public RsrcCacheInfo()
        {
            CASCHandler.Cache.CacheCDN = CDN;
            CASCHandler.Cache.CacheCDNData = Data;
            CASCHandler.Cache.CacheAPM = APM;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
