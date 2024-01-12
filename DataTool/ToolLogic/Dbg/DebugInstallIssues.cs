using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DataTool.Flag;
using DataTool.ToolLogic.List;
using TACTLib.Agent;
using TACTLib.Agent.Protobuf;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Container;
using TACTLib.Core;
using TACTLib.Core.Key;
using TACTLib.Core.Product.Tank;
using TankLib;
using TankLib.Helpers;
using TankLib.TACT;
using CKey=TACTLib.Core.Key.FullKey;
using EKey=TACTLib.Core.Key.TruncatedKey;

namespace DataTool.ToolLogic.Dbg;

[Tool("debug-install-issues", Description = "Collect debugging information about the game install", CustomFlags = typeof(ListFlags), UtilNoArchiveNeeded = true, IsSensitive = true)]
class DebugInstallIssues : ITool {
    public void Parse(ICLIFlags toolFlags) {
        const string filename = "install-issues.txt";
        using var output = new StreamWriter(filename);

        var args = new ClientCreateArgs {
            SpeechLanguage = Program.Flags.SpeechLanguage,
            TextLanguage = Program.Flags.Language,
            HandlerArgs = new ClientCreateArgs_Tank {
                ManifestRegion = Program.Flags.RCN ? ProductHandler_Tank.REGION_CN : ProductHandler_Tank.REGION_DEV,
                LoadManifest = true,
                LoadBundlesForLookup = false
            },
            Online = false
            
            // dont need keys or anything..
        };

        LoadHelper.PreLoad();
        var client = new ClientHandler(Program.Flags.OverwatchDirectory, args);
        LoadHelper.PostLoad(client);

        if (client.ContainerHandler is not ContainerHandler dynamicContainer) {
            Logger.Error("DebugInstallIssues", "This mode is only relevant for Battle.net installs"); 
            return;
        }
        
        int overwatchAssetCount = 0;
        var nonResidentOverwatchAssetCount = 0;
        var dataFileInfo = new Dictionary<int, DataFileInfo>();
        {
            foreach (var dataFileIndex in dynamicContainer.GetDataFileIndices()) {
                dataFileInfo.Add(dataFileIndex, new DataFileInfo());
            }

            foreach (var (eKey, localIndexEntry) in dynamicContainer.GetIndexEntries()) {
                var dataFile = dataFileInfo[localIndexEntry.Index];

                if (!dynamicContainer.OpenIndexEntryForDebug(localIndexEntry, out var header, out var fourCC)) {
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
                //    dataFileInfo[localIndexEntry.Index].m_countOkay++;
                //    continue;
                //}

                var sample = new HeaderSample {
                    m_ekey = eKey,
                    m_data = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)).ToArray(),
                    m_fourCC = fourCC,
                    m_expectedSize = localIndexEntry.EncodedSize,
                    m_offset = localIndexEntry.Offset
                };

                dataFile.m_allBadSamples.Add(sample);

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
                foreach (var ckey in client.EncodingHandler!.GetCKeys()) {
                    if (!client.EncodingHandler.TryGetEncodingEntry(ckey, out var eKeys)) continue;
                    foreach (var ekey in eKeys) {
                        ekeyMap.TryAdd(ekey.AsTruncated(), ckey);
                    }
                }

                foreach (var dataFile in dataFileInfo.Values) {
                    foreach (var sample in dataFile.m_sizeZeroSamples) {
                        ekeyMap.TryGetValue(sample.m_ekey, out sample.m_ckey);
                    }

                    foreach (var sample in dataFile.m_sizeWrongOtherSamples) {
                        ekeyMap.TryGetValue(sample.m_ekey, out sample.m_ckey);
                    }
                }
            }

            // guard to theoretically allow different products...
            if (client.ProductHandler is ProductHandler_Tank tankHandler) {
                var ckeyMap = new Dictionary<CKey, teResourceGUID>(CASCKeyComparer.Instance);
                foreach (var guid in tankHandler.m_assets.Keys) {
                    var cmf = tankHandler.GetContentManifestForAsset(guid);
                    var asset = cmf.GetHashData(guid);

                    ckeyMap.TryAdd(asset.ContentKey, (teResourceGUID) asset.GUID);
                }

                overwatchAssetCount = ckeyMap.Count;

                foreach (var dataFile in dataFileInfo.Values) {
                    foreach (var sample in dataFile.m_sizeZeroSamples) {
                        ckeyMap.TryGetValue(sample.m_ckey, out sample.m_guid);
                    }

                    foreach (var sample in dataFile.m_sizeWrongOtherSamples) {
                        ckeyMap.TryGetValue(sample.m_ckey, out sample.m_guid);
                    }
                }

                foreach (var guid in tankHandler.m_assets.Keys) {
                    var cmf = tankHandler.GetContentManifestForAsset(guid);
                    var asset = cmf.GetHashData(guid);

                    if (client.EncodingHandler.TryGetEncodingEntry(asset.ContentKey, out var eKeys)) {
                        var found = false;
                        foreach (var eKey in eKeys) {
                            if (!client.ContainerHandler.CheckResidency(eKey)) continue;
                            found = true;
                            break;
                        }
                        if (!found) nonResidentOverwatchAssetCount++;
                    } else {
                        // bundled. its okay
                    }
                }
            }
        }
        
