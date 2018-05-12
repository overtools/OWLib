using DataTool.JSON;
using Newtonsoft.Json;
using STULib.Types;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class Unlock {
	    public string Name;
	    public string Rarity;
	    public string Type;
	    public string Description;
	    public string AvailableIn;
	    [JsonConverter(typeof(GUIDConverter))]
	    public ulong GUID;
	    
	    [JsonIgnore]
	    public STUUnlock STU;

	    public Unlock(string name, string rarity, string type, string description, string availableIn, STUUnlock stu, ulong guid) {
	        Name = name.TrimEnd(' '); // ffs blizz, why do the names end in a space sometimes
	        Rarity = rarity;
	        Type = type;
	        Description = description;
	        AvailableIn = availableIn;
	        STU = stu;
	        GUID = guid;
	    }
    }
}
