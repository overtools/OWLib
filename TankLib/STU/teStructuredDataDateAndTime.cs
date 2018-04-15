using System.Runtime.InteropServices;

namespace TankLib.STU {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teStructuredDataDateAndTime {
        public ulong Value;
        
        // todo: actually parse this
        // something something weekday padding?
        // end bytes maybe do something special
    }
}