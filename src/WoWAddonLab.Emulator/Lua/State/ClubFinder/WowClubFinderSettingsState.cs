namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubFinderSettingsState
{
    public bool PlayStyleDungeon { get; set; }
    public bool PlayStyleRaids { get; set; }
    public bool PlayStylePvp { get; set; }
    public bool PlayStyleRp { get; set; }
    public bool PlayStyleSocial { get; set; }
    public bool RoleTank { get; set; }
    public bool RoleHealer { get; set; }
    public bool RoleDps { get; set; }
    public bool SizeSmall { get; set; }
    public bool SizeMedium { get; set; }
    public bool SizeLarge { get; set; }
    public bool MaxLevelOnly { get; set; }
    public bool EnableListing { get; set; }
    public bool SortRelevance { get; set; }
    public bool SortMembers { get; set; }
    public bool SortNewest { get; set; }
    public bool AutoAccept { get; set; }
    public bool CrossFaction { get; set; }
}
