namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitEditabilityState(
    bool CanEdit,
    string? ErrorMessage = null);
