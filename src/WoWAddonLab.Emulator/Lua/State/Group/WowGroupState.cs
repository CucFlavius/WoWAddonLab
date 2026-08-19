namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGroupState
{
    public WowGroupCategoryState Home { get; } = new();
    public WowGroupCategoryState Instance { get; } = new();

    public bool IsInRaid
    {
        get => Home.IsInRaid;
        set => Home.IsInRaid = value;
    }

    public int SubgroupMemberCount
    {
        get => Home.SubgroupMemberCount;
        set => Home.SubgroupMemberCount = value;
    }

    public int GroupMemberCount
    {
        get => Home.GroupMemberCount;
        set => Home.GroupMemberCount = value;
    }

    public bool EveryoneIsAssistant
    {
        get => Home.EveryoneIsAssistant;
        set => Home.EveryoneIsAssistant = value;
    }

    public int TankCount
    {
        get => Home.TankCount;
        set => Home.TankCount = value;
    }

    public int HealerCount
    {
        get => Home.HealerCount;
        set => Home.HealerCount = value;
    }

    public int DamagerCount
    {
        get => Home.DamagerCount;
        set => Home.DamagerCount = value;
    }

    public int NoRoleCount
    {
        get => Home.NoRoleCount;
        set => Home.NoRoleCount = value;
    }

    public WowGroupCategoryState? Resolve(int? category = null) =>
        category switch
        {
            (int)WowPartyCategory.Home => Home.IsPresent ? Home : null,
            (int)WowPartyCategory.Instance => Instance.IsPresent ? Instance : null,
            _ => Instance.IsPresent ? Instance : Home.IsPresent ? Home : null
        };

    public WowPartyCategory? ResolveCategory(int? category = null) =>
        category switch
        {
            (int)WowPartyCategory.Home when Home.IsPresent => WowPartyCategory.Home,
            (int)WowPartyCategory.Instance when Instance.IsPresent => WowPartyCategory.Instance,
            _ when Instance.IsPresent => WowPartyCategory.Instance,
            _ when Home.IsPresent => WowPartyCategory.Home,
            _ => null
        };
}
