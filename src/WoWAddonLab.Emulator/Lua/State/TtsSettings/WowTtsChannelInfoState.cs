namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowTtsChannelInfoState(
    string Name,
    string Shortcut,
    int LocalId,
    uint InstanceId,
    int ZoneChannelId,
    int ChannelType)
{
    public string Key =>
        ChannelType == 1
            ? $"1:{LocalId}"
            : $"{ChannelType}:{Name}";
}
