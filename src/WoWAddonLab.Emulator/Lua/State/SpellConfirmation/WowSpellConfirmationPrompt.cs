namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellConfirmationPrompt
{
    public int SpellId { get; set; }
    public int ConfirmType { get; set; }
    public string Text { get; set; } = string.Empty;
    public uint? ExpirationTickMilliseconds { get; set; }
    public int CurrencyId { get; set; }
    public int CurrencyCost { get; set; }
    public int DifficultyId { get; set; } = 14;
    public int DisplayItemId { get; set; }
    public int ItemContext { get; set; }
    public int TreasureContextLevel { get; set; }
}
