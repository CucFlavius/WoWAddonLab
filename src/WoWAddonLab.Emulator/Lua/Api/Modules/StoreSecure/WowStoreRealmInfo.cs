using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreRealmInfo(
    int VirtualRealmAddress,
    string RealmName,
    int CharacterCount,
    bool? Pvp = null,
    bool? Rp = null);
