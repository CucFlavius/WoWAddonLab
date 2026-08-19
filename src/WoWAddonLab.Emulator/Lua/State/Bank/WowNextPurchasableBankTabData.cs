namespace WoWAddonLab.Emulator.Lua;

public sealed record WowNextPurchasableBankTabData(
    ulong TabCost,
    bool CanAfford,
    string? PurchasePromptTitle,
    string? PurchasePromptBody,
    string? PurchasePromptConfirmation);
