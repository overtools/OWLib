using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4 component XYZW vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teVec4 {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;
        
        /// <summary>W component</summary>
        public float W;

        public teVec4(IReadOnlyList<float> val) {
            if (val.Count != 4) {
                throw new InvalidDataException();
            }
            X = val[0];
            Y = val[1];
            Z = val[2];
            W = val[3];
        }
    }
}