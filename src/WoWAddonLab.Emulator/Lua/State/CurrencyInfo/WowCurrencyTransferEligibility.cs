namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCurrencyTransferEligibility(
    bool CanTransfer,
    WowAccountCurrencyTransferResult? FailureReason);
