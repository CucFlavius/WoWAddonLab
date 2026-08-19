namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindTreeData(
    bool Editable,
    IReadOnlyList<WowSoulbindNodeData> Nodes)
{
    public static WowSoulbindTreeData Empty { get; } =
        new(false, []);
}
