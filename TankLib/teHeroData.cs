using TankLib.DataSerializer;
using static TankLib.DataSerializer.Logical;

namespace TankLib
{
    public class teHeroData : ReadableData
    {
        public uint SkinId;
        public uint WeaponSkinId;
        public uint HighlightIntro;
        [DynamicSizeArray(typeof(int), typeof(uint))]
        public uint[] SprayIds;
        [DynamicSizeArray(typeof(int), typeof(uint))]
        public uint[] VoiceLineIds;
        [DynamicSizeArray(typeof(int), typeof(uint))]
        public uint[] EmoteIds;
        public uint AnnouncerId; // this actually is an educated guess, every hero has a cosmetic category which has one cosmetic (total.) This used to be the same for weapon skins.
        // Since there's an underlying system for announcer logic including it's own STU object, it's safe to assume this.
        public teResourceGUID Hero;
    }
}
