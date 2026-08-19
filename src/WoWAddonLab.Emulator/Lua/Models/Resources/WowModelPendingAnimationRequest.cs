using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelPendingAnimationRequest(
    ushort RequestedAnimationId,
    int RequestedVariation,
    float PlaybackSpeed,
    int TimeOffsetMilliseconds,
    int BlendOperation,
    ushort ResolvedAnimationId,
    int SelectedSequenceIndex,
    int ResolvedSequenceIndex);
