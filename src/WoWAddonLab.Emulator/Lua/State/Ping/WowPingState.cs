namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPingState
{
    public WowPingCooldownInfoState CooldownInfo { get; set; } = new(0, 0);
    public List<WowPingDefaultOptionState> DefaultOptions { get; } = [];
    public Dictionary<string, byte> ContextualTypeByTarget { get; } =
        new(StringComparer.Ordinal);
    public Dictionary<byte, string?> TextureKitByType { get; } = [];
    public bool IsListenerDown { get; set; }
    public List<WowPingRequestState> Requests { get; } = [];
}
