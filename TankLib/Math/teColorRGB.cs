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

        private byte ToHex(float a) {
            return (byte) (a * 255f);
        }
        
        public string ToHex() {
            return $"#{ToHex(R):X2}{ToHex(G):X2}{ToHex(B):X2}";
        }

        public static bool operator ==(teColorRGB a, teColorRGB b) {
            return a.Equals(b);
        }
        
        public static bool operator !=(teColorRGB a, teColorRGB b) {
            return !a.Equals(b);
        }
        
        public bool Equals(teColorRGBA other) {
            return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B);
        }

        public override bool Equals(object obj) {
            return obj is teColorRGBA colorObj && Equals(colorObj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }

        public teColorRGB ToNonLinear(float gamma = 2.2f) {
            return new teColorRGB((float) System.Math.Pow(R, 1/gamma), (float) System.Math.Pow(G, 1/gamma), (float) System.Math.Pow(B, 1/gamma));
        }

        public teColorRGB ToLinear(float gamma = 2.2f) {
            return new teColorRGB((float) System.Math.Pow(R, gamma), (float) System.Math.Pow(G, gamma), (float) System.Math.Pow(B, gamma));
        }
    }
}