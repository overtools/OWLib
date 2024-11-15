using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>2 component XY vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("X: {X}, Y: {Y}")]
    public struct teVec2 {
        /// <summary>X component</summary>
        public float X;

        /// <summary>Y component</summary>
        public float Y;

        public teVec2(float x, float y) {
            X = x;
            Y = y;
        }

        public static teVec2 FromHalf(IReadOnlyList<Half> val) {
            if (val.Count != 2) {
                throw new InvalidDataException();
            }
            return new teVec2((float)val[0], (float)val[1]);
        }

        public static teVec2 operator +(teVec2 left, teVec2 right) {
            return new teVec2(left.X + right.X, left.Y + right.Y);
        }

        public static teVec2 operator -(teVec2 left, teVec2 right) {
            return new teVec2(left.X - right.X, left.Y - right.Y);
        }
    }
}