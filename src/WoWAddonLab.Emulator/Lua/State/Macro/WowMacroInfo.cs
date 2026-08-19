namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMacroInfo(string name, object? icon, string body)
{
    public string Name { get; set; } = name;
    public object? Icon { get; set; } = icon;
    public string Body { get; set; } = body;
    public string? ItemName { get; set; }
    public string? ItemLink { get; set; }
    public int? SpellId { get; set; }
}
