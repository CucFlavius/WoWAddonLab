using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowSpellVisualKitApplication(
    WowSpellVisualKitDefinition Definition,
    bool OneShot);
