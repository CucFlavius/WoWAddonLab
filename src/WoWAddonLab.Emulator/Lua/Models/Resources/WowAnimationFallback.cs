using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowAnimationFallback(
    ushort AnimationId,
    ushort FallbackAnimationId,
    uint Flags);
