using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPlayerChoiceApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterEnums(state);
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetCurrentPlayerChoiceInfo",
                     "GetNumRerolls",
                     "GetRemainingTime",
                     "IsWaitingForPlayerChoiceResponse",
                     "OnUIClosed",
                     "RequestRerollPlayerChoice",
                     "SendPlayerChoiceResponse"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_PlayerChoice");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var playerChoice = LuaBindings.GetRuntime(state).PlayerChoice;
        switch (operation)
        {
            case "GetCurrentPlayerChoiceInfo":
                if (playerChoice.CurrentChoice is not { } info ||
                    info.ChoiceId <= 0 ||
                    info.Options.Count == 0)
                {
                    return 0;
                }
                PushPlayerChoiceInfo(state, info);
                return 1;
            case "GetNumRerolls":
                lua_pushinteger(state, playerChoice.NumRerolls);
                return 1;
            case "GetRemainingTime":
                PushOptionalInteger(state, playerChoice.RemainingTime);
                return 1;
            case "IsWaitingForPlayerChoiceResponse":
                lua_pushboolean(
                    state,
                    playerChoice.WaitingForResponse ? 1 : 0);
                return 1;
            case "OnUIClosed":
                playerChoice.UiClosedRequestCount++;
                playerChoice.WasWaitingOnLastUiClose =
                    playerChoice.WaitingForResponse;
                return 0;
            case "RequestRerollPlayerChoice":
                if (playerChoice.NumRerolls != 0)
                    playerChoice.RerollRequestCount++;
                return 0;
            case "SendPlayerChoiceResponse":
            {
                const string usage =
                    "Usage: C_PlayerChoice.SendPlayerChoiceResponse(responseID)";
                var responseId = RequiredInt32(state, 1, usage);
                if (responseId <= 0 ||
                    playerChoice.CurrentChoice is not { } current ||
                    !current.Options
                        .SelectMany(option => option.Buttons)
                        .Any(button => button.Id == responseId))
                {
                    return 0;
                }

                playerChoice.WaitingForResponse = false;
                playerChoice.LastResponseId = responseId;
                playerChoice.ResponseRequestCount++;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 4);
        SetInteger(state, "Common", 0);
        SetInteger(state, "Uncommon", 1);
        SetInteger(state, "Rare", 2);
        SetInteger(state, "Epic", 3);
        lua_setfield(state, -2, "PlayerChoiceRarity");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 4);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 3);
        lua_setfield(state, -2, "PlayerChoiceRarityMeta");
        lua_pop(state, 1);
    }

    private static void PushPlayerChoiceInfo(
        lua_State state,
        WowPlayerChoiceInfo info)
    {
        lua_createtable(state, 0, 13);
        SetString(state, "objectGUID", info.ObjectGuid);
        SetInteger(state, "choiceID", info.ChoiceId);
        SetString(state, "questionText", info.QuestionText);
        SetString(state, "pendingChoiceText", info.PendingChoiceText);
        SetOptionalString(state, "uiTextureKit", info.UiTextureKit);
        SetBoolean(state, "hideWarboardHeader", info.HideWarboardHeader);
        SetBoolean(state, "keepOpenAfterChoice", info.KeepOpenAfterChoice);
        SetBoolean(state, "showChoicesAsList", info.ShowChoicesAsList);
        SetBoolean(state, "requiresSelection", info.RequiresSelection);
        SetBoolean(state, "showChoicesAsGrid", info.ShowChoicesAsGrid);
        PushOptions(state, info.Options);
        lua_setfield(state, -2, "options");
        SetOptionalInteger(state, "soundKitID", info.SoundKitId);
        SetOptionalInteger(
            state,
            "closeUISoundKitID",
            info.CloseUiSoundKitId);
    }

    private static void PushOptions(
        lua_State state,
        IList<WowPlayerChoiceOptionInfo> options)
    {
        lua_createtable(state, options.Count, 0);
        for (var index = 0; index < options.Count; index++)
        {
            PushOption(state, options[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOption(
        lua_State state,
        WowPlayerChoiceOptionInfo option)
    {
        lua_createtable(state, 0, 18);
        SetInteger(state, "id", option.Id);
        SetString(state, "description", option.Description);
        SetString(state, "header", option.Header);
        SetInteger(state, "choiceArtID", option.ChoiceArtId);
        SetBoolean(state, "desaturatedArt", option.DesaturatedArt);
        SetBoolean(state, "disabledOption", option.DisabledOption);
        SetBoolean(state, "hasRewards", option.HasRewards);
        PushRewardInfo(state, option.RewardInfo);
        lua_setfield(state, -2, "rewardInfo");
        SetOptionalString(state, "uiTextureKit", option.UiTextureKit);
        SetInteger(state, "maxStacks", option.MaxStacks);
        PushButtons(state, option.Buttons);
        lua_setfield(state, -2, "buttons");
        SetOptionalInteger(state, "widgetSetID", option.WidgetSetId);
        SetOptionalInteger(state, "spellID", option.SpellId);
        SetOptionalInteger(state, "rarity", option.Rarity);
        SetOptionalInteger(state, "typeArtID", option.TypeArtId);
        SetOptionalString(
            state,
            "headerIconAtlasElement",
            option.HeaderIconAtlasElement);
        SetOptionalString(state, "subHeader", option.SubHeader);
        SetBoolean(state, "consolidateWidgets", option.ConsolidateWidgets);
    }

    private static void PushRewardInfo(
        lua_State state,
        WowPlayerChoiceRewardInfo rewardInfo)
    {
        lua_createtable(state, 0, 3);

        lua_createtable(state, rewardInfo.CurrencyRewards.Count, 0);
        for (var index = 0; index < rewardInfo.CurrencyRewards.Count; index++)
        {
            var reward = rewardInfo.CurrencyRewards[index];
            lua_createtable(state, 0, 5);
            SetInteger(state, "currencyId", reward.CurrencyId);
            SetString(state, "name", reward.Name);
            SetInteger(state, "currencyTexture", reward.CurrencyTexture);
            SetInteger(state, "quantity", reward.Quantity);
            SetBoolean(
                state,
                "isCurrencyContainer",
                reward.IsCurrencyContainer);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "currencyRewards");

        lua_createtable(state, rewardInfo.ItemRewards.Count, 0);
        for (var index = 0; index < rewardInfo.ItemRewards.Count; index++)
        {
            var reward = rewardInfo.ItemRewards[index];
            lua_createtable(state, 0, 3);
            SetInteger(state, "itemId", reward.ItemId);
            SetString(state, "name", reward.Name);
            SetInteger(state, "quantity", reward.Quantity);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "itemRewards");

        lua_createtable(state, rewardInfo.ReputationRewards.Count, 0);
        for (var index = 0;
             index < rewardInfo.ReputationRewards.Count;
             index++)
        {
            var reward = rewardInfo.ReputationRewards[index];
            lua_createtable(state, 0, 2);
            SetInteger(state, "factionId", reward.FactionId);
            SetInteger(state, "quantity", reward.Quantity);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "repRewards");
    }

    private static void PushButtons(
        lua_State state,
        IList<WowPlayerChoiceButtonInfo> buttons)
    {
        lua_createtable(state, buttons.Count, 0);
        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];
            lua_createtable(state, 0, 11);
            SetInteger(state, "id", button.Id);
            SetString(state, "text", button.Text);
            SetBoolean(state, "disabled", button.Disabled);
            SetBoolean(state, "showCheckmark", button.ShowCheckmark);
            SetBoolean(
                state,
                "hideButtonShowText",
                button.HideButtonShowText);
            SetBoolean(state, "selected", button.Selected);
            SetOptionalString(state, "confirmation", button.Confirmation);
            SetOptionalString(state, "tooltip", button.Tooltip);
            SetOptionalInteger(
                state,
                "rewardQuestID",
                button.RewardQuestId);
            SetOptionalInteger(state, "soundKitID", button.SoundKitId);
            SetOptionalString(state, "listText", button.ListText);
            lua_rawseti(state, -2, index + 1);
        }
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

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(
        lua_State state,
        string field,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }
}
