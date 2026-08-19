using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTaxiMapState
{
    public bool TaxiSystemAvailable { get; set; } = true;
    public int? ActiveMapId { get; set; }
    public List<WowLegacyTaxiNode> Nodes { get; } = [];
    public Dictionary<int, List<WowTaxiMapAllNode>> AllNodesByMap { get; } = [];
    public Dictionary<int, List<WowTaxiMapNode>> MapNodesByMap { get; } = [];
    public HashSet<int> MapsShowingTaxiNodes { get; } = [];
    public int SetTaxiMapRequests { get; set; }
    public int CloseTaxiMapRequests { get; set; }
    public int TakeTaxiNodeRequests { get; set; }
    public int? LastTakenTaxiSlot { get; set; }
}
