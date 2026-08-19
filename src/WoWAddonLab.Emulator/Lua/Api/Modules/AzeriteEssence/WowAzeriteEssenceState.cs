using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAzeriteEssenceState
{
    public bool CanOpenUi { get; set; }
    public bool IsAtForge { get; set; }
    public int HeartLevel { get; set; }
    public int? PendingActivationEssenceId { get; set; }
    public int? NumUnlockedEssencesOverride { get; set; }
    public IList<WowAzeriteEssenceInfoState> Essences { get; } =
        new List<WowAzeriteEssenceInfoState>();
    public ISet<int> UnlockedEssenceIds { get; } = new HashSet<int>();
    public ISet<int> UnlockedMilestoneIds { get; } = new HashSet<int>();
    public ISet<int> UnlockableMilestoneIds { get; } = new HashSet<int>();
    public ISet<(int EssenceId, int MilestoneId)> ActivatablePairs { get; } =
        new HashSet<(int, int)>();
    public ISet<int> DeactivatableMilestoneIds { get; } = new HashSet<int>();
    public IDictionary<int, int> ActiveEssenceByMilestoneId { get; } =
        new Dictionary<int, int>();
    public IList<WowAzeriteEssenceActivationRequest> ActivationRequests
        { get; } = new List<WowAzeriteEssenceActivationRequest>();
    public IList<int> UnlockMilestoneRequests { get; } = new List<int>();
}
