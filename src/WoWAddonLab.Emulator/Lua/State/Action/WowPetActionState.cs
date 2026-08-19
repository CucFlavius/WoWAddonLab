namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPetActionState(
    string Name,
    object? Texture,
    bool IsToken,
    bool IsActive,
    bool AutoCastAllowed,
    bool AutoCastEnabled,
    int? SpellId,
    bool ChecksRange,
    bool InRange)
{
    public WowActionCooldownInfo Cooldown { get; init; } = new();
}
