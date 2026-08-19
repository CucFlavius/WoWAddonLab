using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopCharacterData(
    string? Name,
    string? FileName,
    WowBarberShopAlternateFormRaceData? AlternateFormRaceData,
    string CreateScreenIconAtlas,
    byte Sex);
