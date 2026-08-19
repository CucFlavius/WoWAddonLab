using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowIslandsQueueApi : LuaApiModule
{
    private const int IslandsQueueInteractionType = 43;
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "CloseIslandsQueueScreen", "GetIslandDifficultyInfo",
        "GetIslandsMaxGroupSize", "GetIslandsWeeklyQuestID",
        "QueueForIsland", "RequestPreloadRewardData"
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
        lua_setglobal(state, "C_IslandsQueue");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var islandsQueue = runtime.IslandsQueue;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CloseIslandsQueueScreen":
                islandsQueue.CloseScreenRequests++;
                ClearInteraction(runtime.PlayerInteractions);
                return 0;
            case "GetIslandDifficultyInfo":
                PushDifficultyInfo(state, islandsQueue.Difficulties);
                return 1;
            case "GetIslandsMaxGroupSize":
                lua_pushnumber(state, 3);
                return 1;
            case "GetIslandsWeeklyQuestID":
            {
                var flags = islandsQueue.WeeklyQuestEligibilityFlags;
                if ((flags & 5) != 0)
                    lua_pushnumber(state, 53435);
                else if ((flags & 2) != 0)
                    lua_pushnumber(state, 53436);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "QueueForIsland":
            {
                var difficultyId = RequiredInt32(
                    state,
                    "Usage: C_IslandsQueue.QueueForIsland(difficultyID)");
                islandsQueue.QueueRequests++;
                islandsQueue.LastRequestedDifficultyId = difficultyId;

                var interactions = runtime.PlayerInteractions;
                if (interactions.HasActiveInteraction &&
                    interactions.CurrentInteractionType ==
                    IslandsQueueInteractionType)
                {
                    islandsQueue.SuccessfulQueueRequests++;
                    islandsQueue.LastQueuedDifficultyId = difficultyId;
                    ClearInteraction(interactions);
                }
                return 0;
            }
            case "RequestPreloadRewardData":
            {
                var questId = RequiredInt32(
                    state,
                    "Usage: C_IslandsQueue." +
                    "RequestPreloadRewardData(questId)");
                islandsQueue.PreloadRewardDataRequests++;
                islandsQueue.LastPreloadRewardQuestId = questId;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static void PushDifficultyInfo(
        lua_State state,
        IList<WowIslandDifficultyInfoState> difficulties)
    {
        lua_createtable(state, difficulties.Count, 0);
        for (var index = 0; index < difficulties.Count; index++)
        {
            var difficulty = difficulties[index];
            lua_createtable(state, 0, 2);
            lua_pushnumber(state, difficulty.DifficultyId);
            lua_setfield(state, -2, "difficultyId");
            lua_pushnumber(state, difficulty.PreviewRewardQuestId);
            lua_setfield(state, -2, "previewRewardQuestId");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int RequiredInt32(lua_State state, string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, 1);
        if (double.IsNaN(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((int)value);
    }

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType =
            IslandsQueueInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType !=
            IslandsQueueInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
    }
}
