using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopCustomizationCategory(
    int Id,
    int OrderIndex,
    string? Name,
    string Icon,
    string SelectedIcon,
    bool UndressModel,
    bool Subcategory,
    int CameraZoomLevel,
    float CameraDistanceOffset,
    int? SpellShapeshiftFormId,
    int? ChrModelId,
    IReadOnlyList<WowBarberShopCustomizationOption> Options,
    bool HasNewChoices,
    bool NeedsNativeFormCategory);
