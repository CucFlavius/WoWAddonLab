using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellTargetingState
{
    public bool IsCasting { get; set; }
    public bool IsTargeting { get; set; }
    public bool HasPendingTargetingCursor { get; set; }
    public bool CanTargetGarrisonMission { get; set; }
    public bool CanTargetItem { get; set; }
    public bool CanTargetItemId { get; set; }
    public bool CanTargetQuest { get; set; }

    public IDictionary<ulong, int> GarrisonFollowerResultById { get; } =
        new Dictionary<ulong, int>();

    public IDictionary<(ulong FollowerId, uint AbilityId), int>
        GarrisonFollowerAbilityResult { get; } =
        new Dictionary<(ulong FollowerId, uint AbilityId), int>();

    public IDictionary<string, bool> CanTargetUnitByToken { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public List<WowSpellTargetRequest> TargetRequests { get; } = [];

    public void Clear()
    {
        IsTargeting = false;
        HasPendingTargetingCursor = false;
        CanTargetGarrisonMission = false;
        CanTargetItem = false;
        CanTargetItemId = false;
        CanTargetQuest = false;
        CanTargetUnitByToken.Clear();
    }
}
