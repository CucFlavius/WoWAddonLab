using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowStableInfoState
{
    public IList<WowStablePetInfoState> Pets { get; } =
        new List<WowStablePetInfoState>();
    public IList<WowStablePetSpecInfoState> AvailablePetSpecs { get; } =
        new List<WowStablePetSpecInfoState>();
    public bool IsAtStableMaster { get; set; }
    public bool IsBonusPetSlotAvailable { get; set; }
    public int? LastPickedUpSlotId { get; set; }
    public WowStablePetFavoriteRequest? LastFavoriteRequest { get; set; }
    public WowStablePetSlotRequest? LastSlotRequest { get; set; }
}
