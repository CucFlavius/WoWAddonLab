using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreProductGroupInfo(
    string? GroupName,
    int Texture,
    int DisplayType,
    uint Flags,
    string? DisabledTooltip,
    int ParentProductGroupId);
