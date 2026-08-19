using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowEncounterJournalProvider
{
    IReadOnlyList<WowEncounterJournalTier> Tiers { get; }
    IReadOnlyList<WowEncounterJournalInstance> GetInstances(int tierId, bool raid);
    bool TryGetInstance(int instanceId, out WowEncounterJournalInstance instance);
}
