namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowCommentatorUnitSpellKey(
    string UnitToken,
    int SpellId)
{
    public static WowCommentatorUnitSpellKey Create(string unitToken, int spellId) =>
        new(unitToken.ToLowerInvariant(), spellId);
}
