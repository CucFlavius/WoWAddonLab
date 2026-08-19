namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingCatalogCategoryState(
    int Id,
    int OrderIndex,
    string? Name,
    string? Icon,
    IReadOnlyList<int> SubcategoryIds,
    bool AnyStoredEntries);
