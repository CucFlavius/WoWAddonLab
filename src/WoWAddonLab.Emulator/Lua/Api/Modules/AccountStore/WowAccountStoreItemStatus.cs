using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public enum WowAccountStoreItemStatus
{
    Unowned = 1,
    Refundable = 2,
    Owned = 3
}
