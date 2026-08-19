using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalLootInfo
{
    public int ItemId { get; init; }
    public int? EncounterId { get; init; }
    public string? Name { get; init; }
    public string? ItemQuality { get; init; }
    public int? FilterType { get; init; }
    public uint? Icon { get; init; }
    public string? Slot { get; init; }
    public string? ArmorType { get; init; }
    public string? Link { get; init; }
    public bool? HandError { get; init; }
    public bool? WeaponTypeError { get; init; }
    public bool? DisplayAsPerPlayerLoot { get; init; }
    public bool? DisplayAsVeryRare { get; init; }
    public bool? DisplayAsExtremelyRare { get; init; }
    public int? DisplaySeasonId { get; init; }
}
