using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBarberShopState
{
    public IReadOnlyList<WowBarberShopCustomizationCategory>?
        AvailableCustomizations { get; set; }
    public WowBarberShopCharacterData? CurrentCharacterData { get; set; }
    public bool ApplyCustomizationChoicesResult { get; set; }
    public int CurrentCameraZoom { get; set; }
    public int CurrentCost { get; set; }
    public int? ViewingChrModelId { get; set; }
    public bool HasAlteredForm { get; set; }
    public bool HasAnyChanges { get; set; }
    public int CustomizationFeatureMask { get; set; }
    public bool IsOpen { get; set; }
    public bool IsViewingAlteredForm { get; set; }
    public byte SelectedSex { get; set; }
    public bool ModelDressed { get; set; }
    public double InitialCameraRotationDegrees { get; set; }
    public double CameraRotationDegrees { get; set; }
    public float CameraDistanceOffset { get; set; }
    public bool HasCustomCameraZoom { get; set; }
    public int? ViewingSpellShapeshiftFormId { get; set; }
    public int ViewingShapeshiftFormId { get; set; }

    public IDictionary<int, int> SelectedChoices { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> PreviewChoices { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> SavedPreviewChoices { get; } =
        new Dictionary<int, int>();
    public ISet<int> SeenChoiceIds { get; } = new HashSet<int>();
    public ISet<int> SeenOptionIds { get; } = new HashSet<int>();

    public int ApplyCustomizationChoicesRequests { get; internal set; }
    public int CancelRequests { get; internal set; }
    public int ClearPreviewChoicesRequests { get; internal set; }
    public bool? LastClearSavedChoices { get; internal set; }
    public int? LastSeenChoiceId { get; internal set; }
    public int? LastSeenOptionId { get; internal set; }
    public int PreviewCustomizationChoiceRequests { get; internal set; }
    public WowBarberShopOptionChoice? LastPreviewCustomizationChoice
    {
        get;
        internal set;
    }
    public int RandomizeCustomizationChoicesRequests { get; internal set; }
    public int ResetCameraRotationRequests { get; internal set; }
    public int ResetCustomizationChoicesRequests { get; internal set; }
    public int RotateCameraRequests { get; internal set; }
    public double? LastCameraRotationDifferenceDegrees { get; internal set; }
    public int SaveSeenChoicesRequests { get; internal set; }
    public int SetCameraDistanceOffsetRequests { get; internal set; }
    public int SetCameraZoomLevelRequests { get; internal set; }
    public bool? LastKeepCustomZoom { get; internal set; }
    public int SetCustomizationChoiceRequests { get; internal set; }
    public WowBarberShopOptionChoice? LastCustomizationChoice
    {
        get;
        internal set;
    }
    public int SetModelDressStateRequests { get; internal set; }
    public int SetSelectedSexRequests { get; internal set; }
    public int SetViewingAlteredFormRequests { get; internal set; }
    public int SetViewingChrModelRequests { get; internal set; }
    public int SetViewingShapeshiftFormRequests { get; internal set; }
    public int ZoomCameraRequests { get; internal set; }
    public int? LastCameraZoomAmount { get; internal set; }
}
