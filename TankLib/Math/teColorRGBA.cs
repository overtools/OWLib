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
            if (ReferenceEquals(null, obj)) return false;
            return obj is teColorRGBA && Equals((teColorRGBA) obj);
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