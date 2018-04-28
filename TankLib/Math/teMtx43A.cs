using System;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x4 (4x3+A) matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMtx43A {
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
        
        // A:
        public float M13;
        public float M14;
        public float M15;
        public float M16;
        
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
                    case 3:
                        switch (columnIndex) {
                            case 0:
                                return M14;
                            case 1:
                                return M15;
                            case 2:
                                return M15;
                            case 3:
                                return M16;
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
                    case 3:
                        switch (columnIndex) {
                            case 0:
                                M13 = value;
                                break;
                            case 1:
                                M14 = value;
                                break;
                            case 2:
                                M15 = value;
                                break;
                            case 3:
                                M16 = value;
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