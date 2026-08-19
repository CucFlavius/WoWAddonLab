using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowScrappingMachineApi : LuaApiModule
{
    private const int PlayerInteractionType = 40;
    private const int ScrapSpellId = 265742;
    private const string Namespace = "C_ScrappingMachineUI";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CloseScrappingMachine",
        "DropPendingScrapItemFromCursor",
        "GetCurrentPendingScrapItemLocationByIndex",
        "GetScrapSpellID",
        "GetScrappingMachineName",
        "HasScrappableItems",
        "RemoveAllScrapItems",
        "RemoveCurrentScrappingItem",
        "RemoveItemToScrap",
        "ScrapItems",
        "ValidateScrappingList"
    ];

    public override void Register(lua_State state)
    {
        lua_createtable(state, 0, Functions.Length);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, Namespace);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var scrapping = runtime.ScrappingMachine;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CloseScrappingMachine":
                RemoveAll(runtime, scrapping);
                ClearInteraction(runtime.PlayerInteractions);
                return 0;
            case "DropPendingScrapItemFromCursor":
            {
                var index = RequiredInt32(
                    state,
                    1,
                    "Usage: C_ScrappingMachineUI." +
                    "DropPendingScrapItemFromCursor(index)");
                DropCursorItem(runtime, scrapping, index);
                return 0;
            }
            case "GetCurrentPendingScrapItemLocationByIndex":
            {
                var index = RequiredInt32(
                    state,
                    1,
                    "Usage: local itemLoc = C_ScrappingMachineUI." +
                    "GetCurrentPendingScrapItemLocationByIndex(index)");
                Validate(runtime, scrapping);
                if (!IsSlotIndex(index) ||
                    scrapping.PendingItems[index] is not { } location)
                {
                    return 0;
                }

                WowItemApi.PushItemLocation(state, location);
                return 1;
            }
            case "GetScrapSpellID":
                lua_pushnumber(state, ScrapSpellId);
                return 1;
            case "GetScrappingMachineName":
                if (!runtime.PlayerInteractions.HasActiveInteraction ||
                    runtime.PlayerInteractions.CurrentInteractionType !=
                    PlayerInteractionType ||
                    scrapping.MachineName is null)
                {
                    return 0;
                }
                lua_pushstring(state, scrapping.MachineName);
                return 1;
            case "HasScrappableItems":
                lua_pushboolean(
                    state,
                    scrapping.PendingItems.Any(item => item.HasValue) ? 1 : 0);
                return 1;
            case "RemoveAllScrapItems":
                RemoveAll(runtime, scrapping);
                return 0;
            case "RemoveCurrentScrappingItem":
                Validate(runtime, scrapping);
                RemoveAt(
                    runtime,
                    scrapping,
                    scrapping.CurrentScrappingIndex);
                return 0;
            case "RemoveItemToScrap":
            {
                var index = RequiredInt32(
                    state,
                    1,
                    "Usage: C_ScrappingMachineUI." +
                    "RemoveItemToScrap(index)");
                Validate(runtime, scrapping);
                RemoveAt(runtime, scrapping, index);
                return 0;
            }
            case "ScrapItems":
                Submit(runtime, scrapping);
                return 0;
            case "ValidateScrappingList":
                Validate(runtime, scrapping);
                return 0;
            default:
                return 0;
        }
    }

    private static void DropCursorItem(
        LuaRuntime runtime,
        WowScrappingMachineState state,
        int index)
    {
        if (!IsSlotIndex(index) ||
            state.IsScrapping ||
            runtime.Cursor.Payload?.Kind != WowCursorPayloadKind.Item)
        {
            return;
        }

        var location = state.CursorItemLocation ?? runtime.Cursor.GetItemLocation();
        if (location is not { } item ||
            !state.ScrappableItems.Contains(item) ||
            state.PendingItems.Any(pending => pending == item))
        {
            return;
        }

        state.PendingItems[index] = item;
        runtime.TriggerEvent("SCRAPPING_MACHINE_ITEM_ADDED", index);
        runtime.TriggerEvent("SCRAPPING_MACHINE_PENDING_ITEM_CHANGED");
        runtime.Cursor.ClearPayload();
        state.CursorItemLocation = null;
    }

    private static void RemoveAll(
        LuaRuntime runtime,
        WowScrappingMachineState state)
    {
        var removed = false;
        for (var index = 0; index < state.PendingItems.Length; index++)
        {
            if (!state.PendingItems[index].HasValue)
                continue;
            state.PendingItems[index] = null;
            removed = true;
        }

        if (removed)
            runtime.TriggerEvent("SCRAPPING_MACHINE_PENDING_ITEM_CHANGED");
    }

    private static void RemoveAt(
        LuaRuntime runtime,
        WowScrappingMachineState state,
        int index)
    {
        if (!IsSlotIndex(index) ||
            !state.PendingItems[index].HasValue)
        {
            return;
        }

        state.PendingItems[index] = null;
        runtime.TriggerEvent("SCRAPPING_MACHINE_ITEM_REMOVED", index);
        runtime.TriggerEvent("SCRAPPING_MACHINE_PENDING_ITEM_CHANGED");
    }

    private static void Validate(
        LuaRuntime runtime,
        WowScrappingMachineState state)
    {
        var changed = false;
        for (var index = 0; index < state.PendingItems.Length; index++)
        {
            if (state.PendingItems[index] is not { } item ||
                state.ScrappableItems.Contains(item))
            {
                continue;
            }

            state.PendingItems[index] = null;
            changed = true;
        }

        if (changed)
            runtime.TriggerEvent("SCRAPPING_MACHINE_PENDING_ITEM_CHANGED");
    }

    private static void Submit(
        LuaRuntime runtime,
        WowScrappingMachineState state)
    {
        Validate(runtime, state);
        var items = state.PendingItems
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();
        if (items.Length == 0)
            return;

        state.IsScrapping = true;
        if (state.ActiveSpellId == ScrapSpellId)
        {
            return;
        }

        state.CurrentScrappingIndex =
            Array.FindIndex(state.PendingItems, item => item.HasValue);
        if (!state.CanCastScrapSpell)
            return;

        state.ScrapRequests.Add(new WowScrappingRequest(items));
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static bool IsSlotIndex(int index) =>
        index is >= 0 and < 9;

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = PlayerInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != PlayerInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Remove(PlayerInteractionType);
    }
}
