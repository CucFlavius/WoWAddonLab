namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderClaimInfoState(
    int ClaimsRemaining,
    int? SecondsToRecharge);
