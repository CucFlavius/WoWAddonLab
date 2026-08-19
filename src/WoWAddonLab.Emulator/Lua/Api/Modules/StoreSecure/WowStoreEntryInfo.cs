using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreEntryInfo(
    int ProductId,
    int GroupId,
    int BannerType,
    bool AlreadyOwned,
    WowStoreProductSharedData SharedData);
