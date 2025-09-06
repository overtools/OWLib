using DataTool.Helper;
using TankLib.STU;
using TankLib.STU.Types;

namespace DataTool.DataModels;

public class ResourceKey {
    public string KeyID { get; set; }
    public string Value { get; set; }

    public ResourceKey(STUResourceKey resourceKey) {
        KeyID = resourceKey.GetKeyIDString();
        Value = resourceKey.GetKeyValueString();
    }

    public static ResourceKey? Load(ulong key) {
        var stu = STUHelper.GetInstance<STUResourceKey>(key);
        if (stu == null) return null;
        return new ResourceKey(stu);
    }
}