using System.IO;
using System.IO.Compression;

namespace OWLib {
  public class APM {
    private APMHeader header;
    private APMPackage[] packages;
    private APMEntry[] entries;
    private PackageIndex[] indices;
    private PackageIndexRecord[][] records;
    private uint[][] dependencies;

    public APMHeader Header => header;
    public APMPackage[] Packages => packages;
    public APMEntry[] Entries => entries;
    public PackageIndex[] Indices => indices;
    public PackageIndexRecord[][] Records => records;

    public static ulong keyToTypeID(ulong key) {
      var num = (key >> 48);
      num = (((num >> 1) & 0x55555555) | ((num & 0x55555555) << 1));
      num = (((num >> 2) & 0x33333333) | ((num & 0x33333333) << 2));
      num = (((num >> 4) & 0x0F0F0F0F) | ((num & 0x0F0F0F0F) << 4));
      num = (((num >> 8) & 0x00FF00FF) | ((num & 0x00FF00FF) << 8));
      num = ((num >> 16) | (num << 16));
      num >>= 20;
      return num + 1;
    }

    public static ulong keyToIndexID(ulong key) {
      return key & 0xFFFFFFFFFFFF;
    }

    public APM(Stream apmStream, LookupContentByKeyDelegate lookupContentByKey) {
      using(BinaryReader reader = new BinaryReader(apmStream)) {
        header = reader.Read<APMHeader>();

        entries = new APMEntry[header.entryCount];
        for(int i = 0; i < header.entryCount; ++i) {
          entries[i] = reader.Read<APMEntry>();
        }

        packages = new APMPackage[header.packageCount];
        indices = new PackageIndex[header.packageCount];
        records = new PackageIndexRecord[header.packageCount][];
        dependencies = new uint[header.packageCount][];

        for(int i = 0; i < header.packageCount; ++i) {
          packages[i] = reader.Read<APMPackage>();

          using(Stream indexStream = lookupContentByKey(packages[i].packageKey))
          using(BinaryReader indexReader = new BinaryReader(indexStream)) {
            indices[i] = indexReader.Read<PackageIndex>();
            indexStream.Position = indices[i].recordsOffset;

            using(GZipStream recordStream = new GZipStream(indexStream, CompressionMode.Decompress, true))
            using(BinaryReader recordReader = new BinaryReader(recordStream)) {
              PackageIndexRecord[] recs = new PackageIndexRecord[indices[i].numRecords];
              for(int j = 0; j < indices[i].numRecords; ++j) {
                recs[j] = recordReader.Read<PackageIndexRecord>();
              }
              records[i] = recs;
            }

            indexStream.Position = indices[i].depsOffset;
            uint[] deps = new uint[indices[i].numDeps];
            for(int j = 0; j < indices[i].numDeps; ++j) {
              deps[j] = indexReader.ReadUInt32();
            }
            dependencies[i] = deps;
          }
        }
      }
    }
  }
}
