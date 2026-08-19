using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopCustomizationOption(
    int Id,
    string? Name,
    int OrderIndex,
    uint OptionType,
    IReadOnlyList<WowBarberShopCustomizationChoice> Choices,
    int? CurrentChoiceIndex,
    bool HasNewChoices,
    bool IsSound);
