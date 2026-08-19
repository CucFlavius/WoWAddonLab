namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitChannelState(
    string Name,
    string DisplayName,
    uint TextureId,
    double StartTimeMilliseconds,
    double EndTimeMilliseconds,
    bool IsTradeSkill,
    bool NotInterruptible,
    int SpellId,
    bool IsEmpowered,
    int NumEmpowerStages,
    string CastBarId);
