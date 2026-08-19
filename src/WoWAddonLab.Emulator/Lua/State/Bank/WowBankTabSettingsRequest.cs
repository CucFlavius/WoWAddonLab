namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBankTabSettingsRequest(
    byte BankType,
    int TabId,
    string TabName,
    string TabIcon,
    uint DepositFlags);
