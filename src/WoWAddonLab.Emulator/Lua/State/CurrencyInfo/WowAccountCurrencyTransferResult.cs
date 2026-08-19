namespace WoWAddonLab.Emulator.Lua;

public enum WowAccountCurrencyTransferResult
{
    Success = 0,
    InvalidCharacter = 1,
    CharacterLoggedIn = 2,
    InsufficientCurrency = 3,
    MaxQuantity = 4,
    InvalidCurrency = 5,
    NoValidSourceCharacter = 6,
    ServerError = 7,
    CannotUseCurrency = 8,
    TransactionInProgress = 9,
    CurrencyTransferDisabled = 10
}
