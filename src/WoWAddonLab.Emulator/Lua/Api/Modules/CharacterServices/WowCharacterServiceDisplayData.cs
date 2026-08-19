using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCharacterServiceDisplayData(
    int BoostType,
    int VasType,
    int Level,
    int Expansion,
    string TooltipTitle,
    string TooltipDescription,
    string FlowTitle,
    int Flags,
    int ProfessionLevel,
    WowCharacterServicePopupInfo PopupInfo,
    uint IconFileDataId,
    string? IconTextureKit);
