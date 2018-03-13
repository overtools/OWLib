using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>3 component RGB color</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teColorRGB {
        /// <summary>Red component</summary>
        public float R;
        
        /// <summary>Green component</summary>
        public float G;
        
        /// <summary>Blue component</summary>
        public float B;
    }
}