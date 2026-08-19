namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderCustomerCategoryState(
    string CategoryName,
    int CategoryId,
    int UiSortOrder,
    int? PrimaryCategorySortOrder,
    int? SecondaryCategorySortOrder,
    uint Type);
