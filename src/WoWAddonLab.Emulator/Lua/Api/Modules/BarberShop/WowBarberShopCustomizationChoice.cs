using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopCustomizationChoice(
    int Id,
    string? Name,
    bool IneligibleChoice,
    bool IsNew,
    WowBarberShopColor? SwatchColor1,
    WowBarberShopColor? SwatchColor2,
    int? SoundKitId,
    bool IsLocked,
    string? LockedText);
