using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using static System.Math;

namespace TankLib.Math {
    /// <summary>Quaternion</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("X: {X}, Y: {Y}, Z: {Z}, W: {W}")]
    public struct teQuat {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;
        
        /// <summary>W component</summary>
        public float W;

        public teQuat(float x, float y, float z, float w) {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public teQuat(double x, double y, double z, double w) {
            X = (float) x;
            Y = (float) y;
            Z = (float) z;
            W = (float) w;
        }
        
        public static teQuat Identity => new teQuat(0, 0, 0, 1);
        
        public static teQuat operator *(teQuat left, teQuat right) {
            return Multiply(ref left, ref right);
        }
        
        private enum EulerParity {
            Even,
            Odd
        }

        private enum EulerRepeat {
            No,
            Yes
        }

        private enum EulerFrame {
            S,
            R
        }

        public teVec3 ToEulerAngles() {
            return EulerFromQuat(0, 1, 2, 0, EulerParity.Even, EulerRepeat.No, EulerFrame.S);
        }

        private teVec3 EulerFromQuat(int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame) {
            double[,] mat = new double[4, 4];

            double num1 = X * (double)X + Y * (double)Y + Z * (double)Z + W * (double)W;
            double num2 = num1 <= 0.0 ? 0.0 : 2.0 / num1;
            double num3 = X * num2;
            double num4 = Y * num2;
            double num5 = Z * num2;
            double num6 = W * num3;
            double num7 = W * num4;
            double num8 = W * num5;
            double num9 = X * num3;
            double num10 = X * num4;
            double num11 = X * num5;
            double num12 = Y * num4;
            double num13 = Y * num5;
            double num14 = Z * num5;
            mat[0, 0] = 1.0 - (num12 + num14);
            mat[0, 1] = num10 - num8;
            mat[0, 2] = num11 + num7;
            mat[1, 0] = num10 + num8;
            mat[1, 1] = 1.0 - (num9 + num14);
            mat[1, 2] = num13 - num6;
            mat[2, 0] = num11 - num7;
            mat[2, 1] = num13 + num6;
            mat[2, 2] = 1.0 - (num9 + num12);
            mat[3, 3] = 1.0;
            return EulerFromHMatrix(mat, i, j, k, h, parity, repeat, frame);
        }

        private static teVec3 EulerFromHMatrix(double[,] mat, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame) {
            teVec3 vec3 = new teVec3();
            if (repeat == EulerRepeat.Yes) {
                double y = Sqrt(mat[i, j] * mat[i, j] + mat[i, k] * mat[i, k]);
                if (y > 0.00016) {
                    vec3.X = (float)Atan2(mat[i, j], mat[i, k]);
                    vec3.Y = (float)Atan2(y, mat[i, i]);
                    vec3.Z = (float)Atan2(mat[j, i], -mat[k, i]);
                } else {
                    vec3.X = (float)Atan2(-mat[j, k], mat[j, j]);
                    vec3.Y = (float)Atan2(y, mat[i, i]);
                    vec3.Z = 0.0f;
                }
            } else {
                double x = Sqrt(mat[i, i] * mat[i, i] + mat[j, i] * mat[j, i]);
                if (x > 0.00016) {
                    vec3.X = (float)Atan2(mat[k, j], mat[k, k]);
                    vec3.Y = (float)Atan2(-mat[k, i], x);
                    vec3.Z = (float)Atan2(mat[j, i], mat[i, i]);
                } else {
                    vec3.X = (float)Atan2(-mat[j, k], mat[j, j]);
                    vec3.Y = (float)Atan2(-mat[k, i], x);
                    vec3.Z = 0.0f;
                }
            }
            if (parity == EulerParity.Odd) {
                vec3.X = -vec3.X;
                vec3.Y = -vec3.Y;
                vec3.Z = -vec3.Z;
            }
            if (frame == EulerFrame.R) {
                double x = vec3.X;
                vec3.X = vec3.Z;
                vec3.Z = (float)x;
            }
            
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (vec3.X == -3.14159274f && vec3.Y == 0 && vec3.Z == 0) {
                vec3 = new teVec3(0, 3.14159274f, 3.14159274f); 
                // effectively the same but you know, eulers.
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator
            
            return vec3;
        }
        
        public static teQuat Multiply(ref teQuat left, ref teQuat right) {
            teQuat result;
            float lx = left.X;
            float ly = left.Y;
            float lz = left.Z;
            float lw = left.W;
            float rx = right.X;
            float ry = right.Y;
            float rz = right.Z;
            float rw = right.W;
            float a = ly * rz - lz * ry;
            float b = lz * rx - lx * rz;
            float c = lx * ry - ly * rx;
            float d = lx * rx + ly * ry + lz * rz;
            result.X = lx * rw + rx * lw + a;
            result.Y = ly * rw + ry * lw + b;
            result.Z = lz * rw + rz * lw + c;
            result.W = lw * rw - d;
            return result;
        }

        public static implicit operator Quaternion(teQuat quat) {
            return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }
    }
}