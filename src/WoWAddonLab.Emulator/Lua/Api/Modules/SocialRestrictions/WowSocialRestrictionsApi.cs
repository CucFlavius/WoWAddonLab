using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSocialRestrictionsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "AcknowledgeRegionalChatDisabled", "CanReceiveChat", "CanSendChat",
                     "IsChatDisabled", "IsMuted", "IsSilenced", "IsSquelched",
                     "SetChatDisabled"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SocialRestrictions");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var restrictions = runtime.SocialRestrictions;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanReceiveChat":
            case "CanSendChat":
                lua_pushboolean(state, !restrictions.ChatDisabled && !restrictions.IsMuted ? 1 : 0);
                return 1;
            case "IsChatDisabled":
                lua_pushboolean(state, restrictions.ChatDisabled ? 1 : 0);
                return 1;
            case "IsMuted":
                lua_pushboolean(state, restrictions.IsMuted ? 1 : 0);
                return 1;
            case "IsSilenced":
                lua_pushboolean(state, restrictions.IsSilenced ? 1 : 0);
                return 1;
            case "IsSquelched":
                lua_pushboolean(state, restrictions.IsSquelched ? 1 : 0);
                return 1;
            case "SetChatDisabled":
            {
                const string usage =
                    "Usage: C_SocialRestrictions.SetChatDisabled(disabled)";
                if (lua_type(state, 1) != LUA_TBOOLEAN)
                    return luaL_error(state, usage);

                var disabled = lua_toboolean(state, 1) != 0;
                restrictions.ChatDisabled = disabled;
                restrictions.PendingChatDisabledRequest = disabled;
                restrictions.ChatDisabledRequests.Add(disabled);
                return 0;
            }
            case "AcknowledgeRegionalChatDisabled":
                restrictions.RegionalChatDisabledAcknowledged = true;
                return 0;
            default:
                return 0;
        }
    }
}
