namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCharacterCurrencyData(
    string CharacterGuid,
    string CharacterName,
    string FullCharacterName,
    int CurrencyId,
    int Quantity);
