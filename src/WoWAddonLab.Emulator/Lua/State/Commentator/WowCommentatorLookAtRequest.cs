namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorLookAtRequest(
    uint FactionIndex,
    uint PlayerIndex,
    uint? LookAtIndex);
