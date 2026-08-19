using System.Collections;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDeathRecapRecordState
{
    public WowDeathRecapRecordState(int recapId)
    {
        RecapId = recapId;
    }

    public int RecapId { get; }
    public string Link { get; set; } = string.Empty;
    public double MaxHealth { get; set; }
    public IList<WowDeathRecapEventState> Events { get; } =
        new List<WowDeathRecapEventState>();
}
