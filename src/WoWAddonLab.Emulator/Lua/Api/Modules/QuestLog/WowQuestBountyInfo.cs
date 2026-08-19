using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowQuestBountyInfo(
    int QuestId,
    int FactionId,
    int Icon,
    int NumObjectives,
    string? TurninRequirementText = null);
