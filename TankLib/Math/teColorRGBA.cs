using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;

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
        
        public teColorRGBA(float red, float green, float blue, float alpha) {
            R = red;
            G = green;
            B = blue;
            A = alpha;
        }
        
        public teColorRGBA(IReadOnlyList<float> val) {
            if (val.Count != 4) {
                throw new InvalidDataException();
            }

            R = val[0];
            G = val[1];
            B = val[2];
            A = val[3];
        }
        
        public static implicit operator Color(teColorRGBA obj) {
            return Color.FromArgb (
                (int) (obj.R * 255f),
                (int) (obj.G * 255f),
                (int) (obj.B * 255f)
            );
        }

        private byte ToHex(float a) {
            return (byte) System.Math.Round(a * 255f);
        }
        
        public string ToHex() {
            return $"#{ToHex(R):X2}{ToHex(G):X2}{ToHex(B):X2} {A}";
        }

        public static bool operator ==(teColorRGBA a, teColorRGBA b) {
            return a.Equals(b);
        }
        
        public static bool operator !=(teColorRGBA a, teColorRGBA b) {
            return !a.Equals(b);
        }
        
        public bool Equals(teColorRGBA other) {
            return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
        }

        public override bool Equals(object obj) {
            return obj is teColorRGBA colorObj && Equals(colorObj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                hashCode = (hashCode * 397) ^ A.GetHashCode();
                return hashCode;
            }
        }

        public teColorRGBA ToNonLinear(float gamma = 2.2f) {
            return new teColorRGBA((float) System.Math.Pow(R, 1/gamma), (float) System.Math.Pow(G, 1/gamma), (float) System.Math.Pow(B, 1/gamma), A);
        }

        public teColorRGBA ToLinear(float gamma = 2.2f) {
            return new teColorRGBA((float) System.Math.Pow(R, gamma), (float) System.Math.Pow(G, gamma), (float) System.Math.Pow(B, gamma), A);
        }
    }
}
