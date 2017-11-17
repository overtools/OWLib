using System.Collections.Generic;
using System.IO;
using STULib.Types.Map;

namespace STULib.Impl.Version2HashComparer {
    public class MapComparer : Version1Comparer {
        public MapComparer(Stream stuStream, uint owVersion) : base(stuStream, owVersion) { }

        protected override void ReadInstanceData(long offset) {
            Stream.Position = offset;
            InternalInstances = new Dictionary<uint, InstanceData>();
            
            Map map = new Map(Stream, BuildVersion);
            int index = 0;
            InstanceData = new InstanceData[map.STUInstances.Count];
            foreach (uint instance in map.STUInstances) {
                InstanceData[index] = GetInstanceData(instance);
                index++;
            }
        }
    }
}