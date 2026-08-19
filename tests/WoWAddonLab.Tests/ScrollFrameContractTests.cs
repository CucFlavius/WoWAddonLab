namespace WoWAddonLab.Tests;

public sealed class ScrollFrameContractTests
{
    [Fact]
    public void ScrollRangeUsesTheRecursiveScrollChildBoundsRect()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "60:0",
            session.Lua.Evaluate(
                "local scroll=CreateFrame('ScrollFrame',nil,UIParent); " +
                "scroll:SetSize(100,80); scroll:SetPoint('BOTTOMLEFT',100,100); " +
                "local child=CreateFrame('Frame',nil,scroll); child:SetSize(100,80); " +
                "local overflow=CreateFrame('Frame',nil,child); overflow:SetSize(10,10); " +
                "overflow:SetPoint('TOPLEFT',child,'TOPLEFT',150,0); " +
                "scroll:SetScrollChild(child); scroll:UpdateScrollChildRect(); " +
                "local recursive=scroll:GetHorizontalScrollRange(); " +
                "child:SetIgnoringChildrenForBounds(true); scroll:UpdateScrollChildRect(); " +
                "return recursive..':'..scroll:GetHorizontalScrollRange()"));
    }

    [Fact]
    public void DesignatedScrollChildDoesNotInventClipsChildrenState()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-scroll-{Guid.NewGuid():N}");
        var addon = Path.Combine(root, "ScrollXml");
        Directory.CreateDirectory(addon);
        File.WriteAllText(
            Path.Combine(addon, "ScrollXml.toc"),
            "## Interface: 120000\nlayout.xml");
        File.WriteAllText(
            Path.Combine(addon, "layout.xml"),
            "<Ui><ScrollFrame name=\"ClipStateScroll\">" +
            "<Size x=\"100\" y=\"80\"/><ScrollChild>" +
            "<Frame name=\"ClipStateChild\"><Size x=\"200\" y=\"160\"/></Frame>" +
            "</ScrollChild></ScrollFrame></Ui>");
        try
        {
            using var session = new EmulatorSession();
            session.Load(addon);

            var scroll = session.Ui.Find("ClipStateScroll")!;
            var child = session.Ui.Find("ClipStateChild")!;

            Assert.Equal(child.Id, scroll.ScrollChildId);
            Assert.False(child.ClipsChildren);
            Assert.Equal(
                "false",
                session.Lua.Evaluate(
                    "return tostring(ClipStateChild:DoesClipChildren())"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
