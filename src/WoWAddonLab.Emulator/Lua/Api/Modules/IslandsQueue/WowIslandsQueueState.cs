using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowIslandsQueueState
{
    public IList<WowIslandDifficultyInfoState> Difficulties { get; } =
        new List<WowIslandDifficultyInfoState>();
    public byte WeeklyQuestEligibilityFlags { get; set; }

    public int CloseScreenRequests { get; internal set; }
    public int QueueRequests { get; internal set; }
    public int SuccessfulQueueRequests { get; internal set; }
    public int? LastRequestedDifficultyId { get; internal set; }
    public int? LastQueuedDifficultyId { get; internal set; }
    public int PreloadRewardDataRequests { get; internal set; }
    public int? LastPreloadRewardQuestId { get; internal set; }
}
