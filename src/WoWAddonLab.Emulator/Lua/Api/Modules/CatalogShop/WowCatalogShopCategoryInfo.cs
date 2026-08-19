using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopCategoryInfo(
    int Id,
    string DisplayName = "",
    string IconTexture = "",
    string LinkTag = "",
    bool IsDisabled = false,
    bool ShowPersistentRefundButton = false);
