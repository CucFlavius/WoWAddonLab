using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowStableInfoApi : LuaApiModule
{
    private const int StableMasterInteractionType = 22;
    private const int FirstStableSlotId = 6;
    private const int MaximumSlotId = 205;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ClosePetStables",
        "GetActivePetList",
        "GetAvailablePetSpecInfos",
        "GetNumActivePets",
        "GetNumStablePets",
        "GetStablePetFoodTypes",
        "GetStablePetInfo",
        "GetStabledPetList",
        "IsAtStableMaster",
        "IsBonusPetSlotAvailable",
        "IsPetFavorite",
        "PickupStablePet",
        "SetPetFavorite",
        "SetPetSlot"
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
        lua_setglobal(state, "C_StableInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var stable = runtime.StableInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ??
                        string.Empty;

        switch (operation)
        {
            case "ClosePetStables":
                Close(runtime);
                return 0;
            case "GetActivePetList":
                return PushPetList(
                    state,
                    stable.Pets
                        .Where(
                            pet => pet.SlotId is >= 1 and
                                < FirstStableSlotId)
                        .OrderBy(pet => pet.SlotId));
            case "GetAvailablePetSpecInfos":
                return PushPetSpecList(state, stable.AvailablePetSpecs);
            case "GetNumActivePets":
                lua_pushnumber(
                    state,
                    stable.Pets.Count(
                        pet => pet.SlotId is >= 1 and
                            < FirstStableSlotId));
                return 1;
            case "GetNumStablePets":
                lua_pushnumber(
                    state,
                    stable.Pets.Count(
                        pet => pet.SlotId is >= FirstStableSlotId and
                            <= MaximumSlotId));
                return 1;
            case "GetStablePetFoodTypes":
            {
                var slotId = RequiredSlotId(
                    state,
                    1,
                    "Usage: local foodTypes = " +
                    "C_StableInfo.GetStablePetFoodTypes(index)");
                var pet = FindPet(stable, slotId);
                PushStringList(state, pet?.FoodTypes ?? []);
                return 1;
            }
            case "GetStablePetInfo":
            {
                var slotId = RequiredSlotId(
                    state,
                    1,
                    "Usage: local petInfo = " +
                    "C_StableInfo.GetStablePetInfo(index)");
                var pet = FindPet(stable, slotId);
                if (pet is null)
                    lua_pushnil(state);
                else
                    PushPetInfo(state, pet);
                return 1;
            }
            case "GetStabledPetList":
                return PushPetList(
                    state,
                    stable.Pets
                        .Where(
                            pet => pet.SlotId is >= FirstStableSlotId and
                                <= MaximumSlotId)
                        .OrderBy(pet => pet.SlotId));
            case "IsAtStableMaster":
                lua_pushboolean(state, stable.IsAtStableMaster ? 1 : 0);
                return 1;
            case "IsBonusPetSlotAvailable":
                lua_pushboolean(
                    state,
                    stable.IsBonusPetSlotAvailable ? 1 : 0);
                return 1;
            case "IsPetFavorite":
            {
                var slotId = RequiredSlotId(
                    state,
                    1,
                    "Usage: local isFavorite = " +
                    "C_StableInfo.IsPetFavorite(slot)");
                lua_pushboolean(
                    state,
                    FindPet(stable, slotId)?.IsFavorite == true ? 1 : 0);
                return 1;
            }
            case "PickupStablePet":
                PickupPet(state, runtime, stable);
                return 0;
            case "SetPetFavorite":
                SetFavorite(state, stable);
                return 0;
            case "SetPetSlot":
                SetSlot(state, stable);
                return 0;
            default:
                return 0;
        }
    }

    private static void PickupPet(
        lua_State state,
        LuaRuntime runtime,
        WowStableInfoState stable)
    {
        const string usage =
            "Usage: C_StableInfo.PickupStablePet(index)";
        var slotId = RequiredSlotId(state, 1, usage);
        runtime.Cursor.ClearPayload();

        var pet = FindPet(stable, slotId);
        if (pet is null || pet.PetNumber == 0)
            return;

        stable.LastPickedUpSlotId = slotId;
        runtime.Cursor.SetPayload(
            WowCursorPayloadKind.Pet,
            "pet",
            slotId);
    }

    private static void SetFavorite(
        lua_State state,
        WowStableInfoState stable)
    {
        const string usage =
            "Usage: C_StableInfo.SetPetFavorite(slot, isFavorite)";
        var slotId = RequiredSlotId(state, 1, usage);
        var isFavorite = RequiredBoolean(state, 2, usage);
        stable.LastFavoriteRequest =
            new WowStablePetFavoriteRequest(slotId, isFavorite);
    }

    private static void SetSlot(
        lua_State state,
        WowStableInfoState stable)
    {
        const string usage =
            "Usage: C_StableInfo.SetPetSlot(index, slot)";
        var sourceSlotId = RequiredSlotId(state, 1, usage);
        var destinationSlotId = RequiredSlotId(state, 2, usage);
        if (sourceSlotId is < 1 or > MaximumSlotId ||
            destinationSlotId is < 1 or > MaximumSlotId ||
            sourceSlotId == destinationSlotId)
        {
            return;
        }

        var pet = FindPet(stable, sourceSlotId);
        if (pet is null || pet.PetNumber == 0)
            return;

        stable.LastSlotRequest = new WowStablePetSlotRequest(
            sourceSlotId,
            destinationSlotId,
            pet.PetNumber);
    }

    private static void Close(LuaRuntime runtime)
    {
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType =
            StableMasterInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType !=
                StableMasterInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
        runtime.TriggerEvent("PET_STABLE_CLOSED");
    }

    private static int PushPetList(
        lua_State state,
        IEnumerable<WowStablePetInfoState> pets)
    {
        var list = pets.ToList();
        lua_createtable(state, list.Count, 0);
        for (var index = 0; index < list.Count; index++)
        {
            PushPetInfo(state, list[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushPetSpecList(
        lua_State state,
        IEnumerable<WowStablePetSpecInfoState> specs)
    {
        var list = specs.ToList();
        lua_createtable(state, list.Count, 0);
        for (var index = 0; index < list.Count; index++)
        {
            var spec = list[index];
            lua_createtable(state, 0, 3);
            SetNumber(state, "specID", spec.SpecId);
            SetNumber(state, "specIndex", spec.SpecIndex);
            SetString(
                state,
                "specializationName",
                spec.SpecializationName);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushPetInfo(
        lua_State state,
        WowStablePetInfoState pet)
    {
        lua_createtable(state, 0, 16);
        SetNumber(state, "slotID", pet.SlotId);
        SetNumber(state, "icon", pet.Icon);
        SetString(state, "name", pet.Name);
        SetNumber(state, "level", pet.Level);
        SetString(state, "familyName", pet.FamilyName);
        SetString(state, "specialization", pet.Specialization);
        SetString(state, "type", pet.Type);
        SetNumberList(state, "petAbilities", pet.PetAbilities);
        SetNumberList(state, "specAbilities", pet.SpecAbilities);
        SetNumber(state, "displayID", pet.DisplayId);
        SetBoolean(state, "isFavorite", pet.IsFavorite);
        SetBoolean(state, "isExotic", pet.IsExotic);
        SetNumber(state, "uiModelSceneID", pet.UiModelSceneId);
        SetNumber(state, "petNumber", pet.PetNumber);
        SetNumber(state, "creatureID", pet.CreatureId);
        SetNumber(state, "specID", pet.SpecId);
    }

    private static void SetNumberList(
        lua_State state,
        string field,
        IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, field);
    }

    private static void PushStringList(
        lua_State state,
        IReadOnlyList<string> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushstring(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static WowStablePetInfoState? FindPet(
        WowStableInfoState stable,
        int slotId) =>
        stable.Pets.FirstOrDefault(pet => pet.SlotId == slotId);

    private static int RequiredSlotId(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_isnumber(state, index) == 0)
        {
            return RaiseArgumentError(state, usage);
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > uint.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }

        var zeroBased = unchecked((int)(long)(value - 1.0));
        return unchecked(zeroBased + 1);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) == LUA_TNIL)
        {
            RaiseArgumentError(state, usage);
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
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

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }
}
