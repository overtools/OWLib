using System.Runtime.Serialization;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using Utf8Json;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class MapHeader {
        [DataMember]
        public string Name;
        
        [DataMember]
        public string VariantName;
        
        [DataMember]
        public teResourceGUID MapGUID;
        
        [DataMember]
        public teResourceGUID[] GameModes;

        public MapHeader(STUMapHeader mapHeader) {
            Name = GetString(mapHeader.m_displayName);
            VariantName = GetString(mapHeader.m_1C706502);
            MapGUID = mapHeader.m_map;

            GameModes = Helper.JSON.FixArray(mapHeader.m_D608E9F3);
        }

        public string GetName() {
            return VariantName ?? Name ?? "Title Screen";
        }

        public string GetUniqueName() {
            string name = GetName();
            return $"{name}:{teResourceGUID.Index(MapGUID):X}";
        }
    }
}
