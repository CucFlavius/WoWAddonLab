using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public enum UiModelRenderEffectKind
{
    None = 0,
    Shadow = 2,
    Desaturation = 4,
    Dissolve = 5,
    EdgeGlow = 6,
    GradientMask = 9
}
