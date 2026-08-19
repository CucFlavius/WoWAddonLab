using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiMinimapState
{
    public UiMinimapState()
    {
        MaskTextureFileDataId = 186178;
        Quest = new UiMinimapBlobStyle(
            128,
            128,
            128,
            @"Interface\Minimap\UI-QuestBlobMinimap-Inside",
            @"Interface\Minimap\UI-QuestBlobMinimap-Outside",
            @"Interface\Minimap\UI-QuestBlob-MinimapRing")
        {
            OutsideSelectedTexture =
                @"Interface\Minimap\UI-QuestBlobMinimap-OutsideSelected"
        };
        Task = new UiMinimapBlobStyle(
            128,
            255,
            128,
            @"Interface\Minimap\UI-BonusObjectiveBlob-Inside",
            @"Interface\Minimap\UI-BonusObjectiveBlob-Outside",
            @"Interface\Minimap\UI-BonusObjectiveBlob-MinimapRing")
        {
            OutsideSelectedTexture =
                @"Interface\Minimap\UI-BonusObjectiveBlob-OutsideSelected"
        };
        Arch = new UiMinimapBlobStyle(
            128,
            128,
            128,
            @"Interface\Minimap\UI-ArchBlobMinimap-Inside",
            @"Interface\Minimap\UI-ArchBlobMinimap-Outside",
            @"Interface\Minimap\UI-ArchBlob-MinimapRing");
    }

    public float PingWorldX { get; set; }
    public float PingWorldY { get; set; }
    public int PingWorldMapId { get; set; }
    public bool HasPingWorldPosition { get; set; }
    public bool PingActive { get; set; }
    public double PingStartedAt { get; set; }
    public float PingElapsed { get; set; }
    public float PingDuration { get; set; }
    public int Zoom { get; set; }
    public int ZoomLevels { get; } = 6;
    public float BlipRefreshAccumulator { get; set; }
    public string? MaskTexture { get; set; }
    public uint? MaskTextureFileDataId { get; set; }
    public UiMinimapBlobStyle Arch { get; }
    public UiMinimapBlobStyle Quest { get; }
    public UiMinimapBlobStyle Task { get; }
}
