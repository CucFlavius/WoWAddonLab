using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestInteractionApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetQuestID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "CloseQuest", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        if (lua_tostring(state, lua_upvalueindex(1)) == "GetQuestID")
        {
            lua_pushinteger(state, runtime.QuestInteraction.CurrentQuestId);
            return 1;
        }

        var interaction = runtime.QuestInteraction;
        interaction.CloseRequestCount++;
        interaction.ClosedQuestIds.Add(interaction.CurrentQuestId);
        if (runtime.PlayerInteractions.HasActiveInteraction &&
            runtime.PlayerInteractions.CurrentInteractionType == 4)
        {
            runtime.PlayerInteractions.HasActiveInteraction = false;
            runtime.PlayerInteractions.HasPendingInteraction = false;
            runtime.PlayerInteractions.CurrentInteractionType = 0;
            runtime.PlayerInteractions.PendingInteractionType = 0;
            runtime.PlayerInteractions.ValidNpcInteractionTypes.Clear();
        }
        runtime.TriggerEvent("QUEST_FINISHED");
        return 0;
    }
}
