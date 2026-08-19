using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowStoreGuildMasterInfo
{
    public string GuildName { get; init; } = "";
    public IList<WowStoreGuildMemberInfo> GuildMemberInfos { get; } = [];
}
