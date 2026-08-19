namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattlefieldVehicleInfoState(
    float X,
    float Y,
    string Name,
    bool IsOccupied,
    string Atlas,
    int TextureWidth,
    int TextureHeight,
    float Facing,
    bool IsPlayer,
    bool IsAlive,
    bool ShouldDrawBelowPlayerBlips);
