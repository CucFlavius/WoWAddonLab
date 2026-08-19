namespace WoWAddonLab.Tests;

public sealed class MetatableGlobalsContractTests
{
    [Fact]
    public void MetatableGlobalsReturnTheConcreteSharedObjectMetatables()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:true:true",
            session.Lua.Evaluate(
                "local frame=CreateFrame('Frame'); " +
                "local button=CreateFrame('Button'); " +
                "local text=frame:CreateFontString(); " +
                "local fm=GetFrameMetatable('ignored'); " +
                "local bm=GetButtonMetatable(false); " +
                "local tm=GetFontStringMetatable({}); " +
                "return table.concat({" +
                "tostring(fm==getmetatable(frame))," +
                "tostring(bm==getmetatable(button))," +
                "tostring(tm==getmetatable(text))," +
                "tostring(fm==GetFrameMetatable())," +
                "tostring(fm~=bm),tostring(bm~=tm)},':')"));
    }
}
