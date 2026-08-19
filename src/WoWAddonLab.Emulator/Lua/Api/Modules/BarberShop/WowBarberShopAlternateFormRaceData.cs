using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopAlternateFormRaceData(
    int RaceId,
    string? Name,
    string? FileName,
    string CreateScreenIconAtlas);
