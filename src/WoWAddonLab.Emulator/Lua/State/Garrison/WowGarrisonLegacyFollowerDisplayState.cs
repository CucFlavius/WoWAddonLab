namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonLegacyFollowerDisplayState(
    int Id,
    float FollowerPageScale,
    bool? ShowWeapon = null);
