namespace WoWAddonLab.Tests;

public sealed class SliderContractTests
{
    [Fact]
    public void NativeThumbRegionsHaveResolvedTrackGeometry()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local slider=CreateFrame('Slider','ResolvedThumbSlider',UIParent); " +
            "slider:SetPoint('BOTTOMLEFT',100,100); slider:SetSize(200,20); " +
            "slider:SetMinMaxValues(0,100); slider:SetValue(50); " +
            "local thumb=slider:CreateTexture('ResolvedThumbTexture'); " +
            "thumb:SetSize(20,18); slider:SetThumbTexture(thumb)");

        var thumb = session.Ui.Find("ResolvedThumbTexture")!;
        var bounds = session.Ui.ResolveBounds(thumb.Id);

        Assert.True(session.Ui.HasResolvedRect(thumb));
        Assert.Equal(190, bounds.Left);
        Assert.Equal(101, bounds.Bottom);
        Assert.Equal(20, bounds.Width);
        Assert.Equal(18, bounds.Height);
    }

    [Fact]
    public void PointerInputWithoutAThumbDoesNotInventTrackGeometry()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "NoThumbSlider=CreateFrame('Slider','NoThumbSlider',UIParent); " +
            "NoThumbSlider:SetPoint('BOTTOMLEFT',100,100); " +
            "NoThumbSlider:SetSize(100,20); NoThumbSlider:SetMinMaxValues(0,100); " +
            "NoThumbSlider:SetValue(50)");

        session.MouseMove(190, 110);
        session.MouseButton("LeftButton", true);
        var whileDown = session.Lua.Evaluate(
            "return NoThumbSlider:GetValue()..':'.." +
            "tostring(NoThumbSlider:IsDraggingThumb())");
        session.MouseMove(105, 110);
        session.MouseButton("LeftButton", false);

        Assert.Equal("50:false", whileDown);
        Assert.Equal(
            "50:false",
            session.Lua.Evaluate(
                "return NoThumbSlider:GetValue()..':'.." +
                "tostring(NoThumbSlider:IsDraggingThumb())"));
    }

    [Fact]
    public void ThumbTextureObjectsAreOwnedLayeredAndReplacedLikeNativeRegions()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:OVERLAY:true:true:false:false",
            session.Lua.Evaluate(
                "local slider=CreateFrame('Slider',nil,UIParent); " +
                "local firstOwner=CreateFrame('Frame',nil,UIParent); " +
                "local first=firstOwner:CreateTexture(); first:SetTexture(1); " +
                "local secondOwner=CreateFrame('Frame',nil,UIParent); " +
                "local second=secondOwner:CreateTexture(); second:SetTexture(2); " +
                "slider:SetThumbTexture(first); " +
                "local firstOwned=first:GetParent()==slider; " +
                "local layer=first:GetDrawLayer(); slider:SetThumbTexture(second); " +
                "local bad=pcall(slider.SetThumbTexture,slider,firstOwner); " +
                "return table.concat({tostring(firstOwned),layer," +
                "tostring(second:GetParent()==slider)," +
                "tostring(slider:GetThumbTexture()==second)," +
                "tostring(first:IsShown()),tostring(bad)},':')"));
    }

    [Fact]
    public void DragPointerUpdatesUseTheNativeOnePerTenMillisecondWindow()
    {
        using var session = new EmulatorSession();
        uint timestamp = 100;
        session.InputTimestampMillisecondsProvider = () => timestamp;
        session.Lua.Evaluate(
            "ThrottleSlider=CreateFrame('Slider','ThrottleSlider',UIParent); " +
            "ThrottleSlider:SetPoint('BOTTOMLEFT',100,100); " +
            "ThrottleSlider:SetSize(200,20); " +
            "ThrottleSlider:SetMinMaxValues(0,100); ThrottleSlider:SetValue(50); " +
            "local thumb=ThrottleSlider:CreateTexture('ThrottleSliderThumb'); " +
            "thumb:SetSize(20,20); ThrottleSlider:SetThumbTexture(thumb)");

        session.MouseMove(200, 110);
        session.MouseButton("LeftButton", true);

        session.MouseMove(280, 110);
        var firstUpdate = session.Lua.Evaluate("return ThrottleSlider:GetValue()");

        timestamp = 105;
        session.MouseMove(120, 110);
        var throttledUpdate = session.Lua.Evaluate("return ThrottleSlider:GetValue()");

        timestamp = 110;
        session.MouseMove(120, 110);
        var nextWindowUpdate = session.Lua.Evaluate("return ThrottleSlider:GetValue()");
        session.MouseButton("LeftButton", false);

        Assert.Equal(firstUpdate, throttledUpdate);
        Assert.NotEqual(firstUpdate, nextWindowUpdate);
    }
}
