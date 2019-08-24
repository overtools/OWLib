using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        
        public teColorRGB(float red, float green, float blue) {
            R = red;
            G = green;
            B = blue;
        }
        
        public teColorRGB(IReadOnlyList<float> val) {
            if (val.Count != 3) {
                throw new InvalidDataException();
            }

            R = val[0];
            G = val[1];
            B = val[2];
        }
        
        public static implicit operator Color(teColorRGB obj) {
            return Color.FromArgb (
                (int) (obj.R * 255f),
                (int) (obj.G * 255f),
                (int) (obj.B * 255f)
            );
        }
    }
}