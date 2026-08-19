namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUnitPowerBarInfoState(
    int Id,
    int BarType,
    double MinimumPower,
    double StartInset,
    double EndInset,
    bool Smooth,
    bool HideFromOthers,
    bool ShowOnRaid,
    bool OpaqueSpark,
    bool OpaqueFlash,
    bool AnchorTop,
    bool ForcePercentage,
    bool SparkUnderFrame,
    bool FlashAtMinimumPower,
    bool FractionalCounter,
    bool AnimateNumbers,
    bool AttachTooltipToBar);
