namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellPowerCostInfo(
    byte Type,
    string Name,
    int Cost,
    int MinCost,
    int CostPercent,
    int CostPerSecond,
    int RequiredAuraId,
    bool HasRequiredAura);
