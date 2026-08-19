namespace WoWAddonLab.Emulator.Lua;

public sealed record WowShapeshiftFormState(
    string? Icon,
    bool Active,
    bool Castable,
    uint SpellId);
