using System.Runtime.InteropServices;
using TankLib.Math;

namespace TankLib {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMap // literally just a struct for now
    {
        public teMtx43 M0;
        public teMtx44 M1;
        public teMtx44 M2;
        public teMtx44 M3;

        public ulong EntityDefinition;
        public ulong SkyEnvironmentCubemap;
        public ulong BakedLighting;
        public ulong BakedShadow;
        public ulong LUT;
        public ulong SkyboxModel;
        public ulong SkyboxModelLook;
        public ulong MapEnvironmentSound; // 055 file.
        public ulong GroundEnvironmentCubemap;
        public ulong BlendEnvironmentCubemap;
        public ulong Text;
        public ulong UnknownGUID1;

        public teMtx43 M4;
        public teMtx44 M5;
        public teMtx44 M6;
        public teMtx43 M7;

        public ulong UnknownGUID2;
    }
}