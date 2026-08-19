namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCameraMovementState(
    WowCameraMovementDirection Direction,
    float Speed,
    float TimeoutSeconds,
    bool Immediate);
