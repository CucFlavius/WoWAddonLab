namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapLevels(
    int PlayerMinLevel,
    int PlayerMaxLevel,
    int PetMinLevel,
    int PetMaxLevel);
