using TankLib.Helpers.DataSerializer;

namespace TankLib.Replay
{
    public class HeroData : ReadableData
    {
        public uint SkinId;
        public uint WeaponSkinId;
        public uint POTGAnimation;
        [Logical.DynamicSizeArrayAttribute(typeof(int), typeof(uint))]
        public uint[] SprayIds;
        [Logical.DynamicSizeArrayAttribute(typeof(int), typeof(uint))]
        public uint[] VoiceLineIds;
        [Logical.DynamicSizeArrayAttribute(typeof(int), typeof(uint))]
        public uint[] EmoteIds;
        public uint AnnouncerId; // this actually is an educated guess, every hero has a cosmetic category which has one cosmetic (total.) This used to be the same for weapon skins.
        // Since there's an underlying system for announcer logic including it's own STU object, it's safe to assume this.
        public teResourceGUID Hero;
    }
}
