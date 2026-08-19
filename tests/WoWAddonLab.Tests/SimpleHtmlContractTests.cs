namespace WoWAddonLab.Tests;

public sealed class SimpleHtmlContractTests
{
    [Fact]
    public void SetToDefaultsUsesInheritedFrameResetAndPreservesHtmlState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "Fonts\\MORPHEUS.TTF:17:OUTLINE:Fonts\\FRIZQT__.TTF:23:1:true",
            session.Lua.Evaluate(
                "local html=CreateFrame('SimpleHTML'); " +
                "html:SetFont('P','Fonts\\\\MORPHEUS.TTF',17,'OUTLINE'); " +
                "html:SetFont('H1','Fonts\\\\FRIZQT__.TTF',23); " +
                "html:SetText([[<HTML><BODY><P>retained</P></BODY></HTML>]]); " +
                "html:EnableKeyboard(true); html:SetToDefaults(); " +
                "local pp,ph,pf=html:GetFont('P'); local hp,hh=html:GetFont('H1'); " +
                "return table.concat({pp,ph,pf,hp,hh,#html:GetTextData()," +
                "tostring(not html:IsKeyboardEnabled())},':')"));
    }

    [Fact]
    public void MarkupBuildsOwnedTextRegionsAndProjectsOnlyVisibleTextNodes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3:H1:Heading:CENTER:P:Hello |Hentry|hLink|h|nNext:LEFT:P::LEFT",
            session.Lua.Evaluate(
                "local html=CreateFrame('SimpleHTML','MarkupHtml',UIParent); " +
                "html:SetSize(300,200); " +
                "html:SetPoint('TOPLEFT',UIParent,'TOPLEFT',20,-20); " +
                "html:SetFont('P','Fonts\\\\FRIZQT__.TTF',12); " +
                "html:SetFont('H1','Fonts\\\\FRIZQT__.TTF',18); " +
                "html:SetText([[<HTML><BODY>" +
                "<H1 align='CENTER'>  Heading  </H1>" +
                "<P>Hello <A href='entry'>Link</A><BR/> Next</P>" +
                "<HR/><BR/>" +
                "</BODY></HTML>]]); " +
                "local data=html:GetTextData(); " +
                "return table.concat({" +
                "#data,data[1].type,data[1].text,data[1].align," +
                "data[2].type,data[2].text,data[2].align," +
                "data[3].type,data[3].text,data[3].align},':')"));

        var html = session.Ui.Find("MarkupHtml")!;
        Assert.Equal(4, html.HtmlContentNodes.Count);
        Assert.Equal(4, html.Children.Count);
        Assert.True(html.HtmlContentHeight >= 42);
        Assert.All(
            html.HtmlContentNodes,
            node => Assert.NotNull(session.Ui.Find(node.RegionId)));
    }

    [Fact]
    public void ReplacingTextDestroysPriorOwnedRegionsAndIgnoreMarkupUsesParagraphFallback()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local html=CreateFrame('SimpleHTML','ReplaceHtml',UIParent); " +
            "html:SetSize(200,100); " +
            "html:SetPoint('TOPLEFT',UIParent,'TOPLEFT',0,0); " +
            "html:SetText([[<HTML><BODY><H1>First</H1><P>Second</P></BODY></HTML>]]); " +
            "return true");

        var html = session.Ui.Find("ReplaceHtml")!;
        var oldRegionIds = html.HtmlContentNodes.Select(node => node.RegionId).ToArray();

        Assert.Equal(
            "1:P:<H1>literal</H1>:LEFT",
            session.Lua.Evaluate(
                "local html=ReplaceHtml; " +
                "html:SetText('<H1>literal</H1>',true); " +
                "local data=html:GetTextData(); " +
                "return table.concat({#data,data[1].type,data[1].text,data[1].align},':')"));

        Assert.Single(html.HtmlContentNodes);
        Assert.Single(html.Children);
        Assert.All(oldRegionIds, id => Assert.Null(session.Ui.Find(id)));
    }
}
