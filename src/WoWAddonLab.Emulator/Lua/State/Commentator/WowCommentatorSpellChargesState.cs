namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorSpellChargesState(
    int Charges,
    int MaxCharges,
    double StartTime,
    double Duration);
