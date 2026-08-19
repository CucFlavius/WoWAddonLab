using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStablePetInfoState(
    int SlotId,
    uint Icon,
    string Name,
    int Level,
    string FamilyName,
    string Specialization,
    string Type,
    IReadOnlyList<int> PetAbilities,
    IReadOnlyList<int> SpecAbilities,
    int DisplayId,
    bool IsFavorite,
    bool IsExotic,
    int UiModelSceneId,
    int PetNumber,
    int CreatureId,
    int SpecId,
    IReadOnlyList<string> FoodTypes);
