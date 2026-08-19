namespace WoWAddonLab.Emulator.Lua;

public sealed class WowHousingState
{
    public WowHousingState()
    {
        CatalogCategories[18] = new WowHousingCatalogCategoryState(
            18,
            0,
            "All",
            null,
            [],
            false);
    }

    public bool CanEditCharter { get; set; }
    public bool HasHousingExpansionAccess { get; set; }
    public bool IsHousingMarketCartFullRemoveEnabled { get; set; }
    public bool IsHousingMarketEnabled { get; set; }
    public bool IsHousingMarketShopEnabled { get; set; }
    public bool IsHousingServiceEnabled { get; set; } = true;
    public bool IsInsideHouse { get; set; }
    public bool IsInsideHouseOrPlot { get; set; }
    public bool IsInsideOwnHouse { get; set; }
    public bool IsInsidePlot { get; set; }
    public bool IsOnNeighborhoodMap { get; set; }
    public int CurrentHouseRefundAmount { get; set; }
    public uint HousingAccessFlags { get; set; }
    public int MaxHouseLevel { get; set; }
    public string? CurrentNeighborhoodGuid { get; set; }
    public WowHousingHouseInfoState? CurrentHouseInfo { get; set; }
    public WowHousingActionCooldownInfoState? VisitCooldownInfo { get; set; }
    public Dictionary<int, int> HouseFavorByLevel { get; } = [];
    public Dictionary<byte, uint> ReportScreenshotReasonByPlotIndex { get; } = [];
    public Dictionary<string, bool> FactionMatchesNeighborhoodByGuid { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<string, string?> NeighborhoodTextureSuffixByGuid { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<string, int?> UiMapIdByNeighborhoodGuid { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<string, bool> BNetFriendSearchResultByName { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<int, bool> BNetFriendSearchResultById { get; } = [];
    public List<WowHousingRequestState> Requests { get; } = [];
    public byte ActiveHouseEditorMode { get; set; }
    public byte HouseEditorAvailability { get; set; } = 62;
    public byte ActivateHouseEditorModeResult { get; set; } = 62;
    public byte EnterHouseEditorResult { get; set; } = 62;
    public Dictionary<byte, byte> HouseEditorModeAvailability { get; } = [];
    public HashSet<byte> ActiveHouseEditorModes { get; } = [];
    public bool IsHouseEditorActive { get; set; }
    public bool IsHouseEditorStatusAvailable { get; set; }
    public bool IsInHousingInspectMode { get; set; }
    public string? HoveredDecorGuid { get; set; }
    public bool IsDecorGridVisible { get; set; }
    public bool IsDecorPreviewState { get; set; }
    public bool HasMaxPlacementBudget { get; set; }
    public bool IsDecorSelected { get; set; }
    public bool IsHouseExteriorDoorHovered { get; set; }
    public bool IsHouseExteriorHovered { get; set; }
    public bool IsHoveringDecor { get; set; }
    public int MaxPlacementBudget { get; set; }
    public int PlacedDecorCount { get; set; }
    public int PreviewDecorCount { get; set; }
    public int SpentPlacementBudget { get; set; }
    public List<WowHousingPlacedDecorState> PlacedDecor { get; } = [];
    public Dictionary<int, string> DecorHyperlinks { get; } = [];
    public Dictionary<int, int> DecorIcons { get; } = [];
    public Dictionary<int, string> DecorNames { get; } = [];
    public Dictionary<int, WowHousingCatalogCategoryState> CatalogCategories { get; } = [];
    public Dictionary<string, WowHousingDecorInstanceInfoState> DecorInfoByGuid
        { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, WowHousingDecorDebugInfoState> DecorDebugInfoByGuid
        { get; } = new(StringComparer.Ordinal);
    public WowHousingDecorInstanceInfoState? HoveredDecorInfo { get; set; }
    public WowHousingDecorInstanceInfoState? SelectedDecorInfo { get; set; }
    public WowHousingDecorDebugInfoState? HoveredDecorDebugInfo { get; set; }
    public WowHousingDecorDebugInfoState? SelectedDecorDebugInfo { get; set; }
    public HashSet<string> HoveredPlacedDecorGuids { get; } =
        new(StringComparer.Ordinal);
    public HashSet<string> SelectedPlacedDecorGuids { get; } =
        new(StringComparer.Ordinal);
    public string? TrackedHouseGuid { get; set; }
}
