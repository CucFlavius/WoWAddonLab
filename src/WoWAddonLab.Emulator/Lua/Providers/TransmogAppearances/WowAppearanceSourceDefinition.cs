namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAppearanceSourceDefinition(
    int VisualId,
    int SourceId,
    int ItemId,
    int ItemModId,
    int ItemSubclass,
    int IconFileDataId,
    int InventoryType,
    int CategoryId,
    int UiOrder,
    int? InventorySlot,
    int? SourceType,
    string? Name,
    int? Quality,
    int AllowableClassMask,
    int RequiredTransmogHolidayId,
    bool? MeetsTransmogPlayerCondition,
    bool? IsHideVisual);
