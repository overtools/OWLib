using System;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teUUID {
        public Guid Value;

        public teUUID(Guid guid) {
            Value = guid;
        }

        public static teUUID New() {
            return new teUUID(Guid.NewGuid());
        }
        
        public bool Equals(teUUID other) {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            return obj is teUUID && Equals((teUUID) obj);
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }
    }
}