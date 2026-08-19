using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopCategorySectionInfo(
    int Id,
    string DisplayName = "",
    int? ParentCatalogShopCategoryInfoId = null,
    string? CardType = null,
    int? ScrollGridSize = null,
    bool ShouldShowRecommendationOptOutDisclaimer = false);