        AgentDatabase globalAgentDatabase = null;
        try {
            globalAgentDatabase = new AgentDatabase();
        } catch { }

        {
            output.WriteLine("---- SUMMARY ----");

            if (globalAgentDatabase != null) {
                var targetProducts = new[] { "agent", "bna" };
                foreach (var product in targetProducts) {
                    var productInfo = AnonymizeAgentInfo(globalAgentDatabase.Data.ProductInstall.FirstOrDefault(x => x.ProductCode == product));
                    
                    var baseProductState = productInfo?.CachedProductState?.BaseProductState;
                    var versionStr = baseProductState?.CurrentVersionStr ?? baseProductState?.CurrentVersion; // never seen second but sanity
                    output.WriteLine($"Global Agent Info[{product}].Version: {versionStr}");
                }
            }
            
            output.WriteLine($"Total Okay Count: {dataFileInfo.Values.Sum(x => x.m_countOkay)}");
            output.WriteLine($"Total Size Zero Count: {dataFileInfo.Values.Sum(x => x.m_countSizeZero)}");
            output.WriteLine($"Total Size Wrong Other Count: {dataFileInfo.Values.Sum(x => x.m_countSizeWrongOther)}");
            output.WriteLine();
        }
        
        output.WriteLine("---- CONTAINER ----");
        {
            var directoryInfo = new DirectoryInfo(Program.Flags.OverwatchDirectory);
            output.WriteLine($"Drive Letter: {directoryInfo.Root.Name}");
            output.WriteLine($"Directory Creation Date: {directoryInfo.CreationTimeUtc.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine($"Directory Modified Date: {directoryInfo.LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine();
        }

        output.WriteLine($"Total Okay Count: {dataFileInfo.Values.Sum(x => x.m_countOkay)}");
        output.WriteLine($"Total Size Zero Count: {dataFileInfo.Values.Sum(x => x.m_countSizeZero)}");
        output.WriteLine($"Total Size Wrong Other Count: {dataFileInfo.Values.Sum(x => x.m_countSizeWrongOther)}");
        output.WriteLine($"Total Bad Count: {dataFileInfo.Values.Sum(x => x.m_countSizeZero + x.m_countSizeWrongOther)}");
        output.WriteLine($"Total Bad Count With BLTE Magic: {dataFileInfo.Values.Sum(x =>
            x.m_allBadSamples.Count(y => y.m_fourCC == BLTEStream.Magic))}"); 
        output.WriteLine($"Total Overwatch Asset Count: {overwatchAssetCount}"); 
        output.WriteLine($"Total NON-RESIDENT Overwatch Asset Count: {nonResidentOverwatchAssetCount}"); 
        output.WriteLine($"Total Bad Overwatch Asset Count: {dataFileInfo.Values.Sum(x => 
            x.m_allBadSamples.Count(y => y.m_guid != 0))}");
        output.WriteLine();

        {
            var dataDirectory = Path.Combine(dynamicContainer.ContainerDirectory, ContainerHandler.DataDirectory);
            var dataFiles = Directory.GetFiles(dataDirectory, "data.*");
            var dataFileCount = dataFiles.Length;
            var indexFileCount = Directory.GetFiles(dataDirectory, "*.idx").Length;
            var otherFileCount = Directory.GetFiles(dataDirectory).Length - dataFileCount - indexFileCount;
            output.WriteLine($"Data File Count: {dataFileCount}");
            output.WriteLine($"Index File Count: {indexFileCount}");
            output.WriteLine($"Other File Count: {otherFileCount}");
            output.WriteLine($"Total Data File Size: {dataFiles.Sum(x => new FileInfo(x).Length)}");
        }

