using ImGuiNET;

namespace WoWAddonLab.Rendering;

public sealed class FontSet
{
    private readonly IReadOnlyList<RenderFont> _fonts;

    public FontSet(IEnumerable<RenderFont> fonts)
    {
        _fonts = fonts.OrderBy(value => value.PixelSize).ToArray();
    }

    public RenderFont Select(float pixelSize)
    {
        if (_fonts.Count == 0)
            return new RenderFont(
                ImGui.GetFontSize(),
                ImGui.GetFontSize(),
                ImGui.GetFont());
        return _fonts.MinBy(value => Math.Abs(Math.Log(value.PixelSize / Math.Max(1, pixelSize))))!;
    }
}
