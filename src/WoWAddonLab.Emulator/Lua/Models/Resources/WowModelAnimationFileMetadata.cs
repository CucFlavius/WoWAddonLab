using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowModelAnimationFileMetadata(
    ushort AnimationId,
    ushort VariationIndex,
    uint FileDataId);
