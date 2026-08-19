using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSoundPlayback
{
    public required int Handle { get; init; }
    public required WowSoundSourceKind SourceKind { get; init; }
    public int? SoundKitId { get; init; }
    public uint? FileDataId { get; init; }
    public string? FilePath { get; init; }
    public int UiSoundSubType { get; init; } = 3;
    public string Channel { get; init; } = "SFX";
    public bool ForceNoDuplicates { get; init; }
    public bool RunFinishCallback { get; init; }
    public int? OverridePriority { get; init; }
    public float ScaledVolume { get; set; } = 1;
}
