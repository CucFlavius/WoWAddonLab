using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiModelDissolveEffectState(
    WowDissolveEffectDefinition Definition,
    float Strength);
