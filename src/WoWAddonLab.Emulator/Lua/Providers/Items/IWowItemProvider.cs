namespace WoWAddonLab.Emulator.Lua;

public interface IWowItemProvider
{
    IReadOnlyDictionary<int, WowItemData> Items { get; }
}
