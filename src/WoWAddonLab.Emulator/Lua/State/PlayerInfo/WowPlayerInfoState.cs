namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerInfoState
{
    public bool CanUseMountEquipment { get; set; }
    public string MountEquipmentError { get; set; } = string.Empty;
    public bool HasAlternateForm { get; set; }
    public bool IsAlternateForm { get; set; }
    public uint DisplayId { get; set; }
    public uint NativeDisplayId { get; set; }
    public bool IsGliding { get; set; }
    public bool CanGlide { get; set; }
    public float GlideValue { get; set; }
    public bool HasAccountInventoryLock { get; set; }
    public bool IsDisplayRaceNative { get; set; } = true;
    public bool IsMirrorImage { get; set; }
    public bool IsPlayerInRpe { get; set; }
    public bool IsPlayerNpeRestricted { get; set; }
    public bool IsTradingPostAvailable { get; set; }
    public bool IsTravelersLogAvailable { get; set; }
    public bool IsTutorialsTabAvailable { get; set; } = true;
}
