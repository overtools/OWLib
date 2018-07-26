using DataTool.JSON;
using Newtonsoft.Json;
using STULib.Types;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class ItemInfo {
	    public string Name;
	    public string Rarity;
	    public string Type;
	    public string Description;
	    public string AvailableIn;
	    
	    public STUUnlock Unlock;

	    [JsonConverter(typeof(GUIDConverter))]
	    public ulong GUID;

	    public ItemInfo(string name, string rarity, string type, string description, string availableIn, STUUnlock unlock, ulong guid) {
	        Name = name.TrimEnd(' '); // ffs blizz, why do the names end in a space sometimes
	        Rarity = rarity;
	        Type = type;
	        Description = description;
	        AvailableIn = availableIn;
	        Unlock = unlock;
	        GUID = guid;
	    }
    }
}
