using System;
using System.IO;
using System.Linq;
using TankLib.STU;
using TACTLib;

namespace TankLibHelper.Modes {
    public class FindPrimaryClass : IMode {
        public ModeResult Run(string[] args) {
            foreach (var arg in args.Skip(1)) {
                try {
                    using (Stream stream = File.OpenRead(arg)) {
                        teStructuredData structuredData = new teStructuredData(stream);
                        var primary = structuredData.Instances.FirstOrDefault(x => x.Usage == TypeUsage.Root) ?? structuredData.Instances.FirstOrDefault();

                        if (primary == default) {
                            throw new Exception();
                        }

                        Logger.Info(null, $"{Path.GetFileName(arg)}: {primary.GetType().Name}");
                    }
                } catch {
                    Logger.Warn(null, $"Can't find root instance for {arg}");
                }
            }

            return ModeResult.Success;
        }

        public string Mode => "primary";
    }
}
