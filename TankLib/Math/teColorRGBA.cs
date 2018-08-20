using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using Newtonsoft.Json;

namespace TankLib.Math {
    /// <summary>4 component RGBA color</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [JsonObject(MemberSerialization.OptOut)]
    public struct teColorRGBA {
        /// <summary>Red component</summary>
        public float R;
        
        /// <summary>Green component</summary>
        public float G;
        
        /// <summary>Blue component</summary>
        public float B;
        
        /// <summary>Alpha component</summary>
        public float A;
        
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

        public string ToHex() {
            Color c = this;
            return $"#{c.Name}";
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
    }
}