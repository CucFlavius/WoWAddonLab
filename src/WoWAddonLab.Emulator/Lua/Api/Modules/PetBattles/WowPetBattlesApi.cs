using LuaNET.Lua51;
using WoWAddonLab.Emulator.Diagnostics;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPetBattlesApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetBreedQuality",
        "GetAllEffectNames",
        "GetAllStates",
        "GetAbilityInfo",
        "GetActivePet",
        "GetBattleState",
        "GetForfeitPenalty",
        "GetHealth",
        "GetIcon",
        "GetMaxHealth",
        "GetName",
        "GetNumPets",
        "GetPVPMatchmakingInfo",
        "GetSelectedAction",
        "GetXP",
        "CanActivePetSwapOut",
        "CanPetSwapIn",
        "IsInBattle",
        "IsPlayerNPC",
        "IsSkipAvailable",
        "IsTrapAvailable",
        "IsWaitingOnOpponent",
        "ShouldShowPetSelect",
        "IsWildBattle"
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
        lua_setglobal(state, "C_PetBattles");
    }

    private static int Dispatch(lua_State state)
    {
        var petBattles = LuaBindings.GetRuntime(state).PetBattles;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetAbilityInfo":
                if (lua_gettop(state) >= 3)
                {
                    _ = RequiredInt32(
                        state,
                        1,
                        "Usage: C_PetBattles.GetAbilityInfo(petOwner, petIndex, abilityIndex)");
                    _ = RequiredInt32(
                        state,
                        2,
                        "Usage: C_PetBattles.GetAbilityInfo(petOwner, petIndex, abilityIndex)");
                    _ = RequiredInt32(
                        state,
                        3,
                        "Usage: C_PetBattles.GetAbilityInfo(petOwner, petIndex, abilityIndex)");
                }
                return 0;
            case "GetActivePet":
            {
                var owner = OptionalInt32(state, 1);
                var activePet = owner.HasValue &&
                    petBattles.ActivePets.TryGetValue(owner.Value, out var value)
                        ? value
                        : 1;
                lua_pushinteger(state, activePet);
                return 1;
            }
            case "GetAllEffectNames":
                LuaBindings.GetRuntime(state).Log.Write(
                    EmulatorLogLevel.Trace,
                    "compat",
                    "C_PetBattles.GetAllEffectNames: pet-battle effect metadata is unavailable; returning no names.");
                return 0;
            case "GetAllStates":
                if (lua_istable(state, 1) != 0)
                    lua_pushvalue(state, 1);
                else
                    lua_newtable(state);
                LuaBindings.GetRuntime(state).Log.Write(
                    EmulatorLogLevel.Trace,
                    "compat",
                    "C_PetBattles.GetAllStates: pet-battle state metadata is unavailable; returning the unmodified state table.");
                return 1;
            case "GetBattleState":
                lua_pushinteger(state, (int)petBattles.BattleState);
                return 1;
            case "GetBreedQuality":
            {
                var key = RequiredPet(state, operation);
                var quality = petBattles.Pets.TryGetValue(key, out var pet)
                    ? pet.BreedQuality
                    : 0;
                lua_pushinteger(state, quality);
                return 1;
            }
            case "GetForfeitPenalty":
                lua_pushinteger(state, petBattles.ForfeitPenalty);
                return 1;
            case "GetHealth":
            {
                var key = RequiredPet(state, operation);
                var health = petBattles.Pets.TryGetValue(key, out var pet)
                    ? pet.Health
                    : 100;
                lua_pushinteger(state, health);
                return 1;
            }
            case "GetIcon":
            {
                var key = RequiredPet(state, operation);
                if (!petBattles.Pets.TryGetValue(key, out var pet) ||
                    !pet.IconFileId.HasValue)
                {
                    return 0;
                }

                lua_pushinteger(state, pet.IconFileId.Value);
                return 1;
            }
            case "GetMaxHealth":
            {
                var key = RequiredPet(state, operation);
                var maxHealth = petBattles.Pets.TryGetValue(key, out var pet)
                    ? pet.MaxHealth
                    : 100;
                lua_pushinteger(state, maxHealth);
                return 1;
            }
            case "GetName":
            {
                var key = RequiredPet(state, operation);
                if (!petBattles.Pets.TryGetValue(key, out var pet))
                {
                    return 0;
                }

                lua_pushstring(state, pet.CustomName);
                lua_pushstring(state, pet.SpeciesName);
                return 2;
            }
            case "GetNumPets":
            {
                var owner = OptionalInt32(state, 1);
                var count = owner.HasValue &&
                    petBattles.PetCounts.TryGetValue(owner.Value, out var value)
                        ? value
                        : 0;
                lua_pushinteger(state, count);
                return 1;
            }
            case "GetPVPMatchmakingInfo":
                if (petBattles.PvpMatchmaking is { } matchmaking)
                    lua_pushstring(state, matchmaking.Status);
                else
                    lua_pushnil(state);
                lua_pushinteger(state, petBattles.PvpMatchmaking?.EstimatedSeconds ?? 0);
                lua_pushnumber(state, petBattles.PvpMatchmaking?.QueuedSeconds ?? 0);
                return 3;
            case "GetSelectedAction":
                if (petBattles.SelectedAction is not { } selectedAction)
                    return 0;
                lua_pushinteger(state, selectedAction.ActionType);
                lua_pushinteger(state, selectedAction.Index);
                return 2;
            case "GetXP":
            {
                var key = RequiredPet(state, operation);
                var xp = petBattles.Pets.TryGetValue(key, out var pet)
                    ? pet.Xp
                    : 0;
                var maxXp = petBattles.Pets.TryGetValue(key, out pet)
                    ? pet.MaxXp
                    : 50;
                lua_pushinteger(state, xp);
                lua_pushinteger(state, maxXp);
                return 2;
            }
            case "CanActivePetSwapOut":
                lua_pushboolean(state, petBattles.CanActivePetSwapOut ? 1 : 0);
                return 1;
            case "CanPetSwapIn":
            {
                var petIndex = RequiredInt32(
                    state,
                    1,
                    "Usage: CanPetSwapIn(petSlotIndex)");
                lua_pushboolean(
                    state,
                    petBattles.SwappablePetIndices.Contains(petIndex) ? 1 : 0);
                return 1;
            }
            case "IsInBattle":
                lua_pushboolean(state, petBattles.IsInBattle ? 1 : 0);
                return 1;
            case "IsPlayerNPC":
                lua_pushboolean(state, petBattles.IsPlayerNpc ? 1 : 0);
                return 1;
            case "IsSkipAvailable":
                lua_pushboolean(state, petBattles.IsSkipAvailable ? 1 : 0);
                return 1;
            case "IsTrapAvailable":
                lua_pushboolean(state, petBattles.IsTrapAvailable ? 1 : 0);
                lua_pushinteger(state, petBattles.TrappablePetCount);
                return 2;
            case "IsWaitingOnOpponent":
                lua_pushboolean(state, petBattles.IsWaitingOnOpponent ? 1 : 0);
                return 1;
            case "ShouldShowPetSelect":
                lua_pushboolean(state, petBattles.ShouldShowPetSelect ? 1 : 0);
                return 1;
            case "IsWildBattle":
                lua_pushboolean(state, petBattles.IsWildBattle ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static int? OptionalInt32(lua_State state, int index)
    {
        if (lua_gettop(state) < index)
            return null;

        return RequiredInt32(
            state,
            index,
            "Usage: expected an integer enum value");
    }

    private static (int PetOwner, int Slot) RequiredPet(
        lua_State state,
        string operation)
    {
        var usage =
            $"Usage: C_PetBattles.{operation}(petOwner, slot)";
        return (
            RequiredNumericEnum(state, 1, 2, usage),
            RequiredInt32(state, 2, usage));
    }

    private static int RequiredNumericEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static void RegisterEnums(lua_State state)
    {
        EnsureGlobalTable(state, "Enum");

        SetEnum(
            state,
            "PetbattleSlot",
            [
                ("Slot_0", 0),
                ("Slot_1", 1),
                ("Slot_2", 2)
            ]);
        SetEnumMeta(state, "PetbattleSlotMeta", 3, 0, 2);

        SetEnum(
            state,
            "PetbattleState",
            [
                ("Created", 0),
                ("WaitingPreBattle", 1),
                ("RoundInProgress", 2),
                ("WaitingForFrontPets", 3),
                ("CreatedFailed", 4),
                ("FinalRound", 5),
                ("Finished", 6)
            ]);
        SetEnumMeta(state, "PetbattleStateMeta", 7, 0, 6);

        SetEnum(
            state,
            "PetBattleQueueStatus",
            [
                ("None", 0),
                ("Queued", 1),
                ("QueuedUpdate", 2),
                ("AlreadyQueued", 3),
                ("JoinFailed", 4),
                ("JoinFailedSlots", 5),
                ("JoinFailedJournalLock", 6),
                ("JoinFailedNeutral", 7),
                ("MatchAccepted", 8),
                ("MatchDeclined", 9),
                ("MatchOpponentDeclined", 10),
                ("ProposalTimedOut", 11),
                ("Removed", 12),
                ("RequeuedAfterInternalError", 13),
                ("RequeuedAfterOpponentRemoved", 14),
                ("Matchmaking", 15),
                ("LostConnection", 16),
                ("Shutdown", 17),
                ("Suspended", 18),
                ("Unsuspended", 19),
                ("InBattle", 20),
                ("NoBattlingHere", 21)
            ]);
        SetEnumMeta(state, "PetBattleQueueStatusMeta", 22, 0, 21);

        lua_pop(state, 1);
    }

    private static void EnsureGlobalTable(lua_State state, string name)
    {
        lua_getglobal(state, name);
        if (lua_istable(state, -1) != 0)
        {
            return;
        }

        lua_pop(state, 1);
        lua_newtable(state);
        lua_pushvalue(state, -1);
        lua_setglobal(state, name);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, int Value)> members)
    {
        lua_newtable(state);
        foreach (var member in members)
        {
            lua_pushinteger(state, member.Value);
            lua_setfield(state, -2, member.Name);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int numValues,
        int minValue,
        int maxValue) =>
        SetEnum(
            state,
            name,
            [
                ("NumValues", numValues),
                ("MinValue", minValue),
                ("MaxValue", maxValue)
            ]);
}
