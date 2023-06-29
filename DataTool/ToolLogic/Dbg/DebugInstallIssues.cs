using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            output.WriteLine($"Directory Creation Date: {directoryInfo.CreationTimeUtc.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine($"Directory Modified Date: {directoryInfo.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}");
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

            var dataFileInfo = new Dictionary<int, DataFileInfo>();
            foreach (var dataFileIndex in client.ContainerHandler.GetDataFileIndices()) {
                dataFileInfo.Add(dataFileIndex, new DataFileInfo());
            }
            
            foreach (var (eKey, localIndexEntry) in client.ContainerHandler.IndexEntries) {
                var dataFile = dataFileInfo[localIndexEntry.Index];
                
                if (!client.ContainerHandler.OpenIndexEntryForDebug(localIndexEntry, out var header, out var fourCC)) {
                    dataFile.m_countFailedHeaderRead++;
                    continue;
                }

                if (header.m_size == localIndexEntry.EncodedSize) {
                    dataFileInfo[localIndexEntry.Index].m_countOkay++;
                    continue;
                }

                // for testing
                //var bad = Random.Shared.NextSingle() <= 1/1000f;
                //if (!bad) {
                //    continue;
                //}
                
                var sample = new HeaderSample {
                    m_ekey = eKey,
                    m_data = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)).ToArray(),
                    m_fourCC = fourCC,
                    m_expectedSize = localIndexEntry.EncodedSize,
                    m_offset = localIndexEntry.Offset
                };

                if (header.m_size == 0) {
                    dataFile.m_countSizeZero++;
                    dataFile.m_sizeZeroSamples.Add(sample);
                } else {
                    dataFile.m_countSizeWrongOther++;
                    dataFile.m_sizeWrongOtherSamples.Add(sample);
                }
            }

            {
                var ekeyMap = new Dictionary<EKey, CKey>(CASCKeyComparer.Instance);
                foreach (var entry in client.EncodingHandler!.Entries.Values) {
                    ekeyMap.TryAdd(entry.EKey.AsEKey(), entry.CKey);
                }

                foreach (var dataFile in dataFileInfo.Values) {
                    foreach (var sample in dataFile.m_sizeZeroSamples) {
                        sample.m_ckey = ekeyMap[sample.m_ekey];
                    }
                    foreach (var sample in dataFile.m_sizeWrongOtherSamples) {
                        sample.m_ckey = ekeyMap[sample.m_ekey];
                    }
                }
            }

            foreach (var (dataFileIndex, info) in dataFileInfo.OrderBy(x => x.Key)) {
                var dataFilePath = client.ContainerHandler.GetDataFilePath(dataFileIndex);
                
                output.WriteLine($"Data File[{dataFileIndex}]:");
                output.WriteLine($"  Size: {new FileInfo(dataFilePath).Length}");
                output.WriteLine($"  Count Failed Header Read: {info.m_countFailedHeaderRead}");
                output.WriteLine($"  Count Okay: {info.m_countOkay}");
                output.WriteLine($"  Count Size Zero: {info.m_countSizeZero}");
                output.WriteLine($"  Count Size Wrong Other: {info.m_countSizeWrongOther}");

                var sizeZeroSamples = info.m_sizeZeroSamples
                    .Take(Math.Min(info.m_sizeZeroSamples.Count, 30))
                    .ToArray();
                var sizeWrongOtherSamples = info.m_sizeWrongOtherSamples
                    .Take(Math.Min(info.m_sizeWrongOtherSamples.Count, 30))
                    .ToArray();

                void LogSample(HeaderSample sample) {
                    output.WriteLine($"    EKey: {sample.m_ekey.ToHexString().ToLowerInvariant()}");
                    output.WriteLine($"    CKey: {sample.m_ckey.ToHexString().ToLowerInvariant()}");
                    output.WriteLine($"    Header: {Convert.ToHexString(sample.m_data).ToLowerInvariant()}");
                    output.WriteLine($"    FourCC: {sample.m_fourCC:X8}");
                    output.WriteLine($"    Expected Size: {sample.m_expectedSize}");
                    output.WriteLine($"    Offset: 0x{sample.m_offset:X}");
                }

                for (int i = 0; i < sizeZeroSamples.Length; i++) {
                    output.WriteLine($"  Size Zero Samples[{i}]:");
                    LogSample(sizeZeroSamples[i]);
                }
                for (int i = 0; i < sizeWrongOtherSamples.Length; i++) {
                    output.WriteLine($"  Size Wrong Other Samples[{i}]:");
                    LogSample(sizeWrongOtherSamples[i]);
                }
            }
        }

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

    private class DataFileInfo {
        public int m_countFailedHeaderRead;
        public int m_countOkay;
        public int m_countSizeZero;
        public int m_countSizeWrongOther;

        public readonly List<HeaderSample> m_sizeZeroSamples = new List<HeaderSample>();
        public readonly List<HeaderSample> m_sizeWrongOtherSamples = new List<HeaderSample>();
    }

    private class HeaderSample {
        public EKey m_ekey;
        public byte[] m_data;
        public uint m_fourCC;
        public uint m_expectedSize;
        public uint m_offset;
        
        public CKey m_ckey;
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