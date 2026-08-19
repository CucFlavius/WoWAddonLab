using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowQuestInteractionState
{
    public int CurrentQuestId { get; set; }
    public int CloseRequestCount { get; internal set; }
    public List<int> ClosedQuestIds { get; } = [];
}
