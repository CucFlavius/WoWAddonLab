using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiModelEdgeGlowEffectState(
    Vector4 GlowColor,
    float GlowMultiplier,
    float FresnelCoefficient,
    bool InvertFresnel);
