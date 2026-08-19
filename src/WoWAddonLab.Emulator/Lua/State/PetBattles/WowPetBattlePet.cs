namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPetBattlePet(
    int BreedQuality,
    int? IconFileId,
    string CustomName,
    string SpeciesName,
    int Health = 100,
    int MaxHealth = 100,
    int Xp = 0,
    int MaxXp = 50);
