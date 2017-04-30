// Decompiled with JetBrains decompiler

using System.Diagnostics;

namespace APPLIB {
    [DebuggerDisplay("real = {real}, i = {i}, j = {j}, k = {k}")]
    public class Quaternion3D {
        public float real;
        public float i;
        public float j;
        public float k;

        public Quaternion3D() {
            this.real = 0.0f;
            this.i = 0.0f;
            this.j = 0.0f;
            this.k = 0.0f;
        }

        public Quaternion3D(float _real, float _i, float _j, float _k) {
            this.real = _real;
            this.i = _i;
            this.j = _j;
            this.k = _k;
        }

        public Quaternion3D(Vector3D vecXYZ, float _real) {
            this.real = _real;
            this.i = vecXYZ.X;
            this.j = vecXYZ.Y;
            this.k = vecXYZ.Z;
        }
    }
}
