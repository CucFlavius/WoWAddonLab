using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLootRollItemInfo(
    int? TextureFileId,
    string? Name,
    int Count,
    int Quality,
    bool BindOnPickup,
    bool CanNeed,
    bool CanGreed,
    bool CanDisenchant,
    int ReasonNeed,
    int ReasonGreed,
    int ReasonDisenchant,
    int DisenchantSkillRequired,
    bool CanTransmog);
