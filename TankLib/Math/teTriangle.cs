using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("A: {IndexA}, B: {IndexB}, C: {IndexC}")]
    public struct teTriangle {

        /// <summary>Index 1</summary>
        public int IndexA;

        /// <summary>Index 2</summary>
        public int IndexB;

        /// <summary>Index 3</summary>
        public int IndexC;

        public teTriangle(ushort indexA, ushort indexB, ushort indexC) {
            IndexA = (int) indexA;
            IndexB = (int) indexB;
            IndexC = (int) indexC;
        }

        public teTriangle(IReadOnlyList<ushort> val) {
            if (val.Count != 3) {
                throw new InvalidDataException();
            }
            IndexA = val[0];
            IndexB = val[1];
            IndexC = val[2];
        }

    }
}
