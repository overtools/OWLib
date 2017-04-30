// Decompiled with JetBrains decompiler

using System;
using System.Diagnostics;

namespace APPLIB {
    [DebuggerDisplay("X = {X}, Y = {Y}, Z = {Z}")]
    public class Vector3D {
        public float X;
        public float Y;
        public float Z;

        public Vector3D() {
            this.X = 0.0f;
            this.Y = 0.0f;
            this.Z = 0.0f;
        }

        public Vector3D(float xx, float yy, float zz) {
            this.X = xx;
            this.Y = yy;
            this.Z = zz;
        }

        public Vector3D(Vector3D Vec) {
            this.X = Vec.X;
            this.Y = Vec.Y;
            this.Z = Vec.Z;
        }

        public static Vector3D operator *(Vector3D left, float right) {
            return Vector3D.Multiply(left, right);
        }

        public static Vector3D operator *(Vector3D left, int right) {
            return Vector3D.Multiply(left, right);
        }

        public static Vector3D operator *(float left, Vector3D right) {
            return Vector3D.Multiply(right, left);
        }

        public static Vector3D operator *(Vector3D left, Vector3D right) {
            return Vector3D.Multiply(left, right);
        }

        public static Vector3D operator *(Vector3D left, Quaternion3D right) {
            return Vector3D.Multiply(left, right);
        }

        public static Vector3D Multiply(Vector3D vector, float scale) {
            return new Vector3D(vector.X * scale, vector.Y * scale, vector.Z * scale);
        }

        public static Vector3D Multiply(Vector3D vector, int scale) {
            return new Vector3D(vector.X * (float)scale, vector.Y * (float)scale, vector.Z * (float)scale);
        }

        public static Vector3D Multiply(Vector3D vector, Vector3D scale) {
            return new Vector3D(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z);
        }

        public static Vector3D Multiply(Vector3D vec, Quaternion3D q) {
            float num1 = Convert.ToSingle(2) * (float)((double)q.i * (double)vec.X + (double)q.j * (double)vec.Y + (double)q.k * (double)vec.Z);
            float num2 = Convert.ToSingle(2) * q.real;
            float num3 = num2 * q.real - Convert.ToSingle(1);
            return new Vector3D((float)((double)num3 * (double)vec.X + (double)num1 * (double)q.i + (double)num2 * ((double)q.k * (double)vec.Z - (double)q.k * (double)vec.Y)), (float)((double)num3 * (double)vec.Y + (double)num1 * (double)q.j + (double)num2 * ((double)q.k * (double)vec.X - (double)q.i * (double)vec.Z)), (float)((double)num3 * (double)vec.Z + (double)num1 * (double)q.k + (double)num2 * ((double)q.i * (double)vec.Y - (double)q.j * (double)vec.X)));
        }
        
        public static Vector3D operator +(Vector3D a, Vector3D b) {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public float LengthSquared {
            get {
                return (float)((double)this.X * (double)this.X + (double)this.Y * (double)this.Y + (double)this.Z * (double)this.Z);
            }
        }

        public static Vector3D Cross(Vector3D left, Vector3D right) {
            return new Vector3D((float)((double)left.Y * (double)right.Z - (double)left.Z * (double)right.Y), (float)((double)left.Z * (double)right.X - (double)left.X * (double)right.Z), (float)((double)left.X * (double)right.Y - (double)left.Y * (double)right.X));
        }

        public static float Dot(Vector3D left, Vector3D right) {
            return (float)((double)left.X * (double)right.X + (double)left.Y * (double)right.Y + (double)left.Z * (double)right.Z);
        }
    }
}
