namespace WoWAddonLab.Tests;

public sealed class BitContractTests
{
    [Fact]
    public void BitModulePublishesTheNativeEightFunctionSurface()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:function:function:function:function:function:function:function:function:nil",
            session.Lua.Evaluate(
                "local upvalueName=debug.getupvalue(bit.band,1);" +
                "return table.concat({tostring(package.loaded.bit==bit)," +
                "type(bit.bnot),type(bit.band),type(bit.bor),type(bit.bxor)," +
                "type(bit.lshift),type(bit.rshift),type(bit.arshift),type(bit.mod)," +
                "tostring(upvalueName)},':')"));
    }

    [Fact]
    public void LogicalOperationsAreVariadicAndReturnUnsignedWords()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "4294967295:2147483648:2147483649:4294967295:3",
            session.Lua.Evaluate(
                "return table.concat({bit.bnot(0)," +
                "bit.band(4294967295,4294967294,2147483648)," +
                "bit.bor(2147483648,1),bit.bxor(4294967295,0)," +
                "bit.band(15,7,3)},':')"));
    }

    [Fact]
    public void ShiftOperationsUseTheHardwareLowFiveBitCount()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "2147483648:2147483647:-1:2:1",
            session.Lua.Evaluate(
                "return table.concat({bit.lshift(1,31),bit.rshift(-1,1)," +
                "bit.arshift(-1,1),bit.lshift(1,33),bit.rshift(-1,-1)},':')"));
    }

    [Fact]
    public void ArgumentsUseCheckedLuaNumbersAndNativeInt64Truncation()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "3:0:0:0:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local noBand=pcall(bit.band);local badBand=pcall(bit.band,1,{});" +
                "local noNot=pcall(bit.bnot);local noShift=pcall(bit.lshift,1);" +
                "local badFirst=pcall(bit.lshift,{},1);local badSecond=pcall(bit.lshift,1,{});" +
                "return table.concat({bit.band('15.9',3.9),bit.band(4294967296)," +
                "bit.band(9223372036854775808),bit.band(0/0),tostring(noBand)," +
                "tostring(badBand),tostring(noNot),tostring(noShift)," +
                "tostring(badFirst),tostring(badSecond)},':')"));
    }

    [Fact]
    public void ModMatchesTheNativeRemainderAndZeroWordBranches()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:-1:2:true:true:false:false",
            session.Lua.Evaluate(
                "local missing=pcall(bit.mod,1);local invalid=pcall(bit.mod,1,{});" +
                "return table.concat({bit.mod(5,2),bit.mod(-5,2),bit.mod(5,0.5)," +
                "tostring(bit.mod(5,0)==1/0)," +
                "tostring(bit.mod(5,4294967296)==1/4294967296)," +
                "tostring(missing),tostring(invalid)},':')"));
    }
}
