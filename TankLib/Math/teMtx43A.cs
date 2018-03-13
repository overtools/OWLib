using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x3 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMtx43A {  // todo 43 = 4x3 matrix?
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
    }
}