using System;
using STULib.Types.Generic;

namespace STULib {
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class STUSuppressWarningAttribute : Attribute {
        public uint ThisInstance;
        public STUWarningType Type;
        public uint FieldChecksum;
        
        public Common.InstanceUsage InstanceType;
        public uint InstanceChecksum;
        
        public STUSuppressWarningAttribute(uint thisInstance,STUWarningType type, uint fieldChecksum) {
            ThisInstance = thisInstance;
            Type = type;
            FieldChecksum = fieldChecksum;
        }
        
        public STUSuppressWarningAttribute(uint thisInstance, STUWarningType type, Common.InstanceUsage instanceType, uint fieldChecksum, uint instanceChecksum) {
            ThisInstance = thisInstance;
            Type = type;
            FieldChecksum = fieldChecksum;
            InstanceChecksum = instanceChecksum;
            InstanceType = instanceType;
        }
    }

    public enum STUWarningType : uint {
        None = 0,
        MissingField = 1,
        MissingInstance = 2
    }
}