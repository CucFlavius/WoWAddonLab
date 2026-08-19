namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitCastState(
    string Name,
    string DisplayName,
    uint TextureId,
    double StartTimeMilliseconds,
    double EndTimeMilliseconds,
    bool IsTradeSkill,
    string CastId,
    bool NotInterruptible,
    int SpellId,
    string CastBarId,
    double DelayTimeMilliseconds);
