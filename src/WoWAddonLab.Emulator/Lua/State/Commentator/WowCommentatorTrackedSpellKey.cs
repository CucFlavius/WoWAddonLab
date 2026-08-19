namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowCommentatorTrackedSpellKey(
    string UnitToken,
    WowTrackedSpellCategory Category)
{
    public static WowCommentatorTrackedSpellKey Create(
        string unitToken,
        WowTrackedSpellCategory category) =>
        new(unitToken.ToLowerInvariant(), category);
}
