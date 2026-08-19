using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowRemixArtifactUiState
{
    public IDictionary<uint, WowRemixArtifactAppearanceInfoState>
        AppearanceInfoById { get; } =
        new Dictionary<uint, WowRemixArtifactAppearanceInfoState>();

    public IDictionary<uint, int>
        TraitTreeIdsByZeroBasedInventorySlot { get; } =
        new Dictionary<uint, int>();

    public WowRemixArtifactArtInfoState? ArtifactArtInfo { get; set; }
    public WowRemixArtifactItemInfoState? ArtifactItemInfo { get; set; }
    public int? CurrentArtifactItemId { get; set; }
    public int? SelectedItemSpecIndex { get; set; }
    public int? EquippedItemSpecIndex { get; set; }
    public int? CurrentTraitTreeId { get; set; }

    public int ClearRequests { get; internal set; }
}
