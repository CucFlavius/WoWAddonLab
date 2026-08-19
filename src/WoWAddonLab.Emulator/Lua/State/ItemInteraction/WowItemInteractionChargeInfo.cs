namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemInteractionChargeInfo(
    int NewChargeAmount,
    int RechargeRate,
    int TimeToNextCharge);
