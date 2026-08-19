namespace WoWAddonLab.Tests;

public sealed class GameTooltipContractTests
{
    [Fact]
    public void ShrinkToFitUsesTheWidestActualWrappedRowUnlessWidthIsForced()
    {
        using var session = new EmulatorSession();
        const string text =
            "abcdefghijklmnopqrstuvwxy abcdefghijklmnopqrstuvwxy " +
            "abcdefghijklmnopqrstuvwxy abcdefghijklmnopqrstuvwxy";

        session.Lua.Evaluate(
            $"GameTooltip:SetText('{text}',1,1,1,1,true)");
        var tooltip = session.Ui.Find("GameTooltip")!;
        var shrunk = tooltip.Width!.Value;

        session.Lua.Evaluate("GameTooltip:SetShrinkToFitWrapped(false)");
        var notShrunk = tooltip.Width!.Value;

        session.Lua.Evaluate(
            "GameTooltip:SetShrinkToFitWrapped(true); " +
            "GameTooltip:SetMinimumWidth(240,true)");
        var forced = tooltip.Width!.Value;

        Assert.True(shrunk < notShrunk);
        Assert.Equal(250.4f, notShrunk, 3);
        Assert.True(forced >= 240);
    }

    [Fact]
    public void TexturesInTheSameAnchorGroupShareOneMaximumLayoutExtent()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "GameTooltip:SetText('row'); " +
            "local info={width=16,height=16,margin={left=1,right=3}," +
            "anchor=1,region=0}; GameTooltip:AddTexture(1,info)");
        var tooltip = session.Ui.Find("GameTooltip")!;
        var oneTexture = tooltip.Width!.Value;

        session.Lua.Evaluate(
            "local info={width=12,height=16,margin={left=1,right=3}," +
            "anchor=1,region=0}; GameTooltip:AddTexture(2,info)");
        var sameGroup = tooltip.Width!.Value;

        session.Lua.Evaluate(
            "local info={width=12,height=16,margin={left=1,right=3}," +
            "anchor=4,region=0}; GameTooltip:AddTexture(3,info)");
        var oppositeGroup = tooltip.Width!.Value;

        Assert.Equal(oneTexture, sameGroup);
        Assert.True(oppositeGroup > sameGroup);
    }

    [Fact]
    public void DoubleLineGapAndLinePaddingFollowTheNativeLayoutConstants()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "GameTooltip:ClearLines(); " +
            "GameTooltip:AddDoubleLine('left','right',1,1,1,1,1,1,false,7)");

        var tooltip = session.Ui.Find("GameTooltip")!;
        var line = tooltip.Tooltip!.Lines[0];
        var left = session.Ui.Find(line.LeftId)!;
        var right = session.Ui.Find(line.RightId)!;
        var leftBounds = session.Ui.ResolveBounds(left.Id);
        var rightBounds = session.Ui.ResolveBounds(right.Id);

        Assert.Equal(7, line.LeftPadding);
        Assert.Equal(17, leftBounds.Left - session.Ui.ResolveBounds(tooltip.Id).Left, 3);
        Assert.Equal(38.4f, rightBounds.Left - leftBounds.Right, 3);
    }
}
