namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBankTabData(
    int Id,
    byte BankType,
    string? Name,
    int? IconFileId,
    uint DepositFlags,
    string? TabCleanupConfirmation,
    string? TabNameEditBoxHeader);
