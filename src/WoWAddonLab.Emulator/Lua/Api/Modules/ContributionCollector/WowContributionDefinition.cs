using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContributionDefinition(
    string Name = "",
    string Description = "",
    int OrderIndex = 0,
    int? RewardQuestId = null,
    WowContributionCurrency? RequiredCurrency = null,
    WowContributionItem? RequiredItem = null);
