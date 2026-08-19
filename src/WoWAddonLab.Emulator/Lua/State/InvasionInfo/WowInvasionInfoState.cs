namespace WoWAddonLab.Emulator.Lua;

public sealed class WowInvasionInfoState
{
    public IDictionary<int, WowInvasionInfo> InvasionsById { get; } =
        new Dictionary<int, WowInvasionInfo>();

    public IDictionary<int, int> InvasionIdsByUiMapId { get; } =
        new Dictionary<int, int>();
}
