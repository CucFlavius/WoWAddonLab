using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowCharacterServiceProvider
{
    bool TryGetDisplayData(int boostType, out WowCharacterServiceDisplayData data);
}
