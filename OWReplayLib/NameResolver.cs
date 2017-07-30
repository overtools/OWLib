using System.Collections.Generic;
using CASCExplorer;
using OverTool;
using OWLib;
using OWLib.Types.STUD;

namespace OWReplayLib {
    public static class NameResolver {
        public enum ResolveType : ushort {
            COSMETIC        = 0xA5,
            SKIN            = COSMETIC,
            VOICE_LINE      = COSMETIC,
            SPRAY           = COSMETIC,
            HEROIC_INTRO    = COSMETIC,
            EMOTE           = COSMETIC,
            MAP             = 0x02,
            HERO            = 0x75,
            WEAPON_SKIN     = 0xAD
        }

        public static string GetName(Dictionary<ulong, Record> map, CASCHandler handler, uint id, ResolveType type) {
            return GetName(map, handler, id | (((ulong)type) << 32));
        }

        public static string GetName(Dictionary<ulong, Record> map, CASCHandler handler, ulong id) {
            if (handler == null) {
                return null;
            }

            ResolveType type = (ResolveType) GUID.Type(id);

            if (type == ResolveType.COSMETIC) {
                return ListInventory.GetInventoryName(id, map, handler)?.Item1;
            }

            if (type == ResolveType.WEAPON_SKIN) {
                return null; // TODO: A lot of overhead to get weaponskin name.
            }

            if (type == ResolveType.HERO) {
                STUD hero = new STUD(OverTool.Util.OpenFile(map[id], handler));
                HeroMaster master = hero?.Instances[0] as HeroMaster;
                if (master == null) {
                    return null;
                }
                return OverTool.Util.GetString(master.Header.name.key, map, handler);
            }

            if (type == ResolveType.MAP) {
                return null; // TODO: document 002 file types
            }

            return null;
        }
    }
}
