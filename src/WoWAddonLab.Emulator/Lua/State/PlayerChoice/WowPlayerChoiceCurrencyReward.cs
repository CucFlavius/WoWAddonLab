namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPlayerChoiceCurrencyReward(
    int CurrencyId,
    string Name,
    int CurrencyTexture,
    int Quantity,
    bool IsCurrencyContainer);
