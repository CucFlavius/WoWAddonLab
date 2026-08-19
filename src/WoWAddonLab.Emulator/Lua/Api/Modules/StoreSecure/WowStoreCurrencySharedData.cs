using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreCurrencySharedData(
    int RegionId,
    string? FormatShort = null,
    string? FormatLong = null,
    string? LicenseAcceptText = null,
    bool? RequireLicenseAccept = null,
    bool? BrowseHasStar = null,
    bool? HideBrowseNotice = null,
    bool? HideConfirmationBrowseNotice = null);
