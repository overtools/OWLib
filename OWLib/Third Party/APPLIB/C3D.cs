// Decompiled with JetBrains decompiler

using System;

namespace APPLIB {
    public class C3D {
        private enum EulerParity {
            Even,
            Odd,
        }

        private enum EulerRepeat {
            No,
            Yes,
        }

        private enum EulerFrame {
            S,
            R,
        }

        public static Vector3D ToEulerAngles(Quaternion3D q) {
            return Eul_FromQuat(q, 0, 1, 2, 0, EulerParity.Even, EulerRepeat.No, EulerFrame.S);
        }

        private static Vector3D Eul_FromQuat(Quaternion3D q, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame) {
            double[,] M = new double[4, 4];
            double num1 = (double)q.i * (double)q.i + (double)q.j * (double)q.j + (double)q.k * (double)q.k + (double)q.real * (double)q.real;
            double num2 = num1 <= 0.0 ? 0.0 : 2.0 / num1;
            double num3 = (double)q.i * num2;
            double num4 = (double)q.j * num2;
            double num5 = (double)q.k * num2;
            double num6 = (double)q.real * num3;
            double num7 = (double)q.real * num4;
            double num8 = (double)q.real * num5;
            double num9 = (double)q.i * num3;
            double num10 = (double)q.i * num4;
            double num11 = (double)q.i * num5;
            double num12 = (double)q.j * num4;
            double num13 = (double)q.j * num5;
            double num14 = (double)q.k * num5;
            M[0, 0] = 1.0 - (num12 + num14);
            M[0, 1] = num10 - num8;
            M[0, 2] = num11 + num7;
            M[1, 0] = num10 + num8;
            M[1, 1] = 1.0 - (num9 + num14);
            M[1, 2] = num13 - num6;
            M[2, 0] = num11 - num7;
            M[2, 1] = num13 + num6;
            M[2, 2] = 1.0 - (num9 + num12);
            M[3, 3] = 1.0;
            return Eul_FromHMatrix(M, i, j, k, h, parity, repeat, frame);
        }

        private static Vector3D Eul_FromHMatrix(double[,] M, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame) {
            Vector3D vector3D = new Vector3D();
            if (repeat == EulerRepeat.Yes) {
                double y = Math.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
                if (y > 0.00016) {
                    vector3D.X = (float)Math.Atan2(M[i, j], M[i, k]);
                    vector3D.Y = (float)Math.Atan2(y, M[i, i]);
                    vector3D.Z = (float)Math.Atan2(M[j, i], -M[k, i]);
                } else {
                    vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
                    vector3D.Y = (float)Math.Atan2(y, M[i, i]);
                    vector3D.Z = 0.0f;
                }
            } else {
                double x = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
                if (x > 0.00016) {
                    vector3D.X = (float)Math.Atan2(M[k, j], M[k, k]);
                    vector3D.Y = (float)Math.Atan2(-M[k, i], x);
                    vector3D.Z = (float)Math.Atan2(M[j, i], M[i, i]);
                } else {
                    vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
                    vector3D.Y = (float)Math.Atan2(-M[k, i], x);
                    vector3D.Z = 0.0f;
                }
            }
            if (parity == EulerParity.Odd) {
                vector3D.X = -vector3D.X;
                vector3D.Y = -vector3D.Y;
                vector3D.Z = -vector3D.Z;
            }
            if (frame == EulerFrame.R) {
                double x = (double)vector3D.X;
                vector3D.X = vector3D.Z;
                vector3D.Z = (float)x;
            }
            return vector3D;
        }
    }
}
