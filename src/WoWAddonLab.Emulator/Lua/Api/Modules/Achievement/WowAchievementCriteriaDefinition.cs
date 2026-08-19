using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAchievementCriteriaDefinition(
    int AchievementId,
    int CriteriaId,
    string Description,
    int Type,
    bool Completed,
    long Quantity,
    long RequiredQuantity,
    string? CharacterName,
    int Flags,
    int AssetId,
    string QuantityString,
    bool Eligible = true,
    int? DurationSeconds = null,
    int? ElapsedSeconds = null);
