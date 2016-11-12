using System.IO;

namespace OWLib.Types.Chunk {
  public interface IChunk {
    string Identifier
    {
      get;
    }

    string RootIdentifier
    {
      get;
    }

    void Parse(Stream input);
  }
}
