namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorFollowRequest(
    uint FactionIndex,
    uint PlayerIndex,
    bool ForceInstantTransition);
