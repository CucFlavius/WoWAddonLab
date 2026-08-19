namespace WoWAddonLab.Tests;

public sealed class EnumerateFramesContractTests
{
    [Fact]
    public void NonTableArgumentsRestartEnumerationWhileInvalidTablesUseTheNativeErrors()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:true:false:true:false:true",
            session.Lua.Evaluate(
                "local first=EnumerateFrames(); " +
                "local scalarResults={" +
                "tostring(EnumerateFrames(false)==first)," +
                "tostring(EnumerateFrames(17)==first)," +
                "tostring(EnumerateFrames('frame')==first)," +
                "tostring(EnumerateFrames(function() end)==first)," +
                "tostring(EnumerateFrames(coroutine.create(function() end))==first)}; " +
                "local plainOk,plainError=pcall(EnumerateFrames,{}); " +
                "local owner=CreateFrame('Frame',nil,UIParent); " +
                "local texture=owner:CreateTexture(); " +
                "local textureOk,textureError=pcall(EnumerateFrames,texture); " +
                "return table.concat(scalarResults,':')..':'.." +
                "table.concat({tostring(plainOk)," +
                "tostring(string.find(plainError,\"Couldn't find 'this'\",1,true)~=nil)," +
                "tostring(textureOk),tostring(string.find(textureError,'expected frame',1,true)~=nil)},':')"));
    }

    [Fact]
    public void EnumerationSkipsForbiddenFramesButCanContinueFromOne()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "EnumAfter:EnumAfter:true:true",
            session.Lua.Evaluate(
                "local before=CreateFrame('Frame','EnumBefore',UIParent); " +
                "local forbidden=CreateForbiddenFrame('Frame','EnumForbidden',UIParent); " +
                "local inherited=CreateFrame('Frame','EnumInherited',forbidden); " +
                "local after=CreateFrame('Frame','EnumAfter',UIParent); " +
                "local found={}; local current=before; " +
                "repeat current=EnumerateFrames(current); " +
                "if current==forbidden or current==inherited or current==after then " +
                "table.insert(found,current:GetName()) end until not current; " +
                "return table.concat(found,',')..':'..EnumerateFrames(forbidden):GetName()..':'.." +
                "tostring(forbidden:IsForbidden())..':'..tostring(inherited:IsForbidden())"));
    }
}
