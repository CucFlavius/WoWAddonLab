using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public static class UiFrameAlphaGradient
{
    public static (UiObject Owner, Vector2 LeadingEdge, Vector2 TrailingEdge)?
        Resolve(UiSystem ui, UiObject value)
    {
        for (UiObject? current = value;
             current is not null;
             current = current.ParentId is { } parentId ? ui.Find(parentId) : null)
        {
            if (current.IsFrameWidget &&
                ui.EffectivelyFlattensRenderLayers(current))
            {
                return current.HasFrameAlphaGradient
                    ? (
                        current,
                        current.FrameAlphaGradientEdges[0],
                        current.FrameAlphaGradientEdges[1])
                    : null;
            }
        }
        return null;
    }
}
