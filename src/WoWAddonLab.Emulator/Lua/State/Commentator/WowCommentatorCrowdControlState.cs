namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorCrowdControlState(
    int SpellId,
    double Expiration,
    double Duration);
