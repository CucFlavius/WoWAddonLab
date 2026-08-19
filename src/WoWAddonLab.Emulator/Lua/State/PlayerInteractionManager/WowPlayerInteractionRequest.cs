namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPlayerInteractionRequest(
    string Unit,
    bool ExactMatch,
    bool LooseTargeting);
