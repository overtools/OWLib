using System;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teUUID {
        public Guid Value;
    }
}