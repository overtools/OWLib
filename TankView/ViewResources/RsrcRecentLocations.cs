using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TankView.ViewResources
{
    public class RsrcRecentLocations : ObservableCollection<string>
    {
        List<string> CachedLocations = new List<string>();

        public RsrcRecentLocations()
        {
            if(Properties.Settings.Default.RecentLocations == null)
            {
                Properties.Settings.Default.RecentLocations = new System.Collections.Specialized.StringCollection();
            }

            string[] locations = new string[Properties.Settings.Default.RecentLocations.Count];
            CachedLocations = Properties.Settings.Default.RecentLocations.Cast<string>().ToList();
            foreach (string location in CachedLocations) {
                base.Add(location);
            }
        }

        public new void Add(string path)
        {
            if (CachedLocations.Contains(path))
            {
                Remove(path);
            }
            Insert(0, path);
            while (CachedLocations.Count > 7)
            {
                Remove(CachedLocations.ElementAt(7));
            }
            Properties.Settings.Default.RecentLocations = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.RecentLocations.AddRange(CachedLocations.ToArray());
            Properties.Settings.Default.Save();
        }

        public new void Remove(string path)
        {
            CachedLocations.Remove(path);
            base.Remove(path);
        }

        public new void Insert(int i, string path)
        {
            CachedLocations.Insert(i, path);
            base.Insert(i, path);
        }
    }
}
