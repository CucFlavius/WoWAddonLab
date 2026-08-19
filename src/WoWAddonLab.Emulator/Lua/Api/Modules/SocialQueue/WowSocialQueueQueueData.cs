using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialQueueQueueData
{
    public string? QueueType { get; init; }
    public IReadOnlyList<int>? LfgIds { get; init; }
    public int? LfgListId { get; init; }
    public int? ActivityId { get; init; }
    public string? BattlefieldType { get; init; }
    public int? ListId { get; init; }
    public string? MapName { get; init; }
    public bool? Rated { get; init; }
    public bool? IsBrawl { get; init; }
    public int? TeamSize { get; init; }
}
