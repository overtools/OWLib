using System.Runtime.Serialization;
using TankTonka.Formatters;
using Utf8Json;

namespace TankTonka.Models {
    public static class Common {
        public enum AssetRepoType : ushort {
            Texture = 0x4,
            Anim = 0x6,
            Material = 0x8,
            Model = 0xC,
            Effect = 0xD,
            DataFlow = 0x13,
            AnimAlias = 0x14,
            AnimCategory = 0x15,
            AnimWeightBoneMask = 0x18,
            ModelLook = 0x1A,
            StatescriptGraph = 0x1B,
            AnimBlendTree = 0x20,
            AnimBlendTreeSet = 0x21,
            GameMessage = 0x25,
            Sound = 0x2C,
            SoundSwitchGroup = 0x2E,
            SoundSwitch = 0x2D,
            SoundParameter = 0x2F,
            SoundStateGroup = 0x30,
            SoundState = 0x31,
            UXLink = 0x36,
            MapCatalog = 0x39,
            UXIDLookup = 0x45,
            ContactSet = 049,
            TexturePayload = 0x4D,
            Font = 0x50,
            FontFamily = 0x51,
            GenericSettings = 0x54,
            SoundSpace = 0x55,
            ProgressionUnlocks = 0x58,
            UXScreen = 0x5A,
            VoiceSet = 0x5F,
            Stat = 0x62,
            Catalog = 0x63,
            MaterialEffect = 0x65,
            Achievement = 0x68,
            VoiceLineSet = 0x70,
            LocaleSettings = 0x72,
            Hero = 0x75,
            VoiceStimulus = 0x78,
            VoiceCategory = 0x79,
            UXDisplayText = 0x7C,
            ShaderGroup = 0x85,
            ShaderInstance = 0x86,
            ShaderCode = 0x87,
            ShaderSource = 0x88,
            ResourceKey = 0x90,
            RaycastType = 0x97,
            RaycastReceiver = 0x98,
            Loadout = 0x9E,
            MapHeader = 0x9F,
            SoundIntegrity = 0xA3,
            Unlock = 0xA5,
            SkinTheme = 0xA6,
            EffectLook = 0xA8,
            MapFont = 0xAA,
            GamePadVibration = 0xAC,
            HeroWeapon = 0xAD,
            UXViewModelSchema = 0xB0,
            VoiceWEMFile = 0xB2,
            MaterialData = 0xB3,
            Movie = 0xB6,
            MapChunk = 0xBC,
            LightingManifest = 0xBD,
            LightingChunk = 0xBE,
            LineupPose = 0xBF,
            GameRuleset = 0xC0,
            GameMode = 0xC5,
            GameRulesetSchema = 0xC6,
            RankedSeason = 0xC8,
            MapShadowData = 0xCB,
            TeamColor = 0xCE,
            LootBox = 0xCF,
            VoiceConversation = 0xD0
        }

        public class StructuredDataInfo {
            [DataMember(Name = "instances")]
            [JsonFormatter(typeof(FourCCArrayFormatter))]
            public uint[] Instances;
        }

        public class ChunkedDataInfo {
            [DataMember(Name = "chunks")]
            [JsonFormatter(typeof(FourCCArrayFormatter))]
            public uint[] Chunks;
        }
    }
}