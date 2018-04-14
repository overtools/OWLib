using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TankLib.Agent;
using TankLib.Agent.Protobuf;
using TankView.ObjectModel;

namespace TankView.ViewModel
{
    public class ProductLocations : ObservableHashCollection<ProductLocation>
    {
        private static Dictionary<string, string> KnownUIDs = new Dictionary<string, string>
        {
            { "prometheus", "Live Region" },
            { "prometheus_dev", "Development Region" },
            { "prometheus_test", "Public Test Region" },
            { "prometheus_tournament", "Professional Region" }
        };

        public ProductLocations()
        {
            try
            {
                ProductDatabase pdb = new ProductDatabase();
                foreach(ProductInstall install in pdb.Data.ProductInstalls)
                {
                    if(KnownUIDs.ContainsKey(install.Uid) && Directory.Exists(install.Settings.InstallPath))
                    {
                        Add(new ProductLocation(KnownUIDs[install.Uid], Path.GetFullPath(install.Settings.InstallPath)));
                    }
                }
            }
            catch
            {

            }
        }
    }
}
