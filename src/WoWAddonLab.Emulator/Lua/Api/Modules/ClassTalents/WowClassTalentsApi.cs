using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClassTalentsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanChangeTalents", "CanCreateNewConfig", "CanEditTalents", "GetActiveConfigID",
        "GetActiveHeroTalentSpec",
        "GetConfigIDsBySpecID", "GetHasStarterBuild",
        "GetHeroTalentSpecsForClassSpec", "GetLastSelectedSavedConfigID",
        "GetStarterBuildActive", "UpdateLastSelectedSavedConfigID"
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
        lua_setglobal(state, "C_ClassTalents");
    }

    private static int Dispatch(lua_State state)
    {
        var talents = LuaBindings.GetRuntime(state).ClassTalents;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? "";
        switch (operation)
        {
            case "CanChangeTalents":
                lua_pushboolean(state, talents.CanChangeTalents ? 1 : 0);
                lua_pushboolean(state, talents.CanAddTalents ? 1 : 0);
                PushOptionalString(state, talents.ChangeError);
                return 3;
            case "CanCreateNewConfig":
                lua_pushboolean(state, talents.CanCreateNewConfig ? 1 : 0);
                return 1;
            case "CanEditTalents":
                lua_pushboolean(state, talents.CanEditTalents ? 1 : 0);
                PushOptionalString(state, talents.ChangeError);
                return 2;
            case "GetActiveConfigID":
                return PushOptionalInteger(state, talents.ActiveConfigId);
            case "GetActiveHeroTalentSpec":
                return PushOptionalInteger(state, talents.ActiveHeroTalentSpec);
            case "GetConfigIDsBySpecID":
            {
                const string usage =
                    "Usage: local configIDs = " +
                    "C_ClassTalents.GetConfigIDsBySpecID([specID])";
                if (!TryReadOptionalInt32(state, 1, out var specializationId))
                    return luaL_error(state, usage);
                var configs =
                    (specializationId ?? talents.CurrentSpecializationId) is { } specId
                        ? talents.ConfigIdsBySpecialization.GetValueOrDefault(specId, [])
                        : [];
                lua_createtable(state, configs.Count, 0);
                for (var index = 0; index < configs.Count; index++)
                {
                    lua_pushinteger(state, configs[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetHasStarterBuild":
                lua_pushboolean(state, talents.HasStarterBuild ? 1 : 0);
                return 1;
            case "GetHeroTalentSpecsForClassSpec":
            {
                const string usage =
                    "Usage: local subTreeIDs, requiredPlayerLevel = " +
                    "C_ClassTalents.GetHeroTalentSpecsForClassSpec(" +
                    "[configID, classSpecID])";
                if (!TryReadOptionalInt32(state, 1, out var configId) ||
                    !TryReadOptionalInt32(state, 2, out var classSpecId))
                {
                    return luaL_error(state, usage);
                }
                var effectiveConfigId = configId ?? talents.ActiveConfigId;
                var effectiveClassSpecId =
                    classSpecId ?? talents.CurrentSpecializationId;
                if (effectiveConfigId is not { } resolvedConfigId ||
                    effectiveClassSpecId is not { } resolvedClassSpecId ||
                    !talents.HeroTalentSpecsByConfigAndClassSpec.TryGetValue(
                        (resolvedConfigId, resolvedClassSpecId),
                        out var heroSpecs) ||
                    heroSpecs.SubTreeIds.Count == 0)
                {
                    return 0;
                }
                lua_createtable(state, heroSpecs.SubTreeIds.Count, 0);
                for (var index = 0; index < heroSpecs.SubTreeIds.Count; index++)
                {
                    lua_pushinteger(state, heroSpecs.SubTreeIds[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                if (heroSpecs.RequiredPlayerLevel is { } requiredPlayerLevel)
                    lua_pushinteger(state, requiredPlayerLevel);
                else
                    lua_pushnil(state);
                return 2;
            }
            case "GetLastSelectedSavedConfigID":
            {
                const string usage =
                    "Usage: local configID = " +
                    "C_ClassTalents.GetLastSelectedSavedConfigID(specID)";
                if (!TryReadOptionalInt32(state, 1, out var specializationId) ||
                    specializationId is null)
                {
                    return luaL_error(state, usage);
                }
                return PushOptionalInteger(
                    state,
                    talents.LastSelectedSavedConfigIdsBySpecialization
                        .GetValueOrDefault(specializationId.Value) is > 0 and var configId
                            ? configId
                            : null);
            }
            case "GetStarterBuildActive":
                lua_pushboolean(state, talents.StarterBuildActive ? 1 : 0);
                return 1;
            case "UpdateLastSelectedSavedConfigID":
            {
                const string usage =
                    "Usage: C_ClassTalents.UpdateLastSelectedSavedConfigID(" +
                    "specID [, configID])";
                if (!TryReadOptionalInt32(state, 1, out var specializationId) ||
                    specializationId is null ||
                    !TryReadOptionalInt32(state, 2, out var configId))
                {
                    return luaL_error(state, usage);
                }
                if (configId is > 0)
                {
                    talents.LastSelectedSavedConfigIdsBySpecialization[
                        specializationId.Value] = configId.Value;
                }
                else
                {
                    talents.LastSelectedSavedConfigIdsBySpecialization.Remove(
                        specializationId.Value);
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushOptionalInteger(lua_State state, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushinteger(state, value.Value);
        return 1;
    }

    private static bool TryReadOptionalInt32(
        lua_State state,
        int index,
        out int? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
