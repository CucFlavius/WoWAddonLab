using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowLegacyGlobalConstants
{
    private static readonly IReadOnlyDictionary<string, double> Values =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["LE_PARTY_CATEGORY_HOME"] = 1,
            ["LE_PARTY_CATEGORY_INSTANCE"] = 2,
            ["NUM_LE_PARTY_CATEGORYS"] = 2,

            ["LE_LFG_CATEGORY_LFD"] = 1,
            ["LE_LFG_CATEGORY_LFR"] = 2,
            ["LE_LFG_CATEGORY_RF"] = 3,
            ["LE_LFG_CATEGORY_SCENARIO"] = 4,
            ["LE_LFG_CATEGORY_FLEXRAID"] = 5,
            ["LE_LFG_CATEGORY_WORLDPVP"] = 6,
            ["LE_LFG_CATEGORY_BATTLEFIELD"] = 7,

            ["LE_UNIT_STAT_STRENGTH"] = 1,
            ["LE_UNIT_STAT_AGILITY"] = 2,
            ["LE_UNIT_STAT_STAMINA"] = 3,
            ["LE_UNIT_STAT_INTELLECT"] = 4,
            ["NUM_LE_UNIT_STATS"] = 4,

            ["LE_PET_JOURNAL_FILTER_COLLECTED"] = 1,
            ["LE_PET_JOURNAL_FILTER_NOT_COLLECTED"] = 2,

            ["LE_FRAME_TUTORIAL_WORLD_MAP_THREAT_ICON"] = 75,
            ["LE_FRAME_TUTORIAL_LINK_TRANSMOG_CUSTOM_SET"] = 110,
        };

    public static void Apply(lua_State state)
    {
        foreach (var (name, value) in Values)
        {
            lua_pushnumber(state, value);
            lua_setglobal(state, name);
        }
    }
}
