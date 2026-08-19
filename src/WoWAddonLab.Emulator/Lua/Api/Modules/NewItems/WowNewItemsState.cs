namespace WoWAddonLab.Emulator.Lua;

public sealed class WowNewItemsState
{
    private readonly HashSet<(int ContainerIndex, uint SlotIndex)> _items = [];

    public bool IsNewItem(int containerIndex, uint slotIndex) =>
        _items.Contains((containerIndex, slotIndex));

    public void MarkNewItem(int containerIndex, uint slotIndex) =>
        _items.Add((containerIndex, slotIndex));

    public void RemoveNewItem(int containerIndex, uint slotIndex) =>
        _items.Remove((containerIndex, slotIndex));

    public void ClearAll() => _items.Clear();
}
