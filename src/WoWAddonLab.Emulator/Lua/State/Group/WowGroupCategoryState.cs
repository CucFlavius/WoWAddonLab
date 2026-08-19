namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGroupCategoryState
{
    public bool IsInRaid { get; set; }
    public int SubgroupMemberCount { get; set; }
    public int GroupMemberCount { get; set; }
    public bool EveryoneIsAssistant { get; set; }
    public int TankCount { get; set; }
    public int HealerCount { get; set; }
    public int DamagerCount { get; set; }
    public int NoRoleCount { get; set; }
    public Dictionary<string, int> ClassCounts { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsPresent =>
        IsInRaid ||
        SubgroupMemberCount > 0 ||
        GroupMemberCount > 0;
}
