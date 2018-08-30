using System.Collections.ObjectModel;
using System.Linq;

namespace TankView.ObjectModel {
    public class ObservableHashCollection<T> : ObservableCollection<T> {
        public new void Add(T host) {
            if (!this.Any(x => x.GetHashCode() == host.GetHashCode())) {
                base.Add(host);
            }
        }
    }
}
