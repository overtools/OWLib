using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.CompilerServices;

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

        public string ToHex(bool includeAlpha = true) {
            var hex = $"#{ToHex(R):X2}{ToHex(G):X2}{ToHex(B):X2}";

            if (includeAlpha) {
                hex += $"{ToHex(A):X2}";
            }

            return hex;
        }

        public string ToCSS() {
            return $"rgba({R * 255}, {G * 255}, {B * 255}, {A})";
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToNonLinearValue(float value, float gamma = 2.4f) {
            return (float)(value <= 0.0031308 ? value * 12.92 : System.Math.Pow(value, 1 / gamma) * 1.055 - 0.055);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToLinearValue(float value, float gamma = 2.4f) {
            return (float)(value <= 0.04045 ? value / 12.92 : System.Math.Pow((value + 0.055) / 1.055, gamma));
        }

        public teColorRGBA ToNonLinear(float gamma = 2.4f) {
            return new teColorRGBA(ToNonLinearValue(R), ToNonLinearValue(G), ToNonLinearValue(B), A);
        }

        public teColorRGBA ToLinear(float gamma = 2.4f) {
            return new teColorRGBA(ToLinearValue(R), ToLinearValue(G), ToLinearValue(B), A);
        }
    }
}
