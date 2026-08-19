using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public enum UiTextureFilterMode : byte
{
    Nearest = 0,
    Linear = 1,
    Bilinear = 3,
    Trilinear = 4,
    Anisotropic = 5
}
