using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSpellDiminishApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly WowSpellDiminishCategory[] PvpCategories =
    [
        WowSpellDiminishCategory.Root,
        WowSpellDiminishCategory.Stun,
        WowSpellDiminishCategory.Incapacitate,
        WowSpellDiminishCategory.Disorient
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetAllSpellDiminishCategories",
                     "GetSpellDiminishCategoryInfo",
                     "IsSystemSupported",
                     "ShouldTrackSpellDiminishCategory"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SpellDiminish");
    }

    private static int Dispatch(lua_State state)
    {
        var diminish = LuaBindings.GetRuntime(state).SpellDiminish;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAllSpellDiminishCategories":
            {
                var ruleset = OptionalRuleset(
                    state,
                    1,
                    "Usage: local categories = " +
                    "C_SpellDiminish.GetAllSpellDiminishCategories([ruleset])");
                var categories = Enum.GetValues<WowSpellDiminishCategory>()
                    .Where(category =>
                        ruleset != WowSpellDiminishRuleset.PlayerVersusPlayer ||
                        Array.IndexOf(PvpCategories, category) >= 0)
                    .Select(category =>
                        diminish.Categories.TryGetValue(category, out var info)
                            ? info
                            : null)
                    .Where(info => info is not null)
                    .Cast<WowSpellDiminishCategoryInfo>()
                    .ToArray();

                lua_createtable(state, categories.Length, 0);
                for (var index = 0; index < categories.Length; index++)
                {
                    PushCategoryInfo(state, categories[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetSpellDiminishCategoryInfo":
            {
                var category = RequiredCategory(
                    state,
                    1,
                    "Usage: local categoryInfo = " +
                    "C_SpellDiminish.GetSpellDiminishCategoryInfo(category)");
                if (!diminish.Categories.TryGetValue(category, out var info))
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushCategoryInfo(state, info);
                return 1;
            }
            case "IsSystemSupported":
                lua_pushboolean(state, 1);
                return 1;
            case "ShouldTrackSpellDiminishCategory":
            {
                var usage = "Usage: local isTracked = " +
                            "C_SpellDiminish.ShouldTrackSpellDiminishCategory(" +
                            "category, ruleset)";
                var category = RequiredCategory(state, 1, usage);
                var ruleset = RequiredRuleset(state, 2, usage);
                var tracked =
                    ruleset != WowSpellDiminishRuleset.PlayerVersusPlayer ||
                    Array.IndexOf(PvpCategories, category) >= 0 &&
                    (!diminish.PvpRuntimeFilterEnabled ||
                     diminish.PvpTrackedCategories.Contains(category));
                lua_pushboolean(state, tracked ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static WowSpellDiminishCategory RequiredCategory(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumericEnum(state, index, 7, usage);
        return (WowSpellDiminishCategory)value;
    }

    private static WowSpellDiminishRuleset RequiredRuleset(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumericEnum(state, index, 2, usage);
        return (WowSpellDiminishRuleset)value;
    }

    private static WowSpellDiminishRuleset? OptionalRuleset(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredRuleset(state, index, usage);
    }

    private static int RequiredNumericEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = (int)number;
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static void PushCategoryInfo(
        lua_State state,
        WowSpellDiminishCategoryInfo info)
    {
        lua_createtable(state, 0, 3);
        lua_pushinteger(state, (int)info.Category);
        lua_setfield(state, -2, "category");
        if (info.Name is { } name)
            lua_pushstring(state, name);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "name");
        if (info.Icon is { } icon)
            lua_pushinteger(state, icon);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "icon");
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 8);
        SetInteger(state, "Root", 0);
        SetInteger(state, "Taunt", 1);
        SetInteger(state, "Stun", 2);
        SetInteger(state, "AoEKnockback", 3);
        SetInteger(state, "Incapacitate", 4);
        SetInteger(state, "Disorient", 5);
        SetInteger(state, "Silence", 6);
        SetInteger(state, "Disarm", 7);
        lua_setfield(state, -2, "SpellDiminishCategory");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 8);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 7);
        lua_setfield(state, -2, "SpellDiminishCategoryMeta");

        lua_createtable(state, 0, 3);
        SetInteger(state, "None", 0);
        SetInteger(state, "PvE", 1);
        SetInteger(state, "PvP", 2);
        lua_setfield(state, -2, "SpellDiminishRuleset");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 2);
        lua_setfield(state, -2, "SpellDiminishRulesetMeta");
        lua_pop(state, 1);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }
}
