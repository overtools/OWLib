using System;
using System.IO;

namespace DataTool.ConvertLogic.WEM {
    public class BankSoundStructure : IBankObject { // might as well use interface
        public enum AdditionalParameterType : byte {
            VoiceVolume = 0x0, // General Settings: Voice: Volume, float
            VoicePitch = 0x2, // General Settings: Voice: Pitch, float
            VoiceLowPassFilter = 0x3, // General Settings: Voice: Low-pass filter, float
            PlaybackPriority = 0x5, // Advanced Settings: Playback Priority: Priority, float
            PlaybackPriortyOffset = 0x6, // Advanced Settings: Playback Priority: Offset priority by ... at max distance, float
            Loop = 0x7, // whether to Loop, given as uint32 = number of loops, or infinite if the value is 0
            MotionVolumeOffset = 0x8, // Motion: Audio to Motion Settings: Motion Volume Offset, float
            PositioningPannerX = 0xB, // Positioning: 2D: Panner X-coordinate, float
            PositioningPannerX2 = 0xC, // todo: erm, wiki?
            PositioningCenter = 0xD, // Positioning: Center %, float
            Bus0Volume = 0x12, // General Settings: User-Defined Auxiliary Sends: Bus #0 Volume, float
            Bus1Volume = 0x13, // General Settings: User-Defined Auxiliary Sends: Bus #1 Volume, float
            Bus2Volume = 0x14, // General Settings: User-Defined Auxiliary Sends: Bus #2 Volume, float
            Bus3Volume = 0x15, // General Settings: User-Defined Auxiliary Sends: Bus #3 Volume, float
            AuxiliarySendsVolume = 0x16, // General Settings: Game-Defined Auxiliary Sends: Volume, float
            OutputBusVolume = 0x17, // General Settings: Output Bus: Volume, float
            OutputBusLowPassFilter = 0x18 // General Settings: Output Bus: Low-pass filter, float
        }

        public void Read(BinaryReader reader) {
            // untested but you can try if you are brave
        #if I_CAN_SIMPLY_SNAP_MY_FINGERS
                bool overrideParentSettingsEffect = reader.ReadBoolean();  // whether to override parent settings for Effects section
                byte numEffects = reader.ReadByte();

                if (numEffects > 0) {
                    byte mask = reader.ReadByte(); // bit mask specifying which effects should be bypassed (see wiki)

                    for (int i = 0; i < numEffects; i++) {
                        byte effectIndex = reader.ReadByte();  // effect index (00 to 03)
                        uint effectID = reader.ReadUInt32();  // id of Effect object
                        short zero = reader.ReadInt16();  // two zero bytes
                        Debug.Assert(zero == 0);
                    }
                }

                uint outputBus = reader.ReadUInt32();
                uint parentObject = reader.ReadUInt32();
                bool overrideParentSettingsPlaybackPriority = reader.ReadBoolean();  // whether to override parent settings for Playback Priority section
                bool offsetPriorityBy = reader.ReadBoolean();  // whether the "Offset priority by ... at max distance" setting is activated

                byte numAdditionalParameters = reader.ReadByte();
                AdditionalParameterType[] parameterTypes = new AdditionalParameterType[numAdditionalParameters];
                for (int i = 0; i < numAdditionalParameters; i++) {
                    AdditionalParameterType type = (AdditionalParameterType)reader.ReadByte();
                    parameterTypes[i] = type;
                }

                // byte zero2 = reader.ReadByte();
                // Debug.Assert(zero2 == 0);
        #else
            throw new NotImplementedException();
        #endif
        }
    }
}