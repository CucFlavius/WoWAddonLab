namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCurrencyTransferTransaction(
    string SourceCharacterGuid,
    string SourceCharacterName,
    string FullSourceCharacterName,
    string DestinationCharacterGuid,
    string DestinationCharacterName,
    string FullDestinationCharacterName,
    int CurrencyType,
    int QuantityTransferred,
    int TotalQuantityConsumed,
    long Timestamp);
