namespace WoWAddonLab.Emulator.Lua;

public sealed class WowVoiceAudioDeviceState
{
    public required string DeviceId { get; init; }
    public required string DisplayName { get; init; }
    public bool IsActive { get; set; }
    public bool IsSystemDefault { get; init; }
    public bool IsCommsDefault { get; init; }
}
