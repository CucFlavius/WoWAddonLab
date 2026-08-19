namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPlayerCurrencyCategoryInfo(
    string? CategoryName,
    IReadOnlyList<int> CurrencyTypes,
    IReadOnlyList<int> ChildCategories);
