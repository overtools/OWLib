using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x3 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("{" + nameof(DebugString) + "}")]
    public struct teMtx43 {
        public float M01;
        public float M02;
        public float M03;
        public float M04;
        
        public float M05;
        public float M06;
        public float M07;
        public float M08;
        
        public float M09;
        public float M10;
        public float M11;
        public float M12;
        
        public string DebugString => $"{M01:F3} {M02:F3} {M03:F3} {M04:F3}\r\n" +
                                     $"{M05:F3} {M06:F3} {M07:F3} {M08:F3}\r\n" +
                                     $"{M09:F3} {M10:F3} {M11:F3} {M12:F3}";
        
        // ewwwwwwwwwww
        public float this[int rowIndex, int columnIndex] {
            get {
                switch (rowIndex) {
                    case 0:
                        switch (columnIndex) {
                            case 0:
                                return M01;
                            case 1:
                                return M02;
                            case 2:
                                return M03;
                            case 3:
                                return M04;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                    case 1:
                        switch (columnIndex) {
                            case 0:
                                return M05;
                            case 1:
                                return M06;
                            case 2:
                                return M07;
                            case 3:
                                return M08;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                    case 2:
                        switch (columnIndex) {
                            case 0:
                                return M09;
                            case 1:
                                return M10;
                            case 2:
                                return M11;
                            case 3:
                                return M12;
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
                                M01 = value;
                                break;
                            case 1:
                                M02 = value;
                                break;
                            case 2:
                                M03 = value;
                                break;
                            case 3:
                                M04 = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                        break;
                    case 1:
                        switch (columnIndex) {
                            case 0:
                                M05 = value;
                                break;
                            case 1:
                                M06 = value;
                                break;
                            case 2:
                                M07 = value;
                                break;
                            case 3:
                                M08 = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " + columnIndex + ")");
                        }
                        break;
                    case 2:
                        switch (columnIndex) {
                            case 0:
                                M09 = value;
                                break;
                            case 1:
                                M10 = value;
                                break;
                            case 2:
                                M11 = value;
                                break;
                            case 3:
                                M12 = value;
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