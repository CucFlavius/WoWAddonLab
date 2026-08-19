namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingDecorDyeSlotState(
    int Id,
    int DyeColorCategoryId,
    int OrderIndex,
    int Channel,
    int? DyeColorId,
    string? DyeColorName);
