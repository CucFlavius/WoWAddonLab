namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonAutoCombatStatsState(
    int CurrentHealth,
    int MaxHealth,
    int Attack,
    int HealingTimestamp,
    int HealCost,
    int MinutesHealingRemaining);
