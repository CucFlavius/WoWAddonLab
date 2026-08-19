namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitDefinitionInfoState(
    int? SpellId,
    string? OverrideName,
    string? OverrideSubtext,
    string? OverrideDescription,
    int? OverrideIcon,
    int? OverriddenSpellId,
    int? SubType);
