using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRemixArtifactItemInfoState(
    int ItemId,
    int? AltItemId,
    int ArtifactAppearanceId,
    int AppearanceModId,
    int? ItemAppearanceId,
    int? AltItemAppearanceId,
    bool AltOnTop);
