namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingDecorInstanceInfoState(
    string DecorGuid,
    int DecorId,
    string? Name,
    bool IsLocked,
    bool CanBeCustomized,
    bool CanBeRemoved,
    bool IsAllowedOutdoors,
    bool IsAllowedIndoors,
    bool IsRefundable,
    IReadOnlyList<WowHousingDecorDyeSlotState> DyeSlots,
    IReadOnlyDictionary<int, object?> DataTagsById,
    uint Size);
