using System.Numerics;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using Silk.NET.OpenGL;

namespace WoWAddonLab.Assets;

public readonly record struct UiTextureMask(UiTextureState Texture, UiTextureQuad Quad);
