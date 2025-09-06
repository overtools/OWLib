#nullable enable
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels;

public class TeamDefinition {
    public teResourceGUID Id { get; set; }
    public string? FullName { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Abbreviation { get; set; }
    public Enum_5A789F71 Division { get; set; }
    public teResourceGUID Logo { get; set; }
    public teResourceGUID LogoAlt { get; set; }
    internal STU_73AE9738 STU { get; set; }

    public TeamDefinition(STU_73AE9738? def, ulong key = default) {
        Init(def, key);
    }

    private void Init(STU_73AE9738? def, ulong key = default) {
        if (def == null) return;

        STU = def;
        Id = (teResourceGUID) key;
        Name = GetString(def.m_137210AF);
        Location = GetString(def.m_4BA3B3CE);
        Abbreviation = GetString(def.m_0945E50A);
        Logo = def.m_AC77C84A;
        LogoAlt = def.m_DA688288;
        Division = def.m_AA53A680;
        FullName = $"{Location} {(string.Equals(Location, Name) ? "" : Name)}".Trim();
    }

    public static TeamDefinition? Load(ulong key) {
        var stu = GetInstance<STU_73AE9738>(key);
        if (stu == null) return null;
        return new TeamDefinition(stu, key);
    }
}