using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STULib {
    public interface IDemangleable {
        ulong[] GetGUIDs();
        void SetGUIDs(ulong[] GUIDs);
    }
}
