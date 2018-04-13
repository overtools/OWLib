using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankView.ObjectModel
{
    public class ObservableHashCollection<T> : ObservableCollection<T>
    {
        public new void Add(T host)
        {
            if (!this.Any(x => x.GetHashCode() == host.GetHashCode()))
            {
                base.Add(host);
            }
        }
    }
}
