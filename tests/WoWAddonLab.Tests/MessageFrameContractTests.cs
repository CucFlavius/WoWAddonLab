namespace WoWAddonLab.Tests;

public sealed class MessageFrameContractTests
{
    private static readonly string[] NativeMethods =
    [
        "AddMessage", "Clear", "GetFadeDuration", "GetFadePower", "GetFading",
        "GetFont", "GetFontObject", "GetFontStringByID", "GetIndentedWordWrap",
        "GetInsertMode", "GetJustifyH", "GetJustifyV", "GetShadowColor",
        "GetShadowOffset", "GetSpacing", "GetTextColor", "GetTimeVisible",
        "HasMessageByID", "ResetMessageFadeByID", "SetFadeDuration", "SetFadePower",
        "SetFading", "SetFont", "SetFontObject", "SetIndentedWordWrap",
        "SetInsertMode", "SetJustifyH", "SetJustifyV", "SetShadowColor",
        "SetShadowOffset", "SetSpacing", "SetTextColor", "SetTimeVisible"
    ];

    [Fact]
    public void MessageFrameRegistersItsExactOwnedSurface()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeMethods.Length + ":" +
            string.Join(',', Enumerable.Repeat("function", NativeMethods.Length)),
            session.Lua.Evaluate(
                "local frame=CreateFrame('MessageFrame'); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(frame[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void SetToDefaultsUsesInheritedFrameResetAndPreservesMessageState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "Fonts\\MORPHEUS.TTF:17:OUTLINE:4:7:2:TOP:true",
            session.Lua.Evaluate(
                "local frame=CreateFrame('MessageFrame'); " +
                "frame:SetFont('Fonts\\\\MORPHEUS.TTF',17,'OUTLINE'); " +
                "frame:SetFadeDuration(4); frame:SetFadePower(7); " +
                "frame:SetTimeVisible(2); frame:SetInsertMode('TOP'); " +
                "frame:AddMessage('retained'); frame:EnableKeyboard(true); " +
                "frame:SetToDefaults(); local path,height,flags=frame:GetFont(); " +
                "return table.concat({path,height,flags,frame:GetFadeDuration()," +
                "frame:GetFadePower(),frame:GetTimeVisible(),frame:GetInsertMode()," +
                "tostring(not frame:IsKeyboardEnabled())},':')"));
    }

    [Fact]
    public void QueuedMessagesBecomeReusableVisibleLineFontStringsOnUpdate()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local frame=CreateFrame('MessageFrame','BinaryMessagePool',UIParent); " +
            "frame:SetSize(100,40); frame:SetPoint('TOPLEFT',UIParent,'TOPLEFT',0,0); " +
            "frame:SetFont('Fonts\\\\FRIZQT__.TTF',10); " +
            "frame:AddMessage('first',1,0,0,0.5,17); " +
            "frame:AddMessage('second',nil,nil,nil,nil,18)");

        var frame = session.Ui.Find("BinaryMessagePool")!;
        Assert.Equal(2, frame.Messages.Count);
        Assert.Equal("nil:false", session.Lua.Evaluate(
            "return tostring(BinaryMessagePool:GetFontStringByID(17))..':'.." +
            "tostring(BinaryMessagePool:HasMessageByID(17))"));

        session.Tick(0);

        Assert.Empty(frame.Messages);
        Assert.Equal(4, frame.MessageLineCapacity);
        Assert.Equal(4, frame.MessageLines.Count);
        Assert.True(frame.MessageLines[0].Active);
        Assert.Equal((uint)18, frame.MessageLines[0].MessageId);
        Assert.True(frame.MessageLines[1].Active);
        Assert.Equal((uint)17, frame.MessageLines[1].MessageId);
        Assert.Equal(
            "table:true:second:first",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(BinaryMessagePool:GetFontStringByID(17))," +
                "tostring(BinaryMessagePool:HasMessageByID(17))," +
                "BinaryMessagePool:GetFontStringByID(18):GetText()," +
                "BinaryMessagePool:GetFontStringByID(17):GetText()},':')"));

        var newest = session.Ui.Find(frame.MessageLines[0].FontStringId)!;
        var older = session.Ui.Find(frame.MessageLines[1].FontStringId)!;
        Assert.Equal(-30, newest.Anchors.Single().Y);
        Assert.Equal(-20, older.Anchors.Single().Y);
        Assert.Equal(128 / 255f, older.Font!.Color.W, 4);
    }

    [Fact]
    public void WrappedMessagesReservePoolRowsInTheNativeInsertionDirection()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local frame=CreateFrame('MessageFrame','WrappedMessagePool',UIParent); " +
            "frame:SetSize(54,50); frame:SetPoint('TOPLEFT',UIParent,'TOPLEFT',0,0); " +
            "frame:SetFont('Fonts\\\\FRIZQT__.TTF',10); frame:SetInsertMode('TOP'); " +
            "frame:AddMessage('one two three four',nil,nil,nil,nil,9)");

        session.Tick(0);

        var frame = session.Ui.Find("WrappedMessagePool")!;
        Assert.Equal(5, frame.MessageLineCapacity);
        Assert.True(frame.MessageLines[0].Active);
        Assert.False(frame.MessageLines[1].Active);
        Assert.Equal((uint)9, frame.MessageLines[0].MessageId);
        Assert.Equal(0, session.Ui.Find(frame.MessageLines[0].FontStringId)!
            .Anchors.Single().Y);
        Assert.Equal(-10, session.Ui.Find(frame.MessageLines[1].FontStringId)!
            .Anchors.Single().Y);
    }

    [Fact]
    public void FadingUsesIndependentVisibleAndPowerWeightedFadeTimers()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local frame=CreateFrame('MessageFrame','FadingMessage',UIParent); " +
            "frame:SetSize(100,20); frame:SetFont('Fonts\\\\FRIZQT__.TTF',10); " +
            "frame:SetTimeVisible(0.1); frame:SetFadeDuration(0.2); " +
            "frame:SetFadePower(2); frame:AddMessage('fade',nil,nil,nil,nil,31)");
        session.Tick(0);

        var frame = session.Ui.Find("FadingMessage")!;
        var line = frame.MessageLines.Single(value => value.Active);
        var fontString = session.Ui.Find(line.FontStringId)!;

        session.Tick(0.1);
        session.Tick(0.1);
        Assert.Equal(64f / 255f, fontString.Alpha, 5);
        Assert.True(line.Active);

        session.Lua.Evaluate("FadingMessage:ResetMessageFadeByID(31)");
        Assert.Equal(1, fontString.Alpha);
        Assert.Equal(0.1f, line.TimeVisible, 3);
        Assert.Equal(0.2f, line.FadeDuration, 3);

        session.Tick(0.1);
        session.Tick(0.1);
        session.Tick(0.1);
        Assert.False(line.Active);
        Assert.False(fontString.Shown);
        Assert.False(bool.Parse(session.Lua.Evaluate(
            "return tostring(FadingMessage:HasMessageByID(31))")));
    }

    [Fact]
    public void MessageFrameXmlOwnsItsFontDefinitionInsetsAndTimingAttributes()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-messageframe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "layout.xml"),
                "<Ui><MessageFrame name=\"XmlMessageFrame\" fade=\"false\" " +
                "displayDuration=\"6\" fadeDuration=\"2\" fadePower=\"3\" " +
                "insertMode=\"TOP\"><Size x=\"120\" y=\"64\"/>" +
                "<FontString font=\"Fonts\\FRIZQT__.TTF\" height=\"14\" " +
                "justifyH=\"LEFT\" spacing=\"2\"><Color r=\"0.25\" g=\"0.5\" b=\"1\" a=\"0.75\"/>" +
                "</FontString><Insets><AbsInset left=\"3\" right=\"4\" top=\"5\" bottom=\"6\"/>" +
                "</Insets></MessageFrame></Ui>");
            File.WriteAllText(
                Path.Combine(root, "MessageFrameXml.toc"),
                "## Interface: 1\n## Title: MessageFrame XML\nlayout.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);
            var frame = session.Ui.Find("XmlMessageFrame")!;

            Assert.False(frame.MessageFading);
            Assert.Equal(6, frame.MessageTimeVisible);
            Assert.Equal(2, frame.MessageFadeDuration);
            Assert.Equal(3, frame.MessageFadePower);
            Assert.Equal("TOP", frame.MessageInsertMode);
            Assert.Equal(14, frame.Font!.FontSize);
            Assert.Equal("LEFT", frame.Font.JustifyHorizontal);
            Assert.Equal(2, frame.Font.Spacing);
            Assert.Equal(3, frame.MessageInsets.Left);
            Assert.Equal(4, frame.MessageInsets.Right);
            Assert.Equal(5, frame.MessageInsets.Top);
            Assert.Equal(6, frame.MessageInsets.Bottom);

            session.Tick(0);
            Assert.Equal(
                frame.MessageLines.Select(line => line.FontStringId).Order(),
                frame.Children.Order());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
