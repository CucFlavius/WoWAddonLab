using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPetInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetPetTalentTree", "GetPetTamersForMap", "GetSpellForPetAction",
        "IsPetActionPassive", "PetAbandon", "PetAssistMode", "PetRename"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_PetInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var pets = LuaBindings.GetRuntime(state).PetInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetPetTalentTree":
                if (pets.TalentTreeName is null)
                    return 0;
                lua_pushstring(state, pets.TalentTreeName);
                return 1;
            case "GetPetTamersForMap":
                RequiredNumber(state, 1, operation);
                lua_newtable(state);
                return 1;
            case "GetSpellForPetAction":
            {
                var actionId = RequiredNumber(state, 1, operation);
                if (pets.SpellIdsByActionId.TryGetValue(actionId, out var spellId))
                    lua_pushnumber(state, spellId);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "IsPetActionPassive":
                lua_pushboolean(
                    state,
                    pets.PassiveActionIds.Contains(RequiredNumber(state, 1, operation)) ? 1 : 0);
                return 1;
            case "PetAbandon":
                pets.LastAbandonedPetNumber = OptionalNumber(state, 1);
                return 0;
            case "PetAssistMode":
                pets.AssistMode = true;
                return 0;
            case "PetRename":
                if (lua_isstring(state, 1) == 0)
                    return UsageError(state, operation);
                pets.LastRename = lua_tostring(state, 1);
                pets.LastRenamedPetNumber = OptionalNumber(state, 2);
                return 0;
            default:
                return 0;
        }
    }

    private static int RequiredNumber(lua_State state, int index, string operation)
    {
        if (lua_isnumber(state, index) == 0)
        {
            UsageError(state, operation);
            return 0;
        }
        return (int)lua_tonumber(state, index);
    }

    private static int? OptionalNumber(lua_State state, int index) =>
        lua_gettop(state) >= index && lua_isnil(state, index) == 0
            ? (int)lua_tonumber(state, index)
            : null;

    private static int UsageError(lua_State state, string operation) =>
        luaL_error(state, $"Usage: C_PetInfo.{operation}(...)");
}
