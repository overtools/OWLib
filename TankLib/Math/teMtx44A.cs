using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x4 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMtx44A {  // todo: what does "A" mean?
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
        
        public float M13;
        public float M14;
        public float M15;
        public float M16;
    }
}