        foreach (var (dataFileIndex, info) in dataFileInfo.OrderBy(x => x.Key)) {
            var dataFilePath = dynamicContainer.GetDataFilePath(dataFileIndex);
            
            output.WriteLine($"Data File[{dataFileIndex}]:");
            output.WriteLine($"  Size: {new FileInfo(dataFilePath).Length}");
            output.WriteLine($"  Count Failed Header Read: {info.m_countFailedHeaderRead}");
            output.WriteLine($"  Count Okay: {info.m_countOkay}");
            output.WriteLine($"  Count Size Zero: {info.m_countSizeZero}");
            output.WriteLine($"  Count Size Wrong Other: {info.m_countSizeWrongOther}");

            var sizeZeroSamples = info.m_sizeZeroSamples
                .Take(Math.Min(info.m_sizeZeroSamples.Count, 100))
                .ToArray();
            var sizeWrongOtherSamples = info.m_sizeWrongOtherSamples
                .Take(Math.Min(info.m_sizeWrongOtherSamples.Count, 999))
                .ToArray();

            void LogSample(HeaderSample sample) {
                output.WriteLine($"    EKey: {sample.m_ekey.ToHexString().ToLowerInvariant()}");
                output.WriteLine($"    CKey: {sample.m_ckey.ToHexString().ToLowerInvariant()}");
                output.WriteLine($"    Overwatch GUID: 0x{sample.m_guid.GUID:X16} / {sample.m_guid}");
                output.WriteLine($"    Header: " +
                    $"{Convert.ToHexString(sample.m_data.Skip(0).Take(16).ToArray()).ToLowerInvariant()} " +
                    $"{Convert.ToHexString(sample.m_data.Skip(16).Take(4).ToArray()).ToLowerInvariant()} " +
                    $"{Convert.ToHexString(sample.m_data.Skip(16+4).Take(1).ToArray()).ToLowerInvariant()} " +
                    $"{Convert.ToHexString(sample.m_data.Skip(16+4+1).Take(1).ToArray()).ToLowerInvariant()} " +
                    $"{Convert.ToHexString(sample.m_data.Skip(16+4+1+1).Take(4).ToArray()).ToLowerInvariant()} " +
                    $"{Convert.ToHexString(sample.m_data.Skip(16+4+1+1+4).Take(4).ToArray()).ToLowerInvariant()}");
                output.WriteLine($"    FourCC: {sample.m_fourCC:X8} ({Encoding.ASCII.GetString(BitConverter.GetBytes(sample.m_fourCC))})");
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
        
        {
            output.WriteLine("---- AGENT ----");
            
            //output.WriteLine($"Has Global Agent Info: {globalAgentDatabase != null}");
            if (globalAgentDatabase != null) {
                var targetProducts = new[] { "agent", "bna", "pro" /*,"thisdoesntexisttest"*/ };
                foreach (var product in targetProducts) {
                    var productInfo = AnonymizeAgentInfo(globalAgentDatabase.Data.ProductInstall.FirstOrDefault(x => x.ProductCode == product));
                    
                    var baseProductState = productInfo?.CachedProductState?.BaseProductState;
                    var versionStr = baseProductState?.CurrentVersionStr ?? baseProductState?.CurrentVersion; // never seen second but sanity
                    output.WriteLine($"Global Agent Info[{product}].Version: {versionStr}");
                }

                foreach (var product in targetProducts) {
                    var productInfo = AnonymizeAgentInfo(globalAgentDatabase.Data.ProductInstall.FirstOrDefault(x => x.ProductCode == product));
                    output.WriteLine($"Global Agent Info[{product}]: {productInfo}");
                }
            }
            
            var agentInfo = AnonymizeAgentInfo(client.AgentProduct);
            output.WriteLine($"Game Agent Info: {agentInfo}");
            output.WriteLine();
        }

        Console.Out.WriteLine($"Debugging information written to {filename} in the toolchain-release directory. Please upload this to Discord in the #corrupt-installs channel");
    }

    private class DataFileInfo {
        public int m_countFailedHeaderRead;
        public int m_countOkay;
        public int m_countSizeZero;
        public int m_countSizeWrongOther;

        public readonly List<HeaderSample> m_allBadSamples = new List<HeaderSample>();
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
        public teResourceGUID m_guid;
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