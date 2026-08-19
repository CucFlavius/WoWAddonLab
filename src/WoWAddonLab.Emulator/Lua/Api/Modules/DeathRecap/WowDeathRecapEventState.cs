using System.Collections;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDeathRecapEventState
{
    public WowDeathRecapEventState(double currentHp)
    {
        CurrentHp = currentHp;
    }

    public double CurrentHp { get; set; }
    public IDictionary<string, object?> Fields { get; } =
        new Dictionary<string, object?>(StringComparer.Ordinal);
}
