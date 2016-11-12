using System.IO;

namespace OWLib.Types.Chunk {
  public interface IChunk {
    string Identifier
    {
      get;
    }

    void Parse(Stream input);
  }
}
