using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventTextureState(
    string Title,
    int IconTexture,
    byte ExpansionLevel,
    int? DifficultyId,
    int? MapId,
    bool? IsLfr,
    int EventTextureId);
