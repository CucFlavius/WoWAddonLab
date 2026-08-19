using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowEdgeGlowEffectDefinition(
    uint Id,
    float FresnelCoefficient,
    Vector4 GlowColor,
    float GlowMultiplier,
    uint Flags);
