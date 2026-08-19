using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowAzeriteEssenceProvider
{
    IReadOnlyList<WowAzeriteMilestoneDefinition> Milestones { get; }
}
