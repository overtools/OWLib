using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(3)]
    public class BankObjectEventAction : IBankObject {
        public enum EventActionScope : byte {
            GameObjectSwitchOrTrigger = 1, // Switch or Trigger
            Global = 2,
            GameObjectReference = 3, // see referenced object id
            GameObjectState = 4,
            All = 5,
            AllExceptReference = 5, // see referenced object id
        }

        public enum EventActionType : byte {
            Stop = 0x1,
            Pause = 0x2,
            Resume = 0x3,
            Play = 0x4,
            Trigger = 0x5,
            Mute = 0x6,
            UnMute = 0x7,
            SetVoicePitch = 0x8,
            ResetVoicePitch = 0x9,
            SetVoiceVolume = 0xA,
            ResetVoiceVolume = 0xB,
            SetBusVolume = 0xC,
            ResetBusVolume = 0xD,
            SetVoiceLowpassFilter = 0xE,
            ResetVoiceLowpassFilter = 0xF,
            EnableState = 0x10,
            DisableState = 0x11,
            SetState = 0x12,
            SetGameParameter = 0x13,
            ResetGameParameter = 0x14,
            SetSwitch = 0x19,
            EnableBypassOrDisableBypass = 0x1A,
            ResetBypassEffect = 0x1B,
            Break = 0x1C,
            Seek = 0x1E
        }

        public enum EventActionParameterType : byte {
            Delay = 0xE, // Delay, given as uint32 in milliseconds
            Play = 0xF, // Play: Fade in time, given as uint32 in milliseconds

            // may not be fade in time, but start time. (wouldn't that be a delay though?)
            Probability = 0x10 // Probability, given as float
        }

        public EventActionScope Scope;
        public EventActionType Type;
        public uint ReferenceObjectID;

        public List<KeyValuePair<EventActionParameterType, object>> Parameters;

        public void Read(BinaryReader reader) {
            Scope = (EventActionScope) reader.ReadByte();
            Type = (EventActionType) reader.ReadByte();
            ReferenceObjectID = reader.ReadUInt32();
            byte zero = reader.ReadByte();
            byte parameterCount = reader.ReadByte();
            Parameters = new List<KeyValuePair<EventActionParameterType, object>>(parameterCount);
            EventActionParameterType[] tempTypes = new EventActionParameterType[parameterCount];
            for (int i = parameterCount - 1; i >= 0; i--) {
                EventActionParameterType parameterType = (EventActionParameterType) reader.ReadByte();
                tempTypes[i] = parameterType;
            }

            foreach (EventActionParameterType parameterType in tempTypes) {
                object val;
                switch (parameterType) {
                    case EventActionParameterType.Probability:
                        val = reader.ReadSingle();
                        break;
                    case EventActionParameterType.Delay:
                    case EventActionParameterType.Play:
                        val = reader.ReadUInt32();
                        break;
                    default:
                        Debugger.Log(0, "[DataTool.Convertlogic.Sound]", $"Unhandled EventActionParameterTyp: {parameterType}\r\n");
                        // throw new ArgumentOutOfRangeException();
                        continue;
                }

                Parameters.Add(new KeyValuePair<EventActionParameterType, object>(parameterType, val));
            }
        }
    }
}