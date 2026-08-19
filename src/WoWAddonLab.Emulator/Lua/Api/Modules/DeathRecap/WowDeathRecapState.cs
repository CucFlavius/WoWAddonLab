using System.Collections;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDeathRecapState
{
    public IDictionary<int, WowDeathRecapRecordState> RecapsById { get; } =
        new Dictionary<int, WowDeathRecapRecordState>();

    public int? MostRecentRecapId { get; set; }
    public string EmptyRecapText { get; set; } = string.Empty;
}
