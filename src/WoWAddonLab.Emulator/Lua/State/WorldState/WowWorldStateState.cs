namespace WoWAddonLab.Emulator.Lua;

public sealed class WowWorldStateState
{
    public IDictionary<uint, uint> Values { get; } =
        new Dictionary<uint, uint>();

    public uint GetValue(uint id) => Values.TryGetValue(id, out var value) ? value : 0;
}
