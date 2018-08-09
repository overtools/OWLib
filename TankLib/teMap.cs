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

        public teResourceGUID EntityDefinition;
        public teResourceGUID SkyEnvironmentCubemap;
        public teResourceGUID BakedLighting;
        public teResourceGUID BakedShadow;
        public teResourceGUID LUT;
        public teResourceGUID SkyboxModel;
        public teResourceGUID SkyboxModelLook;
        public teResourceGUID MapEnvironmentSound; // 055 file.
        public teResourceGUID GroundEnvironmentCubemap;
        public teResourceGUID BlendEnvironmentCubemap;
        public teResourceGUID Text;
        public teResourceGUID Guid0B5;

        public teMtx43 M4;
        public teMtx44 M5;
        public teMtx44 M6;
        public teMtx43 M7;

        public ulong UnknownGUID2;
    }
}