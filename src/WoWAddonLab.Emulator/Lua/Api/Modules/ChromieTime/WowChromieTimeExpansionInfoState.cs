using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowChromieTimeExpansionInfoState(
    int Id,
    string? Name,
    string? Description,
    string? MapAtlas,
    string? PreviewAtlas,
    bool Completed,
    bool AlreadyOn,
    bool Recommended,
    int SortPriority);
