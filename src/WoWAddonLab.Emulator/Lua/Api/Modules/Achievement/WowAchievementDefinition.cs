using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAchievementDefinition(
    int Id,
    int CategoryId,
    string Name,
    string Description,
    string RewardText,
    int Points,
    int Flags,
    uint IconFileDataId,
    bool Completed = false,
    int? CompletionMonth = null,
    int? CompletionDay = null,
    int? CompletionYear = null,
    bool WasEarnedByMe = false,
    string? EarnedBy = null,
    int? RewardItemId = null,
    int? PreviousAchievementId = null,
    IReadOnlyList<int>? SupercedingAchievementIds = null,
    bool Eligible = true);
