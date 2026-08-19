using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowItemInteractionApi : LuaApiModule
{
    private const int PlayerInteractionType = 44;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ClearPendingItem",
        "CloseUI",
        "GetChargeInfo",
        "GetItemConversionCurrencyCost",
        "GetItemInteractionInfo",
        "GetItemInteractionSpellId",
        "InitializeFrame",
        "PerformItemInteraction",
        "Reset",
        "SetPendingItem"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ItemInteraction");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var interaction = runtime.ItemInteraction;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearPendingItem":
                SetPendingItem(runtime, null);
                return 0;
            case "CloseUI":
                ClearInteraction(runtime.PlayerInteractions);
                return 0;
            case "GetChargeInfo":
                PushChargeInfo(state, interaction.ChargeInfo);
                return 1;
            case "GetItemConversionCurrencyCost":
            {
                const string usage =
                    "Usage: local conversionCost = C_ItemInteraction." +
                    "GetItemConversionCurrencyCost(item)";
                var location = WowItemApi.RequiredItemLocation(state, usage);
                interaction.ConversionCosts.TryGetValue(
                    location,
                    out var cost);
                PushConversionCost(
                    state,
                    cost ?? new WowItemInteractionConversionCost(0, 0));
                return 1;
            }
            case "GetItemInteractionInfo":
                if (interaction.Info is null)
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushInteractionInfo(state, interaction.Info);
                return 1;
            case "GetItemInteractionSpellId":
                lua_pushnumber(state, interaction.InteractionSpellId);
                return 1;
            case "InitializeFrame":
                interaction.PendingItem = null;
                return 0;
            case "PerformItemInteraction":
                if (interaction.PendingItem is { } pendingItem &&
                    interaction.EligiblePendingItems.Contains(pendingItem))
                {
                    interaction.PerformRequests.Add(
                        new WowItemInteractionPerformRequest(
                            pendingItem,
                            interaction.SlotIndex));
                }
                return 0;
            case "Reset":
                interaction.InteractionRecordId = 0;
                interaction.SlotIndex = -1;
                interaction.PendingItem = null;
                return 0;
            case "SetPendingItem":
            {
                WowItemLocation? location = null;
                if (lua_gettop(state) >= 1 &&
                    lua_type(state, 1) != LUA_TNIL)
                {
                    const string usage =
                        "Usage: local success = " +
                        "C_ItemInteraction.SetPendingItem([item])";
                    location =
                        WowItemApi.RequiredItemLocation(state, usage);
                }
                lua_pushboolean(
                    state,
                    SetPendingItem(runtime, location) ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static bool SetPendingItem(
        LuaRuntime runtime,
        WowItemLocation? location)
    {
        var interaction = runtime.ItemInteraction;
        if (location is { } sameLocation &&
            interaction.PendingItem == sameLocation)
        {
            return true;
        }

        interaction.PendingItem = null;
        if (location is { } candidate &&
            interaction.EligiblePendingItems.Contains(candidate))
        {
            interaction.PendingItem = candidate;
            runtime.TriggerEvent(
                "ITEM_INTERACTION_ITEM_SELECTION_UPDATED",
                candidate);
            runtime.Cursor.ClearPayload();
            return true;
        }

        runtime.TriggerEvent(
            "ITEM_INTERACTION_ITEM_SELECTION_UPDATED",
            new object?[] { null });
        return false;
    }

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

    private static void PushChargeInfo(
        lua_State state,
        WowItemInteractionChargeInfo info)
    {
        lua_createtable(state, 0, 3);
        SetNumber(state, "newChargeAmount", info.NewChargeAmount);
        SetNumber(state, "rechargeRate", info.RechargeRate);
        SetNumber(state, "timeToNextCharge", info.TimeToNextCharge);
    }

    private static void PushConversionCost(
        lua_State state,
        WowItemInteractionConversionCost cost)
    {
        lua_createtable(state, 0, 2);
        SetNumber(state, "currencyID", cost.CurrencyId);
        SetNumber(state, "amount", cost.Amount);
    }

    private static void PushInteractionInfo(
        lua_State state,
        WowItemInteractionInfo info)
    {
        lua_createtable(state, 0, 15);
        SetString(state, "textureKit", info.TextureKit);
        SetNumber(state, "openSoundKitID", info.OpenSoundKitId);
        SetNumber(state, "closeSoundKitID", info.CloseSoundKitId);
        SetString(state, "titleText", info.TitleText);
        SetString(state, "tutorialText", info.TutorialText);
        SetString(state, "buttonText", info.ButtonText);
        SetNumber(state, "interactionType", info.InteractionType);
        SetNumber(state, "flags", info.Flags);
        SetOptionalString(state, "description", info.Description);
        SetOptionalString(state, "buttonTooltip", info.ButtonTooltip);
        SetOptionalString(
            state,
            "confirmationDescription",
            info.ConfirmationDescription);
        SetOptionalString(state, "slotTooltip", info.SlotTooltip);
        SetOptionalNumber(state, "cost", info.Cost);
        SetOptionalNumber(
            state,
            "currencyTypeId",
            info.CurrencyTypeId);
        SetOptionalNumber(
            state,
            "dropInSlotSoundKitId",
            info.DropInSlotSoundKitId);
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(
            state,
            "UIItemInteractionFlags",
            ("DisplayWithInset", 1),
            ("ConfirmationHasDelay", 2),
            ("ConversionMode", 4),
            ("ClickShowsFlyout", 8),
            ("AddCurrency", 16));
        SetEnumMeta(state, "UIItemInteractionFlagsMeta", 1, 16, 5);

        SetEnum(
            state,
            "UIItemInteractionType",
            ("None", 0),
            ("CastSpell", 1),
            ("CleanseCorruption", 2),
            ("RunecarverScrapping", 3),
            ("ItemConversion", 4));
        SetEnumMeta(state, "UIItemInteractionTypeMeta", 0, 4, 5);
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
            SetNumber(state, value.Name, value.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count) =>
        SetEnum(
            state,
            name,
            ("MinValue", minimum),
            ("MaxValue", maximum),
            ("NumValues", count));

    private static void SetString(
        lua_State state,
        string key,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalString(
        lua_State state,
        string key,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string key,
        int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
        lua_setfield(state, -2, key);
    }

    private static void SetNumber(
        lua_State state,
        string key,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }
}
