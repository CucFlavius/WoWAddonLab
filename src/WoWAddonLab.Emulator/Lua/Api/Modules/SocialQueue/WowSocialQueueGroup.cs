using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialQueueGroup
{
    public string Guid { get; init; } = string.Empty;
    public bool CanJoin { get; init; }
    public int NumQueues { get; init; }
    public bool NeedTank { get; init; }
    public bool NeedHealer { get; init; }
    public bool NeedDamage { get; init; }
    public bool IsSoloQueueParty { get; init; }
    public bool QuestSessionActive { get; init; }
    public string? LeaderGuid { get; init; }
    public IList<WowSocialQueuePlayerInfo> Members { get; } = [];
    public IList<WowSocialQueueGroupQueueInfo> Queues { get; } = [];
}
