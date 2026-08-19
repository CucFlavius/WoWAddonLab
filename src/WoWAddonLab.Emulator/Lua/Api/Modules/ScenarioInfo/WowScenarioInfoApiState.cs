using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowScenarioInfoApiState
{
    public IDictionary<int, IReadOnlyList<WowScenarioIconInfoState>> IconsByMapId
        { get; } =
        new Dictionary<int, IReadOnlyList<WowScenarioIconInfoState>>();
}
