using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowSpellVisualKitEffectDefinition(
    int EffectId,
    uint Effect,
    uint EffectType);
