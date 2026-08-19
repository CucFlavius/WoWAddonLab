using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowVignetteInfo(
    string VignetteGuid,
    string ObjectGuid,
    string? Name,
    bool IsDead,
    bool OnWorldMap,
    bool ZoneInfiniteAoi,
    bool OnMinimap,
    bool IsUnique,
    bool InFogOfWar,
    string? AtlasName,
    bool HasTooltip,
    int VignetteId,
    byte Type,
    int RewardQuestId,
    int? TooltipWidgetSet = null,
    int? IconWidgetSet = null,
    bool? AddPaddingAboveTooltipWidgets = null,
    WowVignetteMapPin? MapPin = null,
    byte? ObjectiveType = null);
