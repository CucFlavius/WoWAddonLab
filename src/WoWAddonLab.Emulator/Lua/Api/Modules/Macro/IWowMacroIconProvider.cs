using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowMacroIconProvider
{
    IReadOnlyList<string> LooseSpellIcons { get; }
    IReadOnlyList<string> LooseItemIcons { get; }
    IReadOnlyList<uint> SpellIcons { get; }
    IReadOnlyList<uint> ItemIcons { get; }
    uint? ResolveFileDataId(string icon);
}
