using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPetJournalApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CagePetByID", "ClearFanfare", "ClearHoveredBattlePet",
        "GetNumCollectedInfo", "GetNumPets", "GetNumPetsNeedingFanfare", "GetNumPetSources", "GetNumPetTypes", "GetSummonBattlePetCooldown",
        "GetBattlePetLink", "GetPetAbilityInfo", "GetPetAbilityList", "GetPetCooldownByGUID",
        "GetPetInfoByIndex", "GetPetInfoByPetID", "GetPetInfoBySpeciesID", "GetPetInfoTableByPetID",
        "GetPetLoadOutInfo", "GetPetModelSceneInfoBySpeciesID", "GetPetSortParameter",
        "GetPetStats", "GetPetSummonInfo", "GetSearchFilter", "GetSummonedPetGUID",
        "HasFavoritePets", "IsFilterChecked", "IsJournalUnlocked", "IsPetSourceChecked",
        "IsFindBattleEnabled", "IsPetTypeChecked", "IsUsingDefaultFilters", "ClearRecentFanfares",
        "PetCanBeReleased", "PetIsFavorite", "PetIsHurt", "PetIsLockedForConvert",
        "PetIsRevoked", "PetIsSlotted", "PetIsSummonable", "PetIsTradable", "PetNeedsFanfare",
        "PickupPet", "PickupSummonRandomPet", "ReleasePetByID", "SetAbility",
        "SetAllPetSourcesChecked", "SetAllPetTypesChecked", "SetCustomName", "SetDefaultFilters",
        "SetFavorite", "SetFilterChecked", "SetHoveredBattlePet", "SetPetSortParameter",
        "SetPetSourceChecked", "SetPetTypeFilter", "SetSearchFilter", "SpellTargetBattlePet",
        "SummonPetByGUID", "SummonRandomPet"
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
        lua_setglobal(state, "C_PetJournal");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "SpellTargetBattlePet":
            {
                const string usage =
                    "Usage: C_PetJournal." +
                    "SpellTargetBattlePet(battlePetGUID)";
                if (lua_isstring(state, 1) == 0)
                    return luaL_error(state, usage);
                var guid = lua_tostring(state, 1) ?? string.Empty;
                var targeting =
                    LuaBindings.GetRuntime(state).SpellTargeting;
                targeting.TargetRequests.Add(
                    new WowSpellTargetRequest(operation, guid));
                targeting.Clear();
                return 0;
            }
            case "GetNumPets":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "GetNumCollectedInfo":
            {
                const string usage = "Usage: GetNumCollectedInfo(speciesID)";
                if (lua_isnumber(state, 1) == 0)
                    return luaL_error(state, usage);
                var value = lua_tonumber(state, 1);
                if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
                    return luaL_error(state, usage);
                var speciesId = (int)value;
                var journal = LuaBindings.GetRuntime(state).PetJournal;
                if (!journal.CollectionInfoBySpeciesId.TryGetValue(speciesId, out var info))
                    return 0;
                lua_pushinteger(state, info.Collected);
                lua_pushinteger(state, info.Limit);
                return 2;
            }
            case "GetSummonBattlePetCooldown":
                lua_pushnumber(state, 0);
                lua_pushnumber(state, 0);
                lua_pushboolean(state, 0);
                return 3;
            case "GetNumPetSources":
            case "GetNumPetTypes":
            case "GetPetSortParameter":
                lua_pushinteger(state, 0);
                return 1;
            case "GetNumPetsNeedingFanfare":
                lua_pushinteger(
                    state,
                    LuaBindings.GetRuntime(state).CommunityFeatures.PetsNeedingFanfare);
                return 1;
            case "GetPetInfoByIndex":
            case "GetPetInfoByPetID":
            case "GetPetInfoBySpeciesID":
            case "GetPetInfoTableByPetID":
            case "GetPetAbilityInfo":
            case "GetPetCooldownByGUID":
            case "GetPetModelSceneInfoBySpeciesID":
            case "GetPetStats":
            case "GetSummonedPetGUID":
            case "GetBattlePetLink":
                return 0;
            case "GetPetAbilityList":
                lua_newtable(state);
                lua_newtable(state);
                return 2;
            case "GetPetLoadOutInfo":
                lua_pushnil(state);
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                lua_pushboolean(state, 0);
                return 5;
            case "GetSearchFilter":
                lua_pushstring(state, string.Empty);
                return 1;
            case "GetPetSummonInfo":
                lua_pushboolean(state, 0);
                lua_pushinteger(state, 0);
                lua_pushstring(state, string.Empty);
                return 3;
            case "HasFavoritePets":
            case "IsFindBattleEnabled":
            case "PetCanBeReleased":
            case "PetIsFavorite":
            case "PetIsHurt":
            case "PetIsLockedForConvert":
            case "PetIsRevoked":
            case "PetIsSlotted":
            case "PetIsSummonable":
            case "PetIsTradable":
            case "PetNeedsFanfare":
                lua_pushboolean(state, 0);
                return 1;
            case "IsFilterChecked":
            case "IsPetSourceChecked":
            case "IsPetTypeChecked":
            case "IsUsingDefaultFilters":
            case "IsJournalUnlocked":
                lua_pushboolean(state, 1);
                return 1;
            default:
                return 0;
        }
    }
}
