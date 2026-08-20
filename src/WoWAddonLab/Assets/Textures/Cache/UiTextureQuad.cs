using System.Numerics;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using Silk.NET.OpenGL;

namespace WoWAddonLab.Assets;

public readonly record struct UiTextureQuad(
    Vector2 UpperLeft,
    Vector2 LowerLeft,
    Vector2 UpperRight,
    Vector2 LowerRight);
