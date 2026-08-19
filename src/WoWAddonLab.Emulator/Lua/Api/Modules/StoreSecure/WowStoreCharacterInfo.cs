using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreCharacterInfo(
    string Name,
    string? ClassName,
    string? RaceName,
    int Level,
    string? ClassFileName,
    string? RaceFileName,
    string? Guid,
    string? WowAccount,
    int CurrentServer,
    int Faction,
    byte Sex,
    string? CreateScreenIconAtlas = null);
