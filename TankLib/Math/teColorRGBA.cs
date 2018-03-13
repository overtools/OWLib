using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4 component RGBA color</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teColorRGBA {
        /// <summary>Red component</summary>
        public float R;
        
        /// <summary>Green component</summary>
        public float G;
        
        /// <summary>Blue component</summary>
        public float B;
        
        /// <summary>Alpha component</summary>
        public float A;
    }
}