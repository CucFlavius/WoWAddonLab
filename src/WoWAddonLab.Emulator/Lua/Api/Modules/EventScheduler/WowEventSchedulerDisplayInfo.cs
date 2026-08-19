using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEventSchedulerDisplayInfo(
    bool HideTimeLeft,
    bool HideDescription,
    string? OverrideAtlas = null,
    int? OverrideTooltipWidgetSetId = null);
