using System.Collections.Generic;
using System.IO;

namespace TankLib.Chunks {
    /// <summary>Chunk interface, which all chunk types should use</summary>
    public interface IChunk {
        /// <summary>Unique identifier of this chunk type</summary>
        string ID { get; }
        /// <summary>Load the chunk from a stream</summary>
        void Parse(Stream stream);
        
        List<IChunk> SubChunks { get; set; }
    }
}