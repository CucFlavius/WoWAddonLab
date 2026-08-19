using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowDissolveEffectDefinition(
    uint Id,
    float Ramp,
    float StartValue,
    float EndValue,
    float FadeInTime,
    float FadeOutTime,
    float Duration,
    byte AttachId,
    byte ProjectionType,
    WowTextureBlendSetDefinition TextureBlendSet,
    float Scale,
    uint Flags,
    uint CurveId,
    uint Priority,
    float FresnelIntensity,
    Vector4 Fields14Through17,
    float Field18,
    int Field19,
    int Field20);
