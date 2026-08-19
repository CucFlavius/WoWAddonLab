namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBackpackCurrencyInfo(
    string Name,
    int Quantity,
    int IconFileId,
    int CurrencyTypesId);
