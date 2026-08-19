using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGroupApi : LuaApiModule
{
    private static readonly string[] LocalizedClassNames =
    [
        "Warrior",
        "Paladin",
        "Hunter",
        "Rogue",
        "Priest",
        "Death Knight",
        "Shaman",
        "Mage",
        "Warlock",
        "Monk",
        "Druid",
        "Demon Hunter",
        "Evoker"
    ];

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetNumGroupMembers",
        "GetNumSubgroupMembers",
        "GetGroupMemberCounts",
        "GetRaidRosterInfo",
        "IsEveryoneAssistant",
        "IsInGroup",
        "IsInRaid"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "IsInRaid":
                lua_pushboolean(
                    state,
                    runtime.Group.Resolve(OptionalPartyCategory(state))?.IsInRaid == true ? 1 : 0);
                return 1;
            case "IsInGroup":
                lua_pushboolean(state, runtime.Group.Resolve(OptionalPartyCategory(state)) is not null ? 1 : 0);
                return 1;
            case "GetNumSubgroupMembers":
                lua_pushinteger(
                    state,
                    runtime.Group.Resolve(OptionalPartyCategory(state))?.SubgroupMemberCount ?? 0);
                return 1;
            case "GetNumGroupMembers":
                lua_pushinteger(
                    state,
                    runtime.Group.Resolve(OptionalPartyCategory(state))?.GroupMemberCount ?? 0);
                return 1;
            case "GetGroupMemberCounts":
            {
                var group = runtime.Group.Resolve(OptionalPartyCategory(state));
                lua_createtable(state, 0, 4 + LocalizedClassNames.Length);
                PushIntegerField(state, "TANK", group?.TankCount ?? 0);
                PushIntegerField(state, "HEALER", group?.HealerCount ?? 0);
                PushIntegerField(state, "DAMAGER", group?.DamagerCount ?? 0);
                PushIntegerField(state, "NOROLE", group?.NoRoleCount ?? 0);
                foreach (var className in LocalizedClassNames)
                    PushIntegerField(
                        state,
                        className,
                        group?.ClassCounts.GetValueOrDefault(className) ?? 0);
                if (group is not null)
                {
                    foreach (var (className, count) in group.ClassCounts)
                    {
                        if (!LocalizedClassNames.Contains(className, StringComparer.OrdinalIgnoreCase))
                            PushIntegerField(state, className, count);
                    }
                }
                return 1;
            }
            case "GetRaidRosterInfo":
            {
                if (lua_isnumber(state, 1) == 0)
                    return luaL_error(state, "Usage: GetRaidRosterInfo(index)");

                var index = (int)lua_tonumber(state, 1);
                var category = runtime.Group.ResolveCategory();
                var group = runtime.Group.Resolve();
                var member = group is null
                    ? null
                    : FindRaidMember(runtime, category, group, index);
                if (member is null)
                    return PushMissingRaidRosterInfo(state);

                lua_pushstring(state, member.Name);
                lua_pushinteger(state, member.RaidRank);
                lua_pushinteger(state, member.RaidSubgroup);
                lua_pushinteger(state, member.Level);
                lua_pushstring(state, member.ClassName);
                lua_pushstring(state, member.ClassFile);
                lua_pushstring(state, member.IsConnected ? member.Zone : "Offline");
                lua_pushboolean(state, member.IsConnected ? 1 : 0);
                lua_pushboolean(state, member.IsDead || member.IsGhost ? 1 : 0);
                PushOptionalString(state, member.RaidRole);
                lua_pushboolean(state, member.IsMasterLooter ? 1 : 0);
                lua_pushstring(state, member.GroupRole);
                return 12;
            }
            case "IsEveryoneAssistant":
                lua_pushboolean(state, runtime.Group.Resolve()?.EveryoneIsAssistant == true ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static int? OptionalPartyCategory(lua_State state)
    {
        if (lua_isnumber(state, 1) == 0)
            return null;

        var category = (int)lua_tonumber(state, 1);
        return category is (int)WowPartyCategory.Home or (int)WowPartyCategory.Instance
            ? category
            : null;
    }

    private static WowUnitState? FindRaidMember(
        LuaRuntime runtime,
        WowPartyCategory? category,
        WowGroupCategoryState group,
        int index)
    {
        if (index is < 1 or > 40)
            return null;

        var categoryId = category is null ? 0 : (int)category.Value;
        var indexedMember = runtime.Units.All.Values.FirstOrDefault(
            unit =>
                categoryId != 0 &&
                unit.RaidIndexByPartyCategory.TryGetValue(categoryId, out var categoryIndex) &&
                categoryIndex == index);
        if (indexedMember is not null)
            return indexedMember;

        indexedMember = runtime.Units.All.Values.FirstOrDefault(unit => unit.RaidIndex == index);
        if (indexedMember is not null)
            return indexedMember;

        if (index > group.GroupMemberCount)
            return null;

        return index == 1
            ? runtime.Units.Player
            : runtime.Units.Find($"party{index - 1}");
    }

    private static int PushMissingRaidRosterInfo(lua_State state)
    {
        lua_pushnil(state);
        lua_pushinteger(state, 0);
        lua_pushinteger(state, 1);
        lua_pushinteger(state, 1);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushnil(state);
        lua_pushboolean(state, 0);
        lua_pushstring(state, "NONE");
        return 12;
    }

    private static void PushIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
