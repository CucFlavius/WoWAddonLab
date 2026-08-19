using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowShadowyEffectDefinition(
    uint Id,
    uint PrimaryColor,
    uint SecondaryColor,
    float Value,
    uint Flags,
    float InnerStrength,
    float OuterStrength);
