using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowAccountStoreProvider
{
    IReadOnlyList<WowAccountStoreCategoryDefinition> Categories { get; }
    IReadOnlyList<WowAccountStoreItemDefinition> Items { get; }
}
