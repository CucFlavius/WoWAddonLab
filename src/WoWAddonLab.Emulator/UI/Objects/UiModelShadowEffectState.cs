using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiModelShadowEffectState(
    Vector4 PrimaryColor,
    Vector4 SecondaryColor);
