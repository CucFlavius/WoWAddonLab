using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public enum UiTextureWrapMode : byte
{
    Clamp = 0,
    Repeat = 1,
    ClampToBlack = 2,
    ClampToBlackAdditive = 3,
    ClampToWhite = 4,
    Mirror = 5
}
