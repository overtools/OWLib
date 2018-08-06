using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;

namespace TankLib.Math {
    /// <summary>4x3 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("{" + nameof(DebugString) + "}")]
    public struct teMtx43 {
        public float M11;
        public float M12;
        public float M13;
        public float M14;
        
        public float M21;
        public float M22;
        public float M23;
        public float M24;
        
        public float M31;
        public float M32;
        public float M33;
        public float M34;
        
        public string DebugString => $"{M11:F3} {M12:F3} {M13:F3} {M14:F3}\r\n" +
                                     $"{M21:F3} {M22:F3} {M23:F3} {M24:F3}\r\n" +
                                     $"{M31:F3} {M32:F3} {M33:F3} {M34:F3}";

        public teMtx43(Matrix matrix) {
            M11 = matrix.M11; M12 = matrix.M12; M13 = matrix.M13; M14 = matrix.M41;
            M21 = matrix.M21; M22 = matrix.M22; M23 = matrix.M23; M24 = matrix.M42;
            M31 = matrix.M31; M32 = matrix.M32; M33 = matrix.M33; M34 = matrix.M43;
        }
        
        public static teMtx43 Identity() {
            return new teMtx43 {
                M11 = 1, M22 = 1, M33 = 1
            };
        }
        
        // ewwwwwwwwwww
        public float this[int rowIndex, int columnIndex] {
            get {
                switch (rowIndex) {
                    case 0:
                        switch (columnIndex) {
                            case 0:
                                return M11;
                            case 1:
                                return M12;
                            case 2:
                                return M13;
                            case 3:
                                return M14;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                    case 1:
                        switch (columnIndex) {
                            case 0:
                                return M21;
                            case 1:
                                return M22;
                            case 2:
                                return M23;
                            case 3:
                                return M24;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                    case 2:
                        switch (columnIndex) {
                            case 0:
                                return M31;
                            case 1:
                                return M32;
                            case 2:
                                return M33;
                            case 3:
                                return M34;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                    default:
                        throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                }
            }
            set {
                switch (rowIndex) {
                    case 0:
                        switch (columnIndex) {
                            case 0:
                                M11 = value;
                                break;
                            case 1:
                                M12 = value;
                                break;
                            case 2:
                                M13 = value;
                                break;
                            case 3:
                                M14 = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                        break;
                    case 1:
                        switch (columnIndex) {
                            case 0:
                                M21 = value;
                                break;
                            case 1:
                                M22 = value;
                                break;
                            case 2:
                                M23 = value;
                                break;
                            case 3:
                                M24 = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                        break;
                    case 2:
                        switch (columnIndex) {
                            case 0:
                                M31 = value;
                                break;
                            case 1:
                                M32 = value;
                                break;
                            case 2:
                                M33 = value;
                                break;
                            case 3:
                                M34 = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                        break;
                    default:
                        throw new IndexOutOfRangeException("You tried to set this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                }
            }
        }
    }
}