namespace WoWAddonLab.Emulator.Lua;

public enum WowDamageMeterOverrideType
{
    Ignore = 0,
    AllowFriendlyFire = 1,
    RedirectSourceToOwner = 2,
    RedirectSourceToAuraCaster = 3,
    IgnoreForAbsorbSpell = 4
}
