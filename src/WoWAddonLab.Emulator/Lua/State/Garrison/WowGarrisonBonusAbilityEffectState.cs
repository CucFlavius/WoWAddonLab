namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonBonusAbilityEffectState(
    int BonusAbilityId,
    string TextureKit,
    float PosX,
    float PosY,
    double StartTime,
    int Duration,
    float Radius,
    string? Name,
    string? Description,
    int? Icon);
