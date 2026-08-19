using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class DurationObjectContractTests
{
    private static readonly string[] NativeMethods =
    [
        "Assign", "Copy", "EvaluateElapsedDuration", "EvaluateElapsedPercent",
        "EvaluateRemainingDuration", "EvaluateRemainingPercent",
        "EvaluateTotalDuration", "FormatElapsedDuration",
        "FormatRemainingDuration", "FormatTotalDuration", "GetClock",
        "GetClockTime", "GetElapsedDuration", "GetElapsedPercent", "GetEndTime",
        "GetModRate", "GetRemainingDuration", "GetRemainingPercent",
        "GetStartTime", "GetTotalDuration", "HasExpired", "HasSecretValues",
        "HasStarted", "IsActive", "IsZero", "Reset", "SetClock",
        "SetTimeFromEnd", "SetTimeFromStart", "SetTimeSpan", "SetToDefaults"
    ];

    [Fact]
    public void RegistersNativeUserdataSurfaceMetatableAndExtensionRules()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));
        var names = string.Join(",", NativeMethods.Select(name => $"'{name}'"));

        Assert.Equal(
            "userdata:false:31:true:true:7:false:true",
            session.Lua.Evaluate(
                $"local d=C_Spell.GetSpellCooldownDuration(1); " +
                $"local names={{{names}}}; local count=0; local all=true; " +
                "for _,name in ipairs(names) do count=count+1; all=all and type(d[name])=='function' end; " +
                "d.custom=7; local noUpvalue=debug.getupvalue(d.GetStartTime,1)==nil; " +
                "local writable=pcall(function() d.GetStartTime=3 end); " +
                "return table.concat({type(d),tostring(getmetatable(d)),count," +
                "tostring(all),tostring(noUpvalue),d.custom,tostring(writable)," +
                "tostring(string.find(tostring(d),'LuaDurationObject:',1,true)==1)},':')"));
    }

    [Fact]
    public void ComputesRawAndRateModifiedTimingAgainstTheSessionClock()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));
        for (var index = 0; index < 8; index++)
            session.Tick(0.25);

        Assert.Equal(
            "1:5:4:1:3:2:1:3:0.25:0.75:1:1:0.5:0.5:true:true:false:false",
            session.Lua.Evaluate(
                "local d=C_Spell.GetSpellCooldownDuration(1); " +
                "return table.concat({d:GetStartTime(),d:GetEndTime(),d:GetTotalDuration()," +
                "d:GetStartTime(1),d:GetEndTime(1),d:GetTotalDuration(1)," +
                "d:GetElapsedDuration(),d:GetRemainingDuration()," +
                "d:GetElapsedPercent(),d:GetRemainingPercent()," +
                "d:GetElapsedDuration(1),d:GetRemainingDuration(1)," +
                "d:GetElapsedPercent(1),d:GetRemainingPercent(1)," +
                "tostring(d:HasStarted()),tostring(d:IsActive())," +
                "tostring(d:HasExpired()),tostring(d:HasSecretValues())},':')"));
    }

    [Fact]
    public void MutatesAllNativeTimingVariantsAndKeepsCopiesIndependent()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));

        Assert.Equal(
            "true:4:8:6:8:4:2:3:5:2:1:3:3:0:1:0:true:9",
            session.Lua.Evaluate(
                "local d=C_Spell.GetSpellCooldownDuration(1); local c=d:Copy(); " +
                "c.custom=9; c:SetTimeFromEnd(8,4,2); " +
                "local distinct=d~=c; local a,b,c1,d1=c:GetStartTime(),c:GetEndTime()," +
                "c:GetStartTime(1),c:GetEndTime(1); d:Assign(c); " +
                "c:SetTimeSpan(3,5); local spanStart,spanEnd,spanTotal=" +
                "c:GetStartTime(),c:GetEndTime(),c:GetTotalDuration(); " +
                "c:SetTimeSpan(3,2); local clampStart,clampEnd,clampTotal=" +
                "c:GetStartTime(),c:GetEndTime(),c:GetTotalDuration(); " +
                "c:Reset(); local resetTotal,resetRate=c:GetTotalDuration(),c:GetModRate(); " +
                "return table.concat({tostring(distinct),a,b,c1,d1,d:GetTotalDuration()," +
                "d:GetModRate(),spanStart,spanEnd,spanTotal,c:GetModRate()," +
                "clampStart,clampEnd,clampTotal,resetRate,resetTotal," +
                "tostring(d.custom==nil),c.custom},':')"));
    }

    [Fact]
    public void SharedWidgetConsumersRequireAndReturnDurationUserdata()
    {
        using var session = CreateSession(new WowDurationState(9, 1, 1));

        Assert.Equal(
            "userdata:userdata:9:1:9000:1000:false:false",
            session.Lua.Evaluate(
                "local d=C_Spell.GetSpellCooldownDuration(1); " +
                "local bar=CreateFrame('StatusBar'); bar:SetTimerDuration(d); " +
                "local returned=bar:GetTimerDuration(); " +
                "local cooldown=CreateFrame('Cooldown'); " +
                "cooldown:SetCooldownFromDurationObject(d,false); local s,t=cooldown:GetCooldownTimes(); " +
                "return table.concat({type(d),type(returned),returned:GetStartTime()," +
                "returned:GetTotalDuration(),s,t," +
                "tostring(pcall(bar.SetTimerDuration,bar,{startTime=9,duration=1,modRate=1}))," +
                "tostring(pcall(cooldown.SetCooldownFromDurationObject,cooldown," +
                "{startTime=9,duration=1,modRate=1}))},':')"));
    }

    [Fact]
    public void ValidatesNativeModifierAndMutatorArgumentContracts()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));

        Assert.Equal(
            "true:true:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local d=C_Spell.GetSpellCooldownDuration(1); " +
                "return table.concat({" +
                "tostring(pcall(d.GetStartTime,d,nil))," +
                "tostring(pcall(d.SetTimeFromStart,d,'2','3',nil))," +
                "tostring(pcall(d.GetStartTime,d,2))," +
                "tostring(pcall(d.GetStartTime,d,0,1))," +
                "tostring(pcall(d.SetTimeFromStart,d,1))," +
                "tostring(pcall(d.SetTimeSpan,d,1))," +
                "tostring(pcall(d.Assign,d,{}))," +
                "tostring(pcall(d.SetClock,d,{}))},':')"));
    }

    [Fact]
    public void DurationUtilCreatesNativeShapedDurationClockAndTextBindingUserdata()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));

        Assert.Equal(
            "table:function:function:function:userdata:userdata:userdata:false:5:true:true",
            session.Lua.Evaluate(
                "local c=C_DurationUtil.CreateManualClock('ignored'); " +
                "local d=C_DurationUtil.CreateDuration('ignored'); " +
                "local b=C_DurationUtil.CreateDurationTextBinding('ignored'); " +
                "local names={'GetTime','AdvanceTime','ResetTime','RewindTime','SetTime'}; " +
                "local count=0; local all=true; for _,n in ipairs(names) do " +
                "count=count+1; all=all and type(c[n])=='function' end; c.custom=8; " +
                "return table.concat({type(C_DurationUtil),type(C_DurationUtil.CreateDuration)," +
                "type(C_DurationUtil.CreateDurationTextBinding),type(C_DurationUtil.CreateManualClock)," +
                "type(c),type(d),type(b),tostring(getmetatable(c)),count,tostring(all)," +
                "tostring(debug.getupvalue(c.GetTime,1)==nil)},':')"));
    }

    [Fact]
    public void ManualClockDrivesDurationAndUsesRecoveredSaturatingMutations()
    {
        using var session = CreateSession(new WowDurationState(1, 4, 2));

        Assert.Equal(
            "4.25:false:nil:1.25:2.75:3.25:0:4294967.295:0:true:true",
            session.Lua.Evaluate(
                "local c=C_DurationUtil.CreateManualClock(); c:SetTime(4.25); " +
                "local d=C_DurationUtil.CreateDuration(); d:SetTimeFromStart(3,4); d:SetClock(c); " +
                "local base=d:GetClock(); local initial=c:GetTime(); local same=base==c; " +
                "local inheritedMutation=type(base.AdvanceTime); " +
                "local elapsed,remaining=d:GetElapsedDuration(),d:GetRemainingDuration(); " +
                "c:AdvanceTime(2); local advanced=d:GetElapsedDuration(); c:RewindTime(10); " +
                "local rewound=c:GetTime(); c:SetTime(4294967); c:AdvanceTime(1); " +
                "local saturated=c:GetTime(); c:ResetTime(); local reset=c:GetTime(); " +
                "local copied=d:Copy(); local assigned=C_DurationUtil.CreateDuration(); assigned:Assign(d); " +
                "return table.concat({initial,tostring(same),inheritedMutation,elapsed,remaining," +
                "advanced,rewound,saturated,reset,tostring(copied:GetClock()==base)," +
                "tostring(assigned:GetClock()==base)},':')"));
    }

    private static EmulatorSession CreateSession(WowDurationState duration)
    {
        var session = new EmulatorSession();
        session.Lua.Spells.Add(1, "Duration Source").CooldownDuration = duration;
        return session;
    }
}
