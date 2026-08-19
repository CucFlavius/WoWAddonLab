namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorUnitDataState(
    int HealthMax = 0,
    int Health = 0,
    int AbsorbTotal = 0,
    bool IsDeadOrGhost = false,
    bool IsFeignDeath = false,
    string PowerTypeToken = "",
    int Power = 0,
    int PowerMax = 0);
