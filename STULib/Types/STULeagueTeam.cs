using STULib.Types.Enums;
using STULib.Types.Generic;

namespace STULib.Types {
    [STU(0x73AE9738)]
    public class STULeagueTeam : Common.STUInstance {
        [STUField(0x4BA3B3CE)]
        public Common.STUGUID Location;

        [STUField(0x0945E50A)]
        public Common.STUGUID Abbreviation;

        [STUField(0x137210AF)]
        public Common.STUGUID Name;

        [STUField(0xAC77C84A)]
        public Common.STUGUID Icon;

        [STUField(0xB8DC6D46)]
        public Common.STUGUID Colours;

        [STUField(0xAA53A680)]
        public STUEnumLeagueDivision Division;
    }
}
