using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.ToolLogic.List;
using TACTLib.Agent;
using TACTLib.Agent.Protobuf;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;
using TankLib.TACT;

namespace DataTool.ToolLogic.Dbg;

[Tool("debug-install-issues", Description = "", CustomFlags = typeof(ListFlags), UtilNoArchiveNeeded = true)]
class DebugInstallIssues : ITool {
    public void Parse(ICLIFlags toolFlags) {
        using var output = new StreamWriter("install-issues.txt");
        
        //output.WriteLine("---- SYSTEM ----");

        var args = new ClientCreateArgs {
            SpeechLanguage = Program.Flags.SpeechLanguage,
            TextLanguage = Program.Flags.Language,
            HandlerArgs = new ClientCreateArgs_Tank {
                ManifestRegion = Program.Flags.RCN ? ProductHandler_Tank.REGION_CN : ProductHandler_Tank.REGION_DEV,
                LoadManifest = false // !
            },
            Online = false,

            RemoteKeyringUrl = "https://raw.githubusercontent.com/overtools/OWLib/master/TankLib/Overwatch.keyring"
        };

        LoadHelper.PreLoad();
        var client = new ClientHandler(Program.Flags.OverwatchDirectory, args);
        LoadHelper.PostLoad(client);
        
        output.WriteLine("---- CONTAINER ----");
        {
            var directoryInfo = new DirectoryInfo(Program.Flags.OverwatchDirectory);
            output.WriteLine($"Drive Letter: {directoryInfo.Root.Name}");
            output.WriteLine($"Directory Creation Date: {directoryInfo.CreationTimeUtc}");
            output.WriteLine($"Directory Modified Date: {directoryInfo.LastWriteTimeUtc}");
            output.WriteLine();
        }
        {
            var dataDirectory = Path.Combine(client.ContainerHandler!.ContainerDirectory, ContainerHandler.DataDirectory);

            var dataFiles = Directory.GetFiles(dataDirectory, "data.*");
            var dataFileCount = dataFiles.Length;
            var indexFileCount = Directory.GetFiles(dataDirectory, "*.idx").Length;
            var otherFileCount = Directory.GetFiles(dataDirectory).Length - dataFileCount - indexFileCount;
            
            output.WriteLine($"Data File Count: {dataFileCount}");
            output.WriteLine($"Index File Count: {indexFileCount}");
            output.WriteLine($"Other File Count: {otherFileCount}");
            output.WriteLine($"Total Data File Size: {dataFiles.Sum(x => new FileInfo(x).Length)}");

            var countOkay = new Dictionary<int, int>();
            var countFailedHeaderRead = new Dictionary<int, int>();
            var countSizeZero = new Dictionary<int, int>();
            var countSizeWrong = new Dictionary<int, int>();
            foreach (var dataFileIndex in client.ContainerHandler.GetDataFileIndices()) {
                countFailedHeaderRead.Add(dataFileIndex, 0);
                countOkay.Add(dataFileIndex, 0);
                countSizeZero.Add(dataFileIndex, 0);
                countSizeWrong.Add(dataFileIndex, 0);
            }
            
            foreach (var localIndexEntry in client.ContainerHandler.IndexEntries.Values) {
                if (!client.ContainerHandler.OpenIndexEntryForDebug(localIndexEntry, out var header)) {
                    countFailedHeaderRead[localIndexEntry.Index]++;
                    continue;
                }

                if (header.m_size != localIndexEntry.EncodedSize) {
                    countSizeWrong[localIndexEntry.Index]++;
                    if (header.m_size == 0) countSizeZero[localIndexEntry.Index]++;
                } else {
                    countOkay[localIndexEntry.Index]++;
                }
            }

            foreach (var dataFileIndex in countSizeZero.Keys.OrderBy(x => x)) {
                var dataFilePath = client.ContainerHandler.GetDataFilePath(dataFileIndex);
                
                output.WriteLine($"Data File[{dataFileIndex}]:");
                output.WriteLine($"  Size: {new FileInfo(dataFilePath).Length}");
                output.WriteLine($"  Count Failed Header Read: {countFailedHeaderRead[dataFileIndex]}");
                output.WriteLine($"  Count Okay: {countOkay[dataFileIndex]}");
                output.WriteLine($"  Count Size Zero: {countSizeZero[dataFileIndex]}");
                output.WriteLine($"  Count Size Wrong: {countSizeWrong[dataFileIndex]}");
            }
        }

        //output.Write($"Install Directory: {Program.Flags.OverwatchDirectory}");

        var agentInfo = AnonymizeAgentInfo(client.AgentProduct);
        output.WriteLine();
        output.WriteLine("---- AGENT ----");

        output.WriteLine($"Has Game Agent Info: {agentInfo != null}");
        if (agentInfo != null) {
            output.WriteLine($"Game Agent Info: {agentInfo}");
        }

        AgentDatabase globalAgentDatabase = null;
        try {
            globalAgentDatabase = new AgentDatabase();
        } catch { }

        output.WriteLine($"Has Global Agent Info: {globalAgentDatabase != null}");
        if (globalAgentDatabase != null) {
            var targetProducts = new[] { "pro", "bna", "agent" };
            foreach (var product in targetProducts) {
                output.WriteLine($"Global Agent Info[{product}]: {AnonymizeAgentInfo(globalAgentDatabase.Data.ProductInstall.FirstOrDefault(x => x.ProductCode == product))}");
            }
        }
    }

    private ProductInstall AnonymizeAgentInfo(ProductInstall agentInfo) {
        if (agentInfo == null) return null;
        
        // remove possibly identifying information
        agentInfo = agentInfo.Clone();
        agentInfo.Settings.InstallPath = "";
        agentInfo.Settings.PlayRegion = "";
        agentInfo.Settings.AccountCountry = "";
        agentInfo.Settings.GeoIpCountry = "";
        agentInfo.CachedProductState.BaseProductState.ActiveTagString = ""; // has geoip
        return agentInfo;
    }
}