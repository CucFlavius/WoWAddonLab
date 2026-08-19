namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAzeriteEmpoweredItemTierInfo(
    IReadOnlyList<int> AzeritePowerIds,
    int UnlockLevel);
