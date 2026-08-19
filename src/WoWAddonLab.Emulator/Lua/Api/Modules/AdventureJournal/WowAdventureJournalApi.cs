using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAdventureJournalApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "ActivateEntry", "CanBeShown", "GetNumAvailableSuggestions",
                     "GetPrimaryOffset", "GetReward", "GetSuggestions",
                     "SetPrimaryOffset", "UpdateSuggestions"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AdventureJournal");
    }

    private static int Dispatch(lua_State state)
    {
        var journal = LuaBindings.GetRuntime(state).AdventureJournal;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ActivateEntry":
                return ActivateEntry(state, journal);
            case "CanBeShown":
                lua_pushboolean(state, journal.CanBeShown ? 1 : 0);
                return 1;
            case "GetNumAvailableSuggestions":
                lua_pushnumber(state, journal.AvailableSuggestionCount);
                return 1;
            case "GetPrimaryOffset":
                lua_pushinteger(state, journal.PrimaryOffset);
                return 1;
            case "GetReward":
                return GetReward(state, journal);
            case "GetSuggestions":
                return GetSuggestions(state, journal);
            case "SetPrimaryOffset":
                return SetPrimaryOffset(state, journal);
            case "UpdateSuggestions":
                journal.UpdateSuggestionsRequestCount++;
                journal.LastUpdateSuggestionsForce =
                    lua_toboolean(state, 1) != 0;
                return 0;
            default:
                return 0;
        }
    }

    private static int ActivateEntry(
        lua_State state,
        WowAdventureJournalState journal)
    {
        const string usage =
            "Usage: C_AdventureJournal.ActivateEntry( index )";
        var index = RequiredInt32(state, 1, usage);
        if (index is < 1 or > 3)
            return luaL_error(state, "Invalid adventure journal index.");

        journal.ActivatedEntryIndex = index;
        journal.ActivationCount++;
        return 0;
    }

    private static int GetReward(
        lua_State state,
        WowAdventureJournalState journal)
    {
        const string usage =
            "Usage: C_AdventureJournal.GetReward( index )";
        var index = RequiredInt32(state, 1, usage);
        if (index is < 1 or > 3)
            return luaL_error(state, "Invalid adventure journal index.");
        if (!journal.Rewards.TryGetValue(index, out var reward))
            return 0;

        lua_createtable(state, 0, 12);
        SetOptionalInteger(state, "itemLevel", reward.ItemLevel);
        SetOptionalInteger(
            state,
            "minItemLevel",
            reward.MinimumItemLevel);
        SetOptionalInteger(
            state,
            "maxItemLevel",
            reward.MaximumItemLevel);
        SetOptionalBoolean(
            state,
            "isRewardTable",
            reward.IsRewardTable);
        SetOptionalInteger(state, "itemID", reward.ItemId);
        SetOptionalInteger(
            state,
            "itemQuantity",
            reward.ItemQuantity);
        SetOptionalInteger(state, "itemIcon", reward.ItemIcon);
        SetOptionalString(state, "itemLink", reward.ItemLink);
        SetOptionalInteger(
            state,
            "currencyType",
            reward.CurrencyType);
        SetOptionalInteger(
            state,
            "currencyQuantity",
            reward.CurrencyQuantity);
        SetOptionalInteger(
            state,
            "currencyIcon",
            reward.CurrencyIcon);
        SetOptionalString(
            state,
            "rewardDesc",
            reward.RewardDescription);
        return 1;
    }

    private static int GetSuggestions(
        lua_State state,
        WowAdventureJournalState journal)
    {
        PushReusableTable(state);
        var slotCount = Math.Min(3, journal.Suggestions.Count);
        for (var index = 0; index < slotCount; index++)
        {
            var suggestion = journal.Suggestions[index];
            if (suggestion is null)
                continue;

            lua_createtable(state, 0, 9);
            SetOptionalString(state, "title", suggestion.Title);
            SetOptionalString(
                state,
                "description",
                suggestion.Description);
            SetOptionalString(
                state,
                "buttonText",
                suggestion.ButtonText);
            SetOptionalInteger(
                state,
                "ej_instanceID",
                suggestion.EncounterJournalInstanceId);
            SetOptionalBoolean(
                state,
                "hideDifficulty",
                suggestion.HideDifficulty);
            SetOptionalInteger(
                state,
                "difficultyID",
                suggestion.DifficultyId);
            SetOptionalInteger(
                state,
                "expansionLevel",
                suggestion.ExpansionLevel);
            SetOptionalBoolean(
                state,
                "isRandomDungeon",
                suggestion.IsRandomDungeon);
            SetOptionalInteger(
                state,
                "iconPath",
                suggestion.IconPath);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int SetPrimaryOffset(
        lua_State state,
        WowAdventureJournalState journal)
    {
        const string usage =
            "Usage: C_AdventureJournal.SetPrimaryOffset( offset )";
        var offset = RequiredInt32(state, 1, usage);
        if (offset < 0 ||
            (uint)offset >= journal.AvailableSuggestionCount)
        {
            return luaL_error(
                state,
                $"C_AdventureJournal.SetPrimaryOffset() invalid offset({offset})");
        }

        if (journal.PrimaryOffset != offset)
        {
            journal.PrimaryOffset = offset;
            journal.PrimaryOffsetChangeCount++;
        }
        return 0;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)value;
    }

    private static void PushReusableTable(lua_State state)
    {
        if (lua_istable(state, 1) != 0)
        {
            ClearTable(state, 1);
            lua_pushvalue(state, 1);
            return;
        }
        lua_newtable(state);
    }

    private static void ClearTable(lua_State state, int tableIndex)
    {
        lua_newtable(state);
        var keysIndex = lua_gettop(state);
        var keyCount = 0;

        lua_pushnil(state);
        while (lua_next(state, tableIndex) != 0)
        {
            lua_pop(state, 1);
            lua_pushvalue(state, -1);
            lua_rawseti(state, keysIndex, ++keyCount);
        }

        for (var index = 1; index <= keyCount; index++)
        {
            lua_rawgeti(state, keysIndex, index);
            lua_pushnil(state);
            lua_settable(state, tableIndex);
        }
        lua_pop(state, 1);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (!value.HasValue)
            return;
        lua_pushinteger(state, value.Value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (!value.HasValue)
            return;
        lua_pushboolean(state, value.Value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            return;
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
