using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContributionAppearance(
    string? StateName,
    WowContributionColor StateColor,
    string? TooltipLine,
    bool TooltipUseTimeRemaining,
    string? StatusBarAtlas,
    string? BorderAtlas,
    string? BannerAtlas);
