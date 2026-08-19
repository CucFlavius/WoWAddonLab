using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSpecializationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "CanPlayerUsePVPTalentUI", "CanPlayerUseTalentSpecUI", "CanPlayerUseTalentUI",
        "GetActiveSpecGroup", "GetAllSelectedPvpTalentIDs", "GetClassIDFromSpecID",
        "GetInspectSelectedPvpTalent", "GetNumSpecializationsForClassID",
        "GetPvpTalentAlertStatus", "GetPvpTalentInfo", "GetPvpTalentSlotInfo",
        "GetPvpTalentSlotUnlockLevel", "GetPvpTalentUnlockLevel", "GetSpecIDs",
        "GetSpecialization", "GetSpecializationInfo", "GetSpecializationMasterySpells",
        "GetSpellsDisplay", "GetTalentInfo", "IsInitialized", "IsPvpTalentLocked",
        "MatchesCurrentSpecSet", "SetPetSpecialization", "SetPvpTalentLocked",
        "SetSpecialization"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetNumSpecializations", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetNumSpecGroups", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetLootSpecialization", Callback);
        LuaBindings.RegisterClosureGlobal(state, "HasLootSpecializations", Callback);
        LuaBindings.RegisterClosureGlobal(state, "SetLootSpecialization", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationInfoForClassID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationInfoForSpecID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationInfoByID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationRole", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationRoleEnum", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationRoleByID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetSpecializationRoleEnumByID", Callback);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SpecializationInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var client = runtime.Client;
        var specializations = runtime.Specializations;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetNumSpecializations":
                lua_pushinteger(
                    state,
                    lua_toboolean(state, 1) != 0
                        ? 0
                        : client.SpecializationCount);
                return 1;
            case "GetNumSpecGroups":
                lua_pushinteger(
                    state,
                    lua_toboolean(state, 1) != 0
                        ? 0
                        : specializations.SpecGroupCount);
                return 1;
            case "GetLootSpecialization":
                lua_pushinteger(state, client.LootSpecializationId);
                return 1;
            case "HasLootSpecializations":
                lua_pushboolean(state, specializations.HasLootSpecializations ? 1 : 0);
                return 1;
            case "SetLootSpecialization":
                if (!TryReadRequiredInt32(state, 1, out var lootSpecializationId))
                    return luaL_error(state, "Usage: SetLootSpecialization(specializationID)");
                client.LootSpecializationId = lootSpecializationId;
                return 0;
            case "CanPlayerUsePVPTalentUI":
                lua_pushboolean(state, specializations.CanUsePvpTalentUi ? 1 : 0);
                lua_pushstring(state, specializations.CanUsePvpTalentUiReason);
                return 2;
            case "CanPlayerUseTalentSpecUI":
                lua_pushboolean(state, specializations.CanUseTalentSpecUi ? 1 : 0);
                lua_pushstring(state, specializations.CanUseTalentSpecUiReason);
                return 2;
            case "CanPlayerUseTalentUI":
                lua_pushboolean(state, specializations.CanUseTalentUi ? 1 : 0);
                lua_pushstring(state, specializations.CanUseTalentUiReason);
                return 2;
            case "GetActiveSpecGroup":
                if (!TryReadOptionalBoolean(state, 1, out var isInspect, out _) ||
                    !TryReadOptionalBoolean(state, 2, out _, out _))
                {
                    return luaL_error(
                        state,
                        "Usage: local groupIndex = " +
                        "C_SpecializationInfo.GetActiveSpecGroup([isInspect, isPet])");
                }
                lua_pushinteger(state, specializations.ActiveSpecGroup);
                if (isInspect)
                {
                    lua_pop(state, 1);
                    lua_pushinteger(state, 1);
                }
                return 1;
            case "GetAllSelectedPvpTalentIDs":
                PushIntegerTable(state, specializations.SelectedPvpTalentIds);
                return 1;
            case "GetNumSpecializationsForClassID":
                if (!TryReadRequiredInt32(state, 1, out var classId))
                {
                    return luaL_error(
                        state,
                        "Usage: local specCount = " +
                        "C_SpecializationInfo.GetNumSpecializationsForClassID(classID)");
                }
                lua_pushinteger(
                    state,
                    specializations.CountsByClassId.TryGetValue(
                        classId,
                        out var count)
                        ? count
                        : 0);
                return 1;
            case "GetPvpTalentAlertStatus":
                lua_pushboolean(
                    state,
                    specializations.HasUnspentPvpTalentPoints ? 1 : 0);
                lua_pushboolean(state, specializations.HasNewPvpTalentSlot ? 1 : 0);
                return 2;
            case "GetSpecialization":
                const string specializationUsage =
                    "Usage: local specializationIndex = " +
                    "C_SpecializationInfo.GetSpecialization(" +
                    "[isInspect, isPet, specGroupIndex])";
                if (!TryReadOptionalBoolean(state, 1, out _, out _) ||
                    !TryReadOptionalBoolean(state, 2, out _, out _) ||
                    !TryReadOptionalUInt32(state, 3, out _))
                {
                    return luaL_error(state, specializationUsage);
                }
                if (client.SpecializationIndex is not { } specializationIndex)
                    return 0;
                lua_pushinteger(state, specializationIndex);
                return 1;
            case "GetSpecializationInfo":
                if (!TryReadRequiredOneBasedIndex(
                        state,
                        1,
                        out var infoSpecializationIndex) ||
                    !TryReadOptionalBoolean(state, 2, out _, out _) ||
                    !TryReadOptionalBoolean(state, 3, out _, out _))
                {
                    return luaL_error(
                        state,
                        "Usage: local specId, name, description, icon, role, " +
                        "primaryStat, pointsSpent, background, previewPointsSpent, " +
                        "isUnlocked = C_SpecializationInfo.GetSpecializationInfo(query)");
                }
                if (!specializations.CurrentInfoBySpecializationIndex.TryGetValue(
                        infoSpecializationIndex,
                        out var currentInfo))
                {
                    return 0;
                }
                lua_pushinteger(state, currentInfo.Id);
                PushOptionalString(state, currentInfo.Name);
                PushOptionalString(state, currentInfo.Description);
                PushOptionalInteger(state, currentInfo.IconFileDataId);
                PushOptionalString(state, currentInfo.Role);
                PushOptionalInteger(state, currentInfo.PrimaryStat);
                lua_pushinteger(state, currentInfo.PointsSpent);
                PushOptionalString(state, currentInfo.Background);
                lua_pushinteger(state, currentInfo.PreviewPointsSpent);
                lua_pushboolean(state, currentInfo.IsUnlocked ? 1 : 0);
                return 10;
            case "GetSpecializationInfoForClassID":
            {
                const string usage =
                    "Usage: local id, name, description, icon, role, recommended, " +
                    "allowedForBoost, masterySpell1, masterySpell2 = " +
                    "GetSpecializationInfoForClassID(classID, index [, gender])";
                if (!TryReadRequiredInt32(state, 1, out var requestedClassId) ||
                    !TryReadRequiredOneBasedIndex(state, 2, out var requestedIndex) ||
                    !TryReadOptionalInt32(state, 3, out _))
                {
                    return luaL_error(state, usage);
                }
                if (!specializations.InfoByClassAndIndex.TryGetValue(
                        (requestedClassId, requestedIndex),
                        out var info))
                {
                    return 0;
                }
                lua_pushinteger(state, info.Id);
                PushOptionalString(state, info.Name);
                PushOptionalString(state, info.Description);
                PushOptionalInteger(state, info.IconFileDataId);
                PushOptionalString(state, info.Role);
                lua_pushboolean(state, info.Recommended ? 1 : 0);
                lua_pushboolean(state, info.AllowedForBoost ? 1 : 0);
                PushOptionalInteger(state, info.MasterySpell1);
                PushOptionalInteger(state, info.MasterySpell2);
                return 9;
            }
            case "GetSpecializationInfoForSpecID":
            {
                const string usage =
                    "Usage: local id, name, description, icon, role, recommended, " +
                    "allowedForBoost, masterySpell1, masterySpell2 = " +
                    "GetSpecializationInfoForSpecID(specID [, gender])";
                if (!TryReadRequiredInt32(state, 1, out var requestedSpecId) ||
                    !TryReadOptionalInt32(state, 2, out _))
                {
                    return luaL_error(state, usage);
                }
                var info = specializations.InfoByClassAndIndex.Values
                    .FirstOrDefault(value => value.Id == requestedSpecId);
                if (info is null)
                    return 0;
                return PushLegacySpecializationInfo(state, info);
            }
            case "GetSpecializationInfoByID":
            {
                if (!TryReadRequiredInt32(state, 1, out var requestedSpecId))
                {
                    return luaL_error(
                        state,
                        "Usage: GetSpecializationInfoByID(specID[, sex])");
                }

                var infoById = specializations.CurrentInfoBySpecializationIndex.Values
                    .FirstOrDefault(value => value.Id == requestedSpecId);
                if (infoById is null)
                    return 0;

                lua_pushinteger(state, infoById.Id);
                PushOptionalString(state, infoById.Name);
                PushOptionalString(state, infoById.Description);
                PushOptionalInteger(state, infoById.IconFileDataId);
                PushOptionalString(state, infoById.Role);

                if (!specializations.ClassIdBySpecializationId.TryGetValue(
                        infoById.Id,
                        out var specClassId) ||
                    runtime.Classes.Classes.FirstOrDefault(value => value.Id == specClassId) is not
                        { } classInfo)
                {
                    return 5;
                }

                lua_pushstring(state, classInfo.FileName);
                lua_pushstring(state, classInfo.Name);
                return 7;
            }
            case "GetSpecializationRole":
            case "GetSpecializationRoleEnum":
            {
                if (!TryReadRequiredInt32(state, 1, out var requestedSpecIndex))
                {
                    return luaL_error(
                        state,
                        "Usage: GetSpecializationRole(specIndex[, isInspect[, isPet]])");
                }

                if (!specializations.CurrentInfoBySpecializationIndex.TryGetValue(
                        requestedSpecIndex,
                        out var roleInfo))
                {
                    return 0;
                }

                return PushSpecializationRole(
                    state,
                    roleInfo.Role,
                    operation.EndsWith("Enum", StringComparison.Ordinal));
            }
            case "GetSpecializationRoleByID":
            case "GetSpecializationRoleEnumByID":
            {
                if (!TryReadRequiredInt32(state, 1, out var roleSpecId))
                {
                    return luaL_error(
                        state,
                        "Usage: GetSpecializationInfoByID(specID)");
                }

                var roleInfo = specializations.CurrentInfoBySpecializationIndex.Values
                    .FirstOrDefault(value => value.Id == roleSpecId);
                if (roleInfo is null)
                    return 0;

                return PushSpecializationRole(
                    state,
                    roleInfo.Role,
                    operation.EndsWith("EnumByID", StringComparison.Ordinal));
            }
            case "IsInitialized":
                lua_pushboolean(state, specializations.IsInitialized ? 1 : 0);
                return 1;
            case "IsPvpTalentLocked":
                if (!TryReadRequiredInt32(state, 1, out var lockedTalentId))
                {
                    return luaL_error(
                        state,
                        "Usage: local locked = " +
                        "C_SpecializationInfo.IsPvpTalentLocked(talentID)");
                }
                lua_pushboolean(
                    state,
                    specializations.LockedPvpTalentIds.Contains(lockedTalentId) ? 1 : 0);
                return 1;
            case "MatchesCurrentSpecSet":
                if (!TryReadRequiredInt32(state, 1, out var specializationSetId))
                {
                    return luaL_error(
                        state,
                        "Usage: local matches = " +
                        "C_SpecializationInfo.MatchesCurrentSpecSet(specSetID)");
                }
                lua_pushboolean(
                    state,
                    specializations.CurrentSpecializationSetIds.Contains(
                        specializationSetId)
                        ? 1
                        : 0);
                return 1;
            case "SetPetSpecialization":
            {
                const string usage =
                    "Usage: C_SpecializationInfo.SetPetSpecialization(" +
                    "specIndex [, petNumber])";
                if (!TryReadRequiredOneBasedIndex(state, 1, out var petSpecIndex) ||
                    !TryReadOptionalUInt32(state, 2, out var petNumber))
                {
                    return luaL_error(state, usage);
                }
                specializations.PetSpecializationIndex = petSpecIndex;
                specializations.PetNumber = petNumber;
                return 0;
            }
            case "SetPvpTalentLocked":
            {
                const string usage =
                    "Usage: C_SpecializationInfo.SetPvpTalentLocked(talentID, locked)";
                if (!TryReadRequiredInt32(state, 1, out var pvpTalentId) ||
                    !TryReadRequiredBoolean(state, 2, out var locked))
                {
                    return luaL_error(state, usage);
                }
                if (locked)
                    specializations.LockedPvpTalentIds.Add(pvpTalentId);
                else
                    specializations.LockedPvpTalentIds.Remove(pvpTalentId);
                return 0;
            }
            case "SetSpecialization":
            {
                if (!TryReadRequiredOneBasedIndex(state, 1, out var requested))
                {
                    return luaL_error(
                        state,
                        "Usage: local success = " +
                        "C_SpecializationInfo.SetSpecialization(specIndex)");
                }
                var success = client.SpecializationCount == 0 ||
                              requested <= client.SpecializationCount;
                if (success)
                    client.SpecializationIndex = requested;
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "GetClassIDFromSpecID":
                if (!TryReadRequiredInt32(state, 1, out var specializationId))
                {
                    return luaL_error(
                        state,
                        "Usage: local classID = " +
                        "C_SpecializationInfo.GetClassIDFromSpecID(specID)");
                }
                PushOptionalInteger(
                    state,
                    specializations.ClassIdBySpecializationId.TryGetValue(
                        specializationId,
                        out var specializationClassId)
                        ? specializationClassId
                        : null);
                return 1;
            case "GetInspectSelectedPvpTalent":
                if (lua_isstring(state, 1) == 0 ||
                    !TryReadRequiredInt32(state, 2, out var inspectTalentIndex))
                {
                    return luaL_error(
                        state,
                        "Usage: local selectedTalentID = " +
                        "C_SpecializationInfo.GetInspectSelectedPvpTalent(" +
                        "inspectedUnit, talentIndex)");
                }
                var inspectedUnit = lua_tostring(state, 1) ?? string.Empty;
                PushOptionalInteger(
                    state,
                    specializations.InspectSelectedPvpTalentIds.TryGetValue(
                        (inspectedUnit, inspectTalentIndex),
                        out var selectedInspectTalentId)
                        ? selectedInspectTalentId
                        : null);
                return 1;
            case "GetPvpTalentInfo":
                if (!TryReadRequiredInt32(state, 1, out var talentInfoId))
                {
                    return luaL_error(
                        state,
                        "Usage: local talentInfo = " +
                        "C_SpecializationInfo.GetPvpTalentInfo(talentID)");
                }
                if (!specializations.PvpTalentInfoById.TryGetValue(
                        talentInfoId,
                        out var talentInfo))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_newtable(state);
                SetInteger(state, "talentID", talentInfo.TalentId);
                SetOptionalString(state, "name", talentInfo.Name);
                SetInteger(state, "icon", talentInfo.IconFileDataId);
                SetBoolean(state, "selected", talentInfo.Selected);
                SetBoolean(state, "available", talentInfo.Available);
                SetInteger(state, "spellID", talentInfo.SpellId);
                SetBoolean(state, "unlocked", talentInfo.Unlocked);
                SetBoolean(state, "known", talentInfo.Known);
                SetBoolean(state, "grantedByAura", talentInfo.GrantedByAura);
                SetBoolean(state, "dependenciesUnmet", talentInfo.DependenciesUnmet);
                SetInteger(
                    state,
                    "dependenciesUnmetReason",
                    talentInfo.DependenciesUnmetReason);
                return 1;
            case "GetPvpTalentSlotInfo":
                if (!TryReadRequiredInt32(state, 1, out var slotInfoIndex))
                {
                    return luaL_error(
                        state,
                        "Usage: local slotInfo = " +
                        "C_SpecializationInfo.GetPvpTalentSlotInfo(talentIndex)");
                }
                if (!specializations.PvpTalentSlotInfoByIndex.TryGetValue(
                        slotInfoIndex,
                        out var slotInfo))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_newtable(state);
                SetBoolean(state, "enabled", slotInfo.Enabled);
                SetInteger(state, "level", slotInfo.Level);
                SetOptionalInteger(state, "selectedTalentID", slotInfo.SelectedTalentId);
                lua_pushstring(state, "availableTalentIDs");
                PushIntegerTable(state, slotInfo.AvailableTalentIds);
                lua_settable(state, -3);
                return 1;
            case "GetPvpTalentSlotUnlockLevel":
                if (!TryReadRequiredInt32(state, 1, out var talentIndex))
                {
                    return luaL_error(
                        state,
                        "Usage: local requiredLevel = " +
                        "C_SpecializationInfo.GetPvpTalentSlotUnlockLevel(talentIndex)");
                }
                PushOptionalInteger(
                    state,
                    specializations.PvpTalentSlotUnlockLevel.TryGetValue(
                        talentIndex,
                        out var slotUnlockLevel)
                        ? slotUnlockLevel
                        : null);
                return 1;
            case "GetPvpTalentUnlockLevel":
                if (!TryReadRequiredInt32(state, 1, out var pvpTalentUnlockId))
                {
                    return luaL_error(
                        state,
                        "Usage: local requiredLevel = " +
                        "C_SpecializationInfo.GetPvpTalentUnlockLevel(talentID)");
                }
                PushOptionalInteger(
                    state,
                    specializations.PvpTalentUnlockLevel.TryGetValue(
                        pvpTalentUnlockId,
                        out var talentUnlockLevel)
                        ? talentUnlockLevel
                        : null);
                return 1;
            case "GetSpecIDs":
                if (!TryReadRequiredInt32(state, 1, out var specSetId))
                {
                    return luaL_error(
                        state,
                        "Usage: local specIDs = " +
                        "C_SpecializationInfo.GetSpecIDs(specSetID)");
                }
                PushIntegerTable(
                    state,
                    specializations.SpecializationIdsBySetId.TryGetValue(
                        specSetId,
                        out var specializationIds)
                        ? specializationIds
                        : []);
                return 1;
            case "GetSpecializationMasterySpells":
            {
                const string usage =
                    "Usage: local spellIDs = " +
                    "C_SpecializationInfo.GetSpecializationMasterySpells(" +
                    "specializationIndex [, isInspect, isPet])";
                if (!TryReadRequiredOneBasedIndex(
                        state,
                        1,
                        out var masterySpecializationIndex) ||
                    !TryReadOptionalBoolean(state, 2, out _, out _) ||
                    !TryReadOptionalBoolean(state, 3, out _, out _))
                {
                    return luaL_error(state, usage);
                }
                PushIntegerTable(
                    state,
                    specializations.MasterySpellIdsBySpecializationIndex.TryGetValue(
                        masterySpecializationIndex,
                        out var masterySpellIds)
                        ? masterySpellIds
                        : []);
                return 1;
            }
            case "GetSpellsDisplay":
                if (!TryReadRequiredInt32(
                        state,
                        1,
                        out var displaySpecializationId))
                {
                    return luaL_error(
                        state,
                        "Usage: local spellID = " +
                        "C_SpecializationInfo.GetSpellsDisplay(specializationID)");
                }
                PushIntegerTable(
                    state,
                    specializations.DisplaySpellIdsBySpecializationId.TryGetValue(
                        displaySpecializationId,
                        out var displaySpellIds)
                        ? displaySpellIds
                        : []);
                return 1;
            case "GetTalentInfo":
                if (lua_type(state, 1) != LUA_TTABLE)
                {
                    return luaL_error(
                        state,
                        "Usage: local result = " +
                        "C_SpecializationInfo.GetTalentInfo(query)");
                }
                lua_pushnil(state);
                return 1;
            default:
                return 0;
        }
    }

    private static int PushLegacySpecializationInfo(
        lua_State state,
        WowSpecializationInfoState info)
    {
        lua_pushinteger(state, info.Id);
        PushOptionalString(state, info.Name);
        PushOptionalString(state, info.Description);
        PushOptionalInteger(state, info.IconFileDataId);
        PushOptionalString(state, info.Role);
        lua_pushboolean(state, info.Recommended ? 1 : 0);
        lua_pushboolean(state, info.AllowedForBoost ? 1 : 0);
        PushOptionalInteger(state, info.MasterySpell1);
        PushOptionalInteger(state, info.MasterySpell2);
        return 9;
    }

    private static int PushSpecializationRole(lua_State state, string? role, bool asEnum)
    {
        if (!asEnum)
        {
            PushOptionalString(state, role);
            return 1;
        }

        lua_pushinteger(
            state,
            role?.ToUpperInvariant() switch
            {
                "TANK" => 0,
                "HEALER" => 1,
                "DAMAGER" => 2,
                _ => 0
            });
        return 1;
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadRequiredOneBasedIndex(
        lua_State state,
        int index,
        out int value) =>
        TryReadRequiredInt32(state, index, out value) && value > 0;

    private static bool TryReadOptionalInt32(
        lua_State state,
        int index,
        out int? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (!TryReadRequiredInt32(state, index, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private static bool TryReadOptionalUInt32(
        lua_State state,
        int index,
        out uint? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }

    private static bool TryReadRequiredBoolean(
        lua_State state,
        int index,
        out bool value)
    {
        value = false;
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TBOOLEAN)
            return false;
        value = lua_toboolean(state, index) != 0;
        return true;
    }

    private static bool TryReadOptionalBoolean(
        lua_State state,
        int index,
        out bool value,
        out bool present)
    {
        value = false;
        present = false;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_type(state, index) != LUA_TBOOLEAN)
            return false;
        value = lua_toboolean(state, index) != 0;
        present = true;
        return true;
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

    private static void PushIntegerTable(lua_State state, IEnumerable<int> values)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var value in values)
        {
            lua_pushinteger(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(lua_State state, string name, int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(lua_State state, string name, string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
