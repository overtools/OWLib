using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using TankView.Properties;

namespace TankView.ViewModel {
    public class RecentLocations : ObservableCollection<string> {
        List<string> CachedLocations = new List<string>();

        public RecentLocations() {
            if (Settings.Default.RecentLocations == null) {
                Settings.Default.RecentLocations = new StringCollection();
            }

            string[] locations = new string[Settings.Default.RecentLocations.Count];
            CachedLocations = Settings.Default.RecentLocations.Cast<string>().ToList();
            foreach (string location in CachedLocations) {
                base.Add(location);
            }
        }

        public new void Add(string path) {
            if (CachedLocations.Contains(path)) {
                Remove(path);
            }

            Insert(0, path);
            while (CachedLocations.Count > 7) {
                Remove(CachedLocations.ElementAt(7));
            }

            Settings.Default.RecentLocations = new StringCollection();
            Settings.Default.RecentLocations.AddRange(CachedLocations.ToArray());
            Settings.Default.Save();
        }

        public new void Remove(string path) {
            CachedLocations.Remove(path);
            base.Remove(path);
        }

        public new void Insert(int i, string path) {
            CachedLocations.Insert(i, path);
            base.Insert(i, path);
        }
    }
}
