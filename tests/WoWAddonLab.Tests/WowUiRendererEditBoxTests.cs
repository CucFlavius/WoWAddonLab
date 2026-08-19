using WoWAddonLab.Rendering;

namespace WoWAddonLab.Tests;

public sealed class WowUiRendererEditBoxTests
{
    [Fact]
    public void EditBoxOwnedFontIsIncludedInTheDrawableRenderPlan()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "RenderedEditBox=CreateFrame('EditBox','RenderedEditBox'); " +
            "OrdinaryFrame=CreateFrame('Frame','OrdinaryFrame')");

        Assert.True(WowUiRenderer.IsDrawableObject(session.Ui.Find("RenderedEditBox")!));
        Assert.False(WowUiRenderer.IsDrawableObject(session.Ui.Find("OrdinaryFrame")!));
    }
}
