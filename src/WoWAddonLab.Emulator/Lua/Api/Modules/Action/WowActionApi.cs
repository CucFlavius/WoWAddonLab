using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowActionApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] NamespaceFunctions =
    [
        "EnableActionRangeCheck",
        "FindAssistedCombatActionButtons",
        "FindFlyoutActionButtons",
        "FindPetActionButtons",
        "FindSpellActionButtons",
        "ForceUpdateAction",
        "GetActionAutocast",
        "GetActionBarPage",
        "GetActionChargeDuration",
        "GetActionCharges",
        "GetActionCooldown",
        "GetActionCooldownDuration",
        "GetActionDisplayCount",
        "GetActionLossOfControlCooldownDuration",
        "GetActionLossOfControlCooldownInfo",
        "GetActionText",
        "GetActionTexture",
        "GetActionUseCount",
        "GetBonusBarIndex",
        "GetBonusBarIndexForSlot",
        "GetBonusBarOffset",
        "GetExtraBarIndex",
        "GetItemActionOnEquipSpellID",
        "GetMultiCastBarIndex",
        "GetOverrideBarIndex",
        "GetOverrideBarSkin",
        "GetPetActionPetBarIndices",
        "GetProfessionQuality",
        "GetProfessionQualityInfo",
        "GetSpell",
        "GetTempShapeshiftBarIndex",
        "GetVehicleBarIndex",
        "HasAction",
        "HasAssistedCombatActionButtons",
        "HasBonusActionBar",
        "HasExtraActionBar",
        "HasFlyoutActionButtons",
        "HasOverrideActionBar",
        "HasPetActionButtons",
        "HasPetActionPetBarIndices",
        "HasRangeRequirements",
        "HasSpellActionButtons",
        "HasTempShapeshiftActionBar",
        "HasVehicleActionBar",
        "IsActionInRange",
        "IsAssistedCombatAction",
        "IsAttackAction",
        "IsAutoCastPetAction",
        "IsAutoRepeatAction",
        "IsConsumableAction",
        "IsCurrentAction",
        "IsEnabledAutoCastPetAction",
        "IsEquippedAction",
        "IsEquippedGearOutfitAction",
        "IsHarmfulAction",
        "IsHelpfulAction",
        "IsInterruptAction",
        "IsItemAction",
        "IsOnBarOrSpecialBar",
        "IsPossessBarVisible",
        "IsStackableAction",
        "IsUsableAction",
        "PutActionInSlot",
        "RegisterActionUIButton",
        "SetActionBarPage",
        "ShouldOverrideBarShowHealthBar",
        "ShouldOverrideBarShowManaBar",
        "ToggleAutoCastPetAction",
        "UnregisterActionUIButton",
        "UsesActionText"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetActionInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetActionBarToggles", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SetActionBarToggles", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetPetActionInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetPetActionCooldown", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetPossessInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "PetHasActionBar", Callback);

        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }

        lua_setglobal(state, "C_ActionBar");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        if (operation == "GetActionInfo")
            return GetActionInfo(state, runtime);
        if (operation == "GetActionBarToggles")
        {
            for (var index = 0; index < 7; index++)
                lua_pushboolean(
                    state,
                    (runtime.Actions.ActionBarToggleMask & (1 << index)) != 0 ? 1 : 0);
            return 7;
        }
        if (operation == "SetActionBarToggles")
        {
            byte mask = 0;
            for (var index = 0; index < 7; index++)
            {
                if (lua_toboolean(state, index + 1) != 0)
                    mask |= (byte)(1 << index);
            }
            runtime.Actions.ActionBarToggleMask = mask;
            return 0;
        }
        if (operation == "PetHasActionBar")
            return PushBoolean(state, runtime.Actions.HasPetActionBar);
        if (operation == "GetPossessInfo")
        {
            var indexValue = lua_tonumber(state, 1);
            if (indexValue == 0)
                return luaL_error(state, "Usage: GetPossessInfo(index)");
            var index = (int)indexValue;
            if (index is < 1 or > 2)
                return 0;
            if (!runtime.Actions.PossessActions.TryGetValue(index, out var possess))
                return 0;
            PushLuaScalar(state, possess.Texture);
            PushOptionalInteger(state, possess.SpellId);
            lua_pushboolean(state, possess.Enabled ? 1 : 0);
            return 3;
        }
        if (operation == "GetPetActionInfo")
        {
            var index = RequiredNumericIndex(state, "Usage: GetPetActionInfo(index)");
            if (index is < 1 or > 10)
                return 0;
            if (!runtime.Actions.PetActions.TryGetValue(index, out var pet))
                return 0;
            lua_pushstring(state, pet.Name);
            PushLuaScalar(state, pet.Texture);
            if (pet.IsToken)
                lua_pushnumber(state, 1);
            else
                lua_pushnil(state);
            lua_pushboolean(state, pet.IsActive ? 1 : 0);
            lua_pushboolean(state, pet.AutoCastAllowed ? 1 : 0);
            lua_pushboolean(state, pet.AutoCastEnabled ? 1 : 0);
            PushOptionalInteger(state, pet.SpellId);
            lua_pushboolean(state, pet.ChecksRange ? 1 : 0);
            lua_pushboolean(state, pet.InRange ? 1 : 0);
            return 9;
        }
        if (operation == "GetPetActionCooldown")
        {
            var index = RequiredNumericIndex(state, "Usage: GetPetActionCooldown(index)");
            var cooldown = index is >= 1 and <= 10 &&
                           runtime.Actions.PetActions.TryGetValue(index, out var pet)
                ? pet.Cooldown
                : new WowActionCooldownInfo();
            lua_pushnumber(state, cooldown.StartTime);
            lua_pushnumber(state, cooldown.Duration);
            lua_pushnumber(state, cooldown.IsEnabled ? 1 : 0);
            return 3;
        }

        switch (operation)
        {
            case "GetActionBarPage":
                lua_pushinteger(state, runtime.Actions.ActionBarPage + 1);
                return 1;
            case "SetActionBarPage":
            {
                var page = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: C_ActionBar.SetActionBarPage(pageIndex)");
                if (page is < 1 or > 6)
                    return luaL_error(state, "invalid action bar page (must be between 1 and 6)");
                runtime.Actions.ActionBarPage = page - 1;
                return 0;
            }
            case "GetExtraBarIndex":
                lua_pushinteger(state, 18);
                return 1;
            case "GetMultiCastBarIndex":
                lua_pushinteger(state, 11);
                return 1;
            case "GetVehicleBarIndex":
                lua_pushinteger(state, 15);
                return 1;
            case "GetTempShapeshiftBarIndex":
                lua_pushinteger(state, 16);
                return 1;
            case "GetOverrideBarIndex":
                lua_pushinteger(state, 17);
                return 1;
            case "GetBonusBarIndex":
                lua_pushinteger(state, runtime.Actions.BonusBarIndex);
                return 1;
            case "GetBonusBarOffset":
                lua_pushinteger(
                    state,
                    runtime.Actions.BonusBarIndex >= 0
                        ? runtime.Actions.BonusBarIndex - 5
                        : 0);
                return 1;
            case "GetOverrideBarSkin":
                PushOptionalInteger(state, runtime.Actions.OverrideBarSkin);
                return 1;
            case "HasAssistedCombatActionButtons":
                return PushBoolean(state, runtime.Actions.Slots.Values.Any(action => action.IsAssistedCombat));
            case "HasBonusActionBar":
                return PushBoolean(state, runtime.Actions.BonusBarIndex >= 0);
            case "HasExtraActionBar":
                return PushBoolean(state, runtime.Actions.HasExtraActionBar);
            case "HasOverrideActionBar":
                return PushBoolean(state, runtime.Actions.HasOverrideActionBar);
            case "HasTempShapeshiftActionBar":
                return PushBoolean(state, runtime.Actions.HasTempShapeshiftActionBar);
            case "HasVehicleActionBar":
                return PushBoolean(state, runtime.Actions.HasVehicleActionBar);
            case "IsPossessBarVisible":
                return PushBoolean(state, runtime.Actions.IsPossessBarVisible);
            case "ShouldOverrideBarShowHealthBar":
                return PushBoolean(state, runtime.Actions.ShouldOverrideBarShowHealthBar);
            case "ShouldOverrideBarShowManaBar":
                return PushBoolean(state, runtime.Actions.ShouldOverrideBarShowManaBar);
            case "IsOnBarOrSpecialBar":
            {
                var spellId = RequiredInteger(state, 1, "Usage: C_ActionBar.IsOnBarOrSpecialBar(spellID)");
                return PushBoolean(
                    state,
                    runtime.Actions.Slots.Values.Any(
                        action => action.Type.Equals("spell", StringComparison.OrdinalIgnoreCase) &&
                                  action.Id == spellId));
            }
            case "FindSpellActionButtons":
            case "FindFlyoutActionButtons":
            case "FindPetActionButtons":
            {
                var id = RequiredInteger(state, 1, $"Usage: local slots = C_ActionBar.{operation}(actionID)");
                var type = operation switch
                {
                    "FindSpellActionButtons" => "spell",
                    "FindFlyoutActionButtons" => "flyout",
                    _ => "pet"
                };
                return PushSlotList(
                    state,
                    runtime.Actions.Slots
                        .Where(pair => pair.Value.Type.Equals(type, StringComparison.OrdinalIgnoreCase) &&
                                       pair.Value.Id == id)
                        .Select(pair => pair.Key));
            }
            case "FindAssistedCombatActionButtons":
                return PushSlotList(
                    state,
                    runtime.Actions.Slots
                        .Where(pair => pair.Value.IsAssistedCombat)
                        .Select(pair => pair.Key));
            case "HasFlyoutActionButtons":
            case "HasPetActionButtons":
            case "HasSpellActionButtons":
            {
                var id = RequiredInteger(state, 1, $"Usage: C_ActionBar.{operation}(actionID)");
                var type = operation switch
                {
                    "HasFlyoutActionButtons" => "flyout",
                    "HasPetActionButtons" => "pet",
                    _ => "spell"
                };
                return PushBoolean(
                    state,
                    runtime.Actions.Slots.Values.Any(
                        action => action.Type.Equals(type, StringComparison.OrdinalIgnoreCase) &&
                                  action.Id == id));
            }
            case "GetPetActionPetBarIndices":
            {
                var petActionId = RequiredInteger(
                    state,
                    1,
                    "Usage: local slots = C_ActionBar.GetPetActionPetBarIndices(petActionID)");
                return runtime.Actions.PetActionPetBarIndices.TryGetValue(petActionId, out var indices)
                    ? PushSlotList(state, indices)
                    : 0;
            }
            case "HasPetActionPetBarIndices":
            {
                var petActionId = RequiredInteger(
                    state,
                    1,
                    "Usage: local hasPetActionPetBarIndices = C_ActionBar.HasPetActionPetBarIndices(petActionID)");
                return PushBoolean(
                    state,
                    runtime.Actions.PetActionPetBarIndices.TryGetValue(petActionId, out var indices) &&
                    indices.Count > 0);
            }
            case "EnableActionRangeCheck":
            {
                var actionId = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: C_ActionBar.EnableActionRangeCheck(actionID, enable)");
                var enabled = lua_toboolean(state, 2) != 0;
                if (enabled)
                    runtime.Actions.RangeCheckedSlots.Add(actionId);
                else
                    runtime.Actions.RangeCheckedSlots.Remove(actionId);
                return 0;
            }
            case "RegisterActionUIButton":
            {
                var buttonId = GetObjectId(state, 1);
                var actionId = RequiredOneBasedIndex(
                    state,
                    2,
                    "Usage: C_ActionBar.RegisterActionUIButton(button, actionID, cooldown)");
                var cooldownId = GetObjectId(state, 3);
                if (buttonId is { } id && IsValidActionId(actionId))
                    runtime.Actions.UiRegistrations[id] = new WowActionUiRegistration(id, actionId, cooldownId);
                return 0;
            }
            case "UnregisterActionUIButton":
            {
                var buttonId = GetObjectId(state, 1);
                if (buttonId is { } id)
                    runtime.Actions.UiRegistrations.Remove(id);
                return 0;
            }
            case "ForceUpdateAction":
            case "PutActionInSlot":
            case "ToggleAutoCastPetAction":
                RequiredOneBasedIndex(state, 1, $"Usage: C_ActionBar.{operation}(actionID)");
                return 0;
        }

        var slotId = RequiredOneBasedIndex(state, 1, $"Usage: C_ActionBar.{operation}(actionID)");
        if (!IsValidActionId(slotId) && ReturnsNoValuesForInvalidAction(operation))
            return 0;
        runtime.Actions.Slots.TryGetValue(slotId, out var slot);

        switch (operation)
        {
            case "HasAction":
                return PushBoolean(state, slot is not null);
            case "GetActionTexture":
                lua_pushinteger(state, slot?.TextureId ?? 0);
                return 1;
            case "GetActionText":
                if (slot?.Text is null)
                    return 0;
                lua_pushstring(state, slot.Text);
                return 1;
            case "GetActionDisplayCount":
                PushActionDisplayCount(state, slot);
                return 1;
            case "GetActionUseCount":
                lua_pushinteger(state, slot?.UseCount ?? 0);
                return 1;
            case "GetActionAutocast":
                lua_pushboolean(state, slot?.IsAutoCastAllowed == true ? 1 : 0);
                lua_pushboolean(state, slot?.IsAutoCastEnabled == true ? 1 : 0);
                return 2;
            case "UsesActionText":
                return PushBoolean(
                    state,
                    slot?.Type.Equals("macro", StringComparison.OrdinalIgnoreCase) == true ||
                    slot?.Type.Equals("equipmentset", StringComparison.OrdinalIgnoreCase) == true);
            case "GetSpell":
                lua_pushinteger(
                    state,
                    slot?.Type.Equals("spell", StringComparison.OrdinalIgnoreCase) == true ? slot.Id : 0);
                return 1;
            case "GetBonusBarIndexForSlot":
                if (slotId is < 73 or > 132)
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_pushinteger(state, (slotId - 1) / 12);
                return 1;
            case "GetItemActionOnEquipSpellID":
                PushOptionalInteger(state, slot?.OnEquipSpellId);
                return 1;
            case "GetProfessionQuality":
                PushOptionalInteger(state, slot?.ProfessionQuality);
                return 1;
            case "GetProfessionQualityInfo":
                lua_pushnil(state);
                return 1;
            case "GetActionCooldown":
                PushCooldownInfo(state, slot?.Cooldown ?? new WowActionCooldownInfo());
                return 1;
            case "GetActionCharges":
                PushChargeInfo(state, slot?.Charges ?? new WowActionChargeInfo());
                return 1;
            case "GetActionLossOfControlCooldownInfo":
                PushLossOfControlInfo(state, slot?.LossOfControl ?? new WowActionLossOfControlInfo());
                return 1;
            case "GetActionChargeDuration":
                PushDuration(state, slot?.ChargeDuration);
                return 1;
            case "GetActionCooldownDuration":
                PushDuration(state, slot?.CooldownDuration);
                return 1;
            case "GetActionLossOfControlCooldownDuration":
                PushDuration(state, slot?.LossOfControlCooldownDuration);
                return 1;
            case "IsUsableAction":
                lua_pushboolean(state, slot?.IsUsable == true ? 1 : 0);
                lua_pushboolean(state, slot?.IsLackingResources == true ? 1 : 0);
                return 2;
            case "IsAttackAction":
                return PushBoolean(state, slot?.IsAttack == true);
            case "IsAutoRepeatAction":
                return PushBoolean(state, slot?.IsAutoRepeat == true);
            case "IsCurrentAction":
                return PushBoolean(state, slot?.IsCurrent == true);
            case "IsEquippedAction":
                return PushBoolean(state, slot?.IsEquipped == true);
            case "IsHarmfulAction":
                return PushBoolean(state, slot?.IsHarmful == true);
            case "IsHelpfulAction":
                return PushBoolean(state, slot?.IsHelpful == true);
            case "IsItemAction":
                return PushBoolean(state, slot?.Type.Equals("item", StringComparison.OrdinalIgnoreCase) == true);
            case "IsActionInRange":
                if (slot?.IsInRange is not { } isInRange)
                {
                    lua_pushnil(state);
                    return 1;
                }
                return PushBoolean(state, isInRange);
            case "HasRangeRequirements":
                return PushBoolean(state, slot?.HasRangeRequirements == true);
            case "IsAssistedCombatAction":
                return PushBoolean(state, slot?.IsAssistedCombat == true);
            case "IsAutoCastPetAction":
                return PushBoolean(state, slot?.IsAutoCastPetAction == true);
            case "IsEnabledAutoCastPetAction":
                return PushBoolean(state, slot?.IsAutoCastEnabled == true);
            case "IsConsumableAction":
                return PushBoolean(state, slot?.IsConsumable == true);
            case "IsEquippedGearOutfitAction":
                return PushBoolean(state, slot?.IsEquippedGearOutfit == true);
            case "IsInterruptAction":
                return PushBoolean(state, slot?.IsInterrupt == true);
            case "IsStackableAction":
                return PushBoolean(state, slot?.IsStackable == true);
            default:
                return 0;
        }
    }

    private static int GetActionInfo(lua_State state, LuaRuntime runtime)
    {
        var slot = RequiredNumericIndex(state, "Usage: GetActionInfo(slot)");
        if (!runtime.Actions.Slots.TryGetValue(slot, out var action))
            return 0;

        lua_pushstring(state, action.Type);
        PushLuaScalar(state, action.ActionInfoIdentifier ?? action.Id);

        var returnsSubtype =
            action.SubType is not null ||
            action.Type.Equals("spell", StringComparison.OrdinalIgnoreCase) ||
            action.Type.Equals("macro", StringComparison.OrdinalIgnoreCase) ||
            action.Type.Equals("companion", StringComparison.OrdinalIgnoreCase);
        if (!returnsSubtype)
            return 2;

        PushOptionalString(state, action.SubType);
        return 3;
    }

    private static int RequiredNumericIndex(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            luaL_error(state, usage);
        return (int)lua_tonumber(state, 1);
    }

    private static int RequiredOneBasedIndex(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
            luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (value < 0 || value > uint.MaxValue)
            luaL_error(state, usage);
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static bool IsValidActionId(int actionId) =>
        actionId is >= 1 and <= WowActionState.MaximumActionCount;

    private static bool ReturnsNoValuesForInvalidAction(string operation) =>
        operation is
            "GetActionAutocast" or
            "GetActionChargeDuration" or
            "GetActionCharges" or
            "GetActionCooldown" or
            "GetActionCooldownDuration" or
            "GetActionDisplayCount" or
            "GetActionLossOfControlCooldownDuration" or
            "GetActionLossOfControlCooldownInfo" or
            "GetActionText" or
            "GetActionTexture" or
            "GetActionUseCount" or
            "HasAction" or
            "HasRangeRequirements" or
            "IsActionInRange" or
            "IsAttackAction" or
            "IsAutoRepeatAction" or
            "IsConsumableAction" or
            "IsCurrentAction" or
            "IsEquippedAction" or
            "IsItemAction" or
            "IsStackableAction" or
            "IsUsableAction" or
            "UsesActionText";

    private static int RequiredInteger(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
            luaL_error(state, usage);
        return (int)lua_tonumber(state, index);
    }

    private static int? GetObjectId(lua_State state, int index)
    {
        if (lua_type(state, index) != LUA_TTABLE)
            return null;
        lua_getfield(state, index, "__id");
        var id = lua_type(state, -1) == LUA_TNUMBER ? (int)lua_tonumber(state, -1) : (int?)null;
        lua_pop(state, 1);
        return id;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushLuaScalar(lua_State state, object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case int integer:
                lua_pushinteger(state, integer);
                break;
            case long integer:
                lua_pushinteger(state, integer);
                break;
            case double number:
                lua_pushnumber(state, number);
                break;
            case float number:
                lua_pushnumber(state, number);
                break;
            case bool boolean:
                lua_pushboolean(state, boolean ? 1 : 0);
                break;
            default:
                lua_pushstring(state, value.ToString() ?? string.Empty);
                break;
        }
    }

    private static void PushActionDisplayCount(lua_State state, WowActionSlot? slot)
    {
        const string usage =
            "Usage: local displayCount = C_ActionBar.GetActionDisplayCount(" +
            "actionID [, maxDisplayCount, replacementString])";

        var maximumDisplayCount = 9999;
        if (lua_gettop(state) >= 2 && lua_isnil(state, 2) == 0)
        {
            if (lua_type(state, 2) != LUA_TNUMBER)
                luaL_error(state, usage);

            var value = lua_tonumber(state, 2);
            if (value < int.MinValue || value > int.MaxValue)
                luaL_error(state, usage);
            maximumDisplayCount = (int)value;
        }

        var replacement = "*";
        if (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0)
        {
            var type = lua_type(state, 3);
            if (type is not (LUA_TSTRING or LUA_TNUMBER))
                luaL_error(state, usage);
            replacement = lua_tostring(state, 3) ?? string.Empty;
        }

        if (slot is null)
        {
            lua_pushstring(state, string.Empty);
            return;
        }

        if (slot.IsConsumable || slot.IsStackable || slot.UseCount > 0)
        {
            lua_pushstring(
                state,
                slot.UseCount <= maximumDisplayCount
                    ? slot.UseCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : replacement);
            return;
        }

        lua_pushstring(
            state,
            slot.Charges.CurrentCharges > 1
                ? slot.Charges.CurrentCharges.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty);
    }

    private static int PushSlotList(lua_State state, IEnumerable<int> slots)
    {
        var orderedSlots = slots.Order().ToArray();
        if (orderedSlots.Length == 0)
            return 0;

        lua_newtable(state);
        var index = 1;
        foreach (var slot in orderedSlots)
        {
            lua_pushinteger(state, slot);
            lua_rawseti(state, -2, index++);
        }
        return 1;
    }

    private static void PushDuration(lua_State state, WowDurationState? duration)
        => WowDurationApi.Push(state, duration);

    private static void PushCooldownInfo(lua_State state, WowActionCooldownInfo info)
    {
        lua_newtable(state);
        SetNumber(state, "startTime", info.StartTime);
        SetNumber(state, "duration", info.Duration);
        SetBoolean(state, "isEnabled", info.IsEnabled);
        SetBoolean(state, "isActive", info.IsActive);
        SetNumber(state, "modRate", info.ModRate);
        SetOptionalInteger(state, "activeCategory", info.ActiveCategory);
        SetOptionalNumber(
            state,
            "timeUntilEndOfStartRecovery",
            info.TimeUntilEndOfStartRecovery);
        SetOptionalBoolean(state, "isOnGCD", info.IsOnGlobalCooldown);
    }

    private static void PushChargeInfo(lua_State state, WowActionChargeInfo info)
    {
        lua_newtable(state);
        SetInteger(state, "currentCharges", info.CurrentCharges);
        SetInteger(state, "maxCharges", info.MaxCharges);
        SetNumber(state, "cooldownStartTime", info.CooldownStartTime);
        SetNumber(state, "cooldownDuration", info.CooldownDuration);
        SetNumber(state, "chargeModRate", info.ChargeModRate);
        SetBoolean(state, "isActive", info.IsActive);
    }

    private static void PushLossOfControlInfo(lua_State state, WowActionLossOfControlInfo info)
    {
        lua_newtable(state);
        SetNumber(state, "startTime", info.StartTime);
        SetNumber(state, "duration", info.Duration);
        SetNumber(state, "modRate", info.ModRate);
        SetBoolean(state, "isActive", info.IsActive);
        SetBoolean(state, "shouldReplaceNormalCooldown", info.ShouldReplaceNormalCooldown);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetInteger(lua_State state, string key, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalInteger(lua_State state, string key, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalNumber(lua_State state, string key, double? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalBoolean(lua_State state, string key, bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, key);
    }
}
