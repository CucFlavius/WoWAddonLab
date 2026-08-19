namespace WoWAddonLab.Tests;

public sealed class PrintOwnershipContractTests
{
    [Fact]
    public void BasePrintRemainsOwnedByLuaAndUsesTheGlobalToStringFunction()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:0:3:alpha:2:false",
            session.Lua.Evaluate(
                "local original=tostring; local seen={};" +
                "tostring=function(value) seen[#seen+1]=original(value); return original(value) end;" +
                "local count=select('#',print('alpha',2,false));" +
                "return table.concat({type(print),count,#seen,seen[1],seen[2],seen[3]},':')"));
    }

    [Fact]
    public void BasePrintRejectsANonStringToStringResult()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true",
            session.Lua.Evaluate(
                "local original=tostring; tostring=function() return {} end;" +
                "local ok,err=pcall(print,1);" +
                "return original(ok)..':'..original(string.find(err," +
                "\"'tostring' must return a string to 'print'\",1,true)~=nil)"));
    }
}
