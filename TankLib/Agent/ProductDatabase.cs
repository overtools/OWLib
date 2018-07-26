using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using TankLib.Agent.Protobuf;

namespace TankLib.Agent
{
    public class ProductDatabase
    {
        public string FilePath { get; }
        public Database Data { get; }

        public ProductDatabase() : this(null, false) { }

        public ProductDatabase(string path) : this(path, true) { }

        public ProductDatabase(string path, bool singleInstall)
        {
            FilePath = path;
            if(string.IsNullOrWhiteSpace(FilePath))
            {
                FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent", "product.db");
            }

            using (Stream product = File.OpenRead(FilePath))
            {
                if (singleInstall)
                {
                    Data = new Database
                    {
                        ProductInstalls = new List<ProductInstall>
                        {
                            Serializer.Deserialize<ProductInstall>(product)
                        }
                    };
                }
                else
                {
                    Data = Serializer.Deserialize<Database>(product);
                }
            }
        }
    }
}
