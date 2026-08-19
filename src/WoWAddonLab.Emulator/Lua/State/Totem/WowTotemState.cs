namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTotemState
{
    private readonly Dictionary<int, WowTotemSlotState> _slots = [];

    public int SlotCount => 5;
    public int? TargetedSlot { get; set; }
    public IReadOnlyDictionary<int, WowTotemSlotState> Slots => _slots;

    public WowTotemSlotState Set(int slot)
    {
        if (!_slots.TryGetValue(slot, out var value))
        {
            value = new WowTotemSlotState { Slot = slot };
            _slots.Add(slot, value);
        }
        return value;
    }

    public bool Remove(int slot) => _slots.Remove(slot);
    public WowTotemSlotState? Find(int slot) => _slots.GetValueOrDefault(slot);
}
