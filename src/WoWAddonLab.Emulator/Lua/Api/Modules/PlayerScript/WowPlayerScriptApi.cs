using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPlayerScriptApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "GetAutoDeclineGuildInvites", "GetAutoDeclineNeighborhoodInvites",
                     "GetPlayerFacing", "GetPVPLifetimeStats", "GetReleaseTimeRemaining", "GetSheathState",
                     "IsCharacterNewlyBoosted",
                     "IsLoggedIn", "IsMounted", "IsOutdoors", "SetAutoDeclineGuildInvites",
                     "SetAutoDeclineNeighborhoodInvites", "ResurrectGetOfferer",
                     "ResurrectHasSickness", "ResurrectHasTimer", "RequestTimePlayed"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var player = runtime.PlayerScript;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAutoDeclineGuildInvites":
                lua_pushboolean(state, player.AutoDeclineGuildInvites ? 1 : 0);
                return 1;
            case "GetAutoDeclineNeighborhoodInvites":
                lua_pushboolean(state, player.AutoDeclineNeighborhoodInvites ? 1 : 0);
                return 1;
            case "GetPVPLifetimeStats":
                lua_pushnumber(state, player.LifetimeHonorableKills);
                lua_pushinteger(
                    state,
                    player.LifetimeMaxPvpRank >= 5
                        ? player.LifetimeMaxPvpRank
                        : 0);
                return 2;
            case "GetPlayerFacing":
                if (runtime.Units.Find("player")?.Position is { } position)
                    lua_pushnumber(state, position.Facing);
                else
                    lua_pushnil(state);
                return 1;
            case "GetReleaseTimeRemaining":
            {
                int remainingMilliseconds;
                if (player.ReleaseTimerSuppressed)
                {
                    remainingMilliseconds = -1000;
                }
                else if (player.ReleaseDeadlineTickMilliseconds == 0)
                {
                    remainingMilliseconds = 0;
                }
                else
                {
                    remainingMilliseconds = unchecked(
                        (int)(player.ReleaseDeadlineTickMilliseconds - runtime.FrameTime.TickMilliseconds));
                    if (remainingMilliseconds <= 0)
                        remainingMilliseconds = 0;
                }

                lua_pushnumber(state, remainingMilliseconds / 1000);
                return 1;
            }
            case "GetSheathState":
                if (player.SheathState is { } sheathState)
                    lua_pushinteger(state, sheathState);
                else
                    lua_pushnil(state);
                return 1;
            case "SetAutoDeclineGuildInvites":
                player.AutoDeclineGuildInvites = lua_toboolean(state, 1) != 0;
                return 0;
            case "SetAutoDeclineNeighborhoodInvites":
                player.AutoDeclineNeighborhoodInvites = lua_toboolean(state, 1) != 0;
                return 0;
            case "IsCharacterNewlyBoosted":
                lua_pushboolean(state, player.IsCharacterNewlyBoosted ? 1 : 0);
                return 1;
            case "IsLoggedIn":
                lua_pushboolean(state, (player.CinematicStateFlags & 0x8) == 0 ? 1 : 0);
                return 1;
            case "IsMounted":
                lua_pushboolean(
                    state,
                    runtime.Units.Find("player")?.IsMounted == true ? 1 : 0);
                return 1;
            case "IsOutdoors":
                lua_pushboolean(
                    state,
                    runtime.Units.Find("player")?.IsOutdoors == true ? 1 : 0);
                return 1;
            case "ResurrectGetOfferer":
                if (player.ResurrectOffererName is { } offererName)
                    lua_pushstring(state, offererName);
                else
                    lua_pushnil(state);
                return 1;
            case "ResurrectHasSickness":
                lua_pushboolean(state, player.ResurrectHasSickness ? 1 : 0);
                return 1;
            case "ResurrectHasTimer":
                lua_pushboolean(
                    state,
                    player.ResurrectHasTimer && !runtime.Pvp.IsActiveBattlefieldArena ? 1 : 0);
                return 1;
            case "RequestTimePlayed":
                player.TimePlayedRequestCount++;
                player.TimePlayedResponsePending = true;
                return 0;
            default:
                return 0;
        }
    }

    internal static void Tick(LuaRuntime runtime)
    {
        var player = runtime.PlayerScript;
        if (!player.TimePlayedResponsePending)
            return;
        player.TimePlayedResponsePending = false;
        runtime.TriggerEvent(
            "TIME_PLAYED_MSG",
            player.TotalTimePlayedSeconds,
            player.LevelTimePlayedSeconds);
    }
}
