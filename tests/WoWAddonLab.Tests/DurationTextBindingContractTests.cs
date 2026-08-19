namespace WoWAddonLab.Tests;

public sealed class DurationTextBindingContractTests
{
    private static readonly string[] NativeMethods =
    [
        "CanFormatText", "CanUpdateFontString", "Disable", "Enable",
        "GetDuration", "GetExpiredText", "GetFontString", "GetFormattedText",
        "GetTimeModifier", "GetUpdateInterval", "GetZeroDurationText",
        "HasSecretValues", "IsEnabled", "SetDuration", "SetEnabled",
        "SetExpiredText", "SetFontString", "SetFormatter", "SetTextFormat",
        "SetTimeModifier", "SetToDefaults", "SetUpdateInterval",
        "SetZeroDurationText", "UpdateFontString"
    ];

    [Fact]
    public void RegistersRecoveredSurfaceEnumsAndNativeDefaults()
    {
        using var session = new EmulatorSession();
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:24:true:true:7:false:true:false:false:false:" +
            "nil:nil:nil:nil::0:0:false:7:6:2:1",
            session.Lua.Evaluate(
                $"local b=C_DurationUtil.CreateDurationTextBinding('ignored'); " +
                $"local names={{{names}}}; local count=0; local all=true; " +
                "for _,name in ipairs(names) do count=count+1; " +
                "all=all and type(b[name])=='function' end; b.custom=7; " +
                "local writable=pcall(function() b.Enable=3 end); " +
                "return table.concat({type(b),tostring(getmetatable(b)),count," +
                "tostring(all),tostring(debug.getupvalue(b.Enable,1)==nil),b.custom," +
                "tostring(writable),tostring(string.find(tostring(b)," +
                "'DurationTextBinding:',1,true)==1),tostring(b:CanFormatText())," +
                "tostring(b:CanUpdateFontString()),tostring(b:IsEnabled())," +
                "tostring(b:GetDuration()),tostring(b:GetExpiredText())," +
                "tostring(b:GetFontString()),tostring(b:GetZeroDurationText())," +
                "b:GetFormattedText(),b:GetTimeModifier(),b:GetUpdateInterval()," +
                "tostring(b:HasSecretValues())," +
                "Enum.DurationTextBindingPropertyMeta.NumValues," +
                "Enum.DurationTextBindingPropertyMeta.MaxValue," +
                "Enum.DurationTimeModifierMeta.NumValues," +
                "Enum.DurationTimeModifierMeta.MaxValue},':')"));
    }

    [Fact]
    public void FormatsRecoveredPropertiesAndSetFormatterReplacesTheComponentList()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:3 seconds:R=3 seconds E=1 RP=75:" +
            "R=1 second E=1 RP=50:DONE:true:true",
            session.Lua.Evaluate(
                "local clock=C_DurationUtil.CreateManualClock(); clock:SetTime(2); " +
                "local d=C_DurationUtil.CreateDuration(); d:SetTimeFromStart(1,4,2); " +
                "d:SetClock(clock); local f=C_StringUtil.CreateSecondsFormatter(); " +
                "local b=C_DurationUtil.CreateDurationTextBinding(); b:SetDuration(d); " +
                "b:SetFormatter(f); local single=b:GetFormattedText(); " +
                "b:SetTextFormat('R={} E={} RP={}',{" +
                "{property=Enum.DurationTextBindingProperty.RemainingDuration,formatter=f}," +
                "{property=Enum.DurationTextBindingProperty.ElapsedDuration}," +
                "{property=Enum.DurationTextBindingProperty.RemainingPercent}}); " +
                "local composite=b:GetFormattedText(); b:SetTimeModifier(" +
                "Enum.DurationTimeModifier.BaseTime); local modified=b:GetFormattedText(); " +
                "b:SetExpiredText('DONE'); clock:SetTime(4); local expired=b:GetFormattedText(); " +
                "return table.concat({tostring(b:CanFormatText()),single,composite," +
                "modified,expired,tostring(b:GetDuration()==d)," +
                "tostring(b:GetExpiredText()=='DONE')},':')"));
    }

    [Fact]
    public void UsesZeroAndExpiredFallbacksAndRejectsMismatchedComponents()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            ":ZERO:DONE:false:true:true:nil:nil:11",
            session.Lua.Evaluate(
                "local b=C_DurationUtil.CreateDurationTextBinding(); b.custom=11; " +
                "local empty=b:GetFormattedText(); b:SetZeroDurationText('ZERO'); " +
                "local zero=C_DurationUtil.CreateDuration(); b:SetDuration(zero); " +
                "b:SetFormatter(C_StringUtil.CreateSecondsFormatter()); " +
                "local zeroText=b:GetFormattedText(); local c=C_DurationUtil.CreateManualClock(); " +
                "c:SetTime(3); local d=C_DurationUtil.CreateDuration(); " +
                "d:SetTimeFromStart(1,1); d:SetClock(c); b:SetDuration(d); " +
                "b:SetExpiredText('DONE'); local expired=b:GetFormattedText(); " +
                "local mismatch=pcall(b.SetTextFormat,b,'{} {}',{{property=0}}); " +
                "b:SetExpiredText(nil); b:SetZeroDurationText(); b:SetToDefaults(); " +
                "return table.concat({empty,zeroText,expired,tostring(mismatch)," +
                "tostring(b:GetDuration()==nil),tostring(not b:IsEnabled())," +
                "tostring(b:GetExpiredText()),tostring(b:GetZeroDurationText())," +
                "b.custom},':')"));
    }

    [Fact]
    public void UpdatesBoundFontStringsAtTheRecoveredIntervalAndEnableBoundary()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:3 seconds",
            session.Lua.Evaluate(
                "BindingClock=C_DurationUtil.CreateManualClock(); BindingClock:SetTime(2); " +
                "BindingDuration=C_DurationUtil.CreateDuration(); " +
                "BindingDuration:SetTimeFromStart(1,4); BindingDuration:SetClock(BindingClock); " +
                "BindingText=UIParent:CreateFontString(); " +
                "BindingValue=C_DurationUtil.CreateDurationTextBinding(); " +
                "BindingValue:SetDuration(BindingDuration); " +
                "BindingValue:SetFormatter(C_StringUtil.CreateSecondsFormatter()); " +
                "BindingValue:SetFontString(BindingText); BindingValue:SetUpdateInterval(1); " +
                "BindingValue:Enable(); return table.concat({" +
                "tostring(BindingValue:CanUpdateFontString())," +
                "tostring(BindingValue:IsEnabled()),BindingText:GetText()},':')"));

        session.Lua.Evaluate("BindingClock:SetTime(3)");
        session.Tick(0.25);
        session.Tick(0.25);
        var beforeInterval = session.Lua.Evaluate("return BindingText:GetText()");
        session.Tick(0.25);
        session.Tick(0.25);
        var afterInterval = session.Lua.Evaluate("return BindingText:GetText()");
        session.Lua.Evaluate("BindingValue:Disable(); BindingClock:SetTime(4)");
        session.Tick(2);
        var disabled = session.Lua.Evaluate("return BindingText:GetText()");
        session.Lua.Evaluate("BindingValue:UpdateFontString()");

        Assert.Equal(
            "3 seconds:2 seconds:2 seconds:1 second:false",
            string.Join(
                ':',
                beforeInterval,
                afterInterval,
                disabled,
                session.Lua.Evaluate("return BindingText:GetText()"),
                session.Lua.Evaluate("return tostring(BindingValue:IsEnabled())")));
    }
}
