using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellVisualKitDefinition(
    uint Id,
    IReadOnlyList<WowSpellVisualKitEffectDefinition> Effects);
