using System;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [DataContract]
    public class MapHeader {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Description;
        
        [DataMember]
        public string Description2;
        
        [DataMember]
        public string VariantName;
        
        [DataMember]
        public teResourceGUID MapGUID;
        
        [DataMember]
        public teResourceGUID[] GameModes;

        [DataMember]
        public Enum_668FA6B6 State;

        [DataMember]
        public Enum_A0F51DCC MapType;
        
        public MapHeader(ulong key) {
            STUMapHeader stu = GetInstance<STUMapHeader>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public MapHeader(STUMapHeader stu) {
            Init(stu);
        }

        public void Init(STUMapHeader mapHeader, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetString(mapHeader.m_displayName);
            VariantName = GetString(mapHeader.m_1C706502);
            Description = GetString(mapHeader.m_389CB894);
            Description2 = GetString(mapHeader.m_ACB95597);
            MapGUID = mapHeader.m_map;
            State = mapHeader.m_A125818B;
            MapType = mapHeader.m_mapType;

            GameModes = Helper.JSON.FixArray(mapHeader.m_D608E9F3);
        }

        public MapHeaderLite ToLite() {
            return new MapHeaderLite(this);
        } 

        public string GetName() {
            return VariantName ?? Name ?? "Title Screen";
        }

        public string GetUniqueName() {
            string name = GetName();
            return $"{name}:{teResourceGUID.Index(MapGUID):X}";
        }
    }

    // Lighter version of the MapHeader, just used to make JSON exports less thicc if you only need basic map info
    public class MapHeaderLite {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public string Name;
        
        [DataMember]
        public teResourceGUID MapGUID;

        public MapHeaderLite(MapHeader mapHeader) {
            GUID = mapHeader.GUID;
            Name = mapHeader.Name;
            MapGUID = mapHeader.MapGUID;

        }
    }
}
