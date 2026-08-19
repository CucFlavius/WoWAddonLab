using ImGuiNET;

namespace WoWAddonLab.Rendering;

public sealed record RenderFont(
    float PixelSize,
    float GlyphSize,
    ImFontPtr Font);
