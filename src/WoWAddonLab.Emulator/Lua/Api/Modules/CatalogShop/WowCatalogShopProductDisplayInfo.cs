using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCatalogShopProductDisplayInfo
{
    public int DefaultPreviewModelSceneId { get; init; }
    public int DefaultCardModelSceneId { get; init; }
    public int DefaultWideCardModelSceneId { get; init; }
    public int ItemId { get; init; }
    public int? OverridePreviewModelSceneId { get; init; }
    public int? OverrideCardModelSceneId { get; init; }
    public int? OverrideWideCardModelSceneId { get; init; }
    public IList<int> CreatureDisplayInfoIds { get; } = [];
    public IList<int> SpellVisualIds { get; } = [];
    public int? MainHandItemModifiedAppearanceId { get; init; }
    public int? OffHandItemModifiedAppearanceId { get; init; }
    public IList<int> ItemModifiedAppearanceIds { get; } = [];
    public int? IconFileDataId { get; init; }
    public string? IconTextureKit { get; init; }
    public string? ProductType { get; init; }
    public string? ItemDescription { get; init; }
    public bool HasUnknownLicense { get; init; }
    public string? ProductPmtUrl { get; init; }
    public IList<string> AdditionalProductPmtUrls { get; } = [];
    public string? OtherProductImageAtlasName { get; init; }
    public string? OtherProductGameTitleBaseTag { get; init; }
    public string? OtherProductGameType { get; init; }
    public int? CustomLoopingSoundStart { get; init; }
    public int? CustomLoopingSoundMiddle { get; init; }
    public int? CustomLoopingSoundEnd { get; init; }
    public string? SpecialActorId1 { get; init; }
    public string? SpecialActorId2 { get; init; }
    public string? SpecialActorId3 { get; init; }
    public string? SpecialActorId4 { get; init; }
    public string? SpecialActorId5 { get; init; }
    public int? GameFlavorId { get; init; }
    public int? DecorFileDataId { get; init; }
    public int? Quantity { get; init; }
    public string? HouseTextureAtlas { get; init; }
}
