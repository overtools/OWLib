using System;

namespace TankLib {
    public class Exceptions {
        public class UnknownStructuredDataFieldException : Exception {
            public UnknownStructuredDataFieldException(string message) : base(message) { }
        }
        
        /// <summary>Thrown when a teTexture doesn't have required payload data</summary>
        public class TexturePayloadMissingException : Exception {}
        
        /// <summary>Thrown when a teTexture doesn't require a payload, but it is given one</summary>
        public class TexturePayloadNotRequiredException : Exception {}
        
        /// <summary>Thrown when a teTexture already has a payload</summary>
        public class TexturePayloadAlreadyExistsException : Exception {}
    }
}