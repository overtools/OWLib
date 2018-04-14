using ProtoBuf;
using System;
using System.IO;
using TankLib.Agent.Protobuf;

namespace TankLib.Agent
{
    public class ProductDatabase
    {
        public string FilePath { get; }
        public Database Data { get; }

        public ProductDatabase(string path = null)
        {
            FilePath = path;
            if(string.IsNullOrWhiteSpace(FilePath))
            {
                FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent", "product.db");
            }

            using (Stream product = File.OpenRead(FilePath))
            {
                Data = Serializer.Deserialize<Database>(product);
            }
        }
    }
}
