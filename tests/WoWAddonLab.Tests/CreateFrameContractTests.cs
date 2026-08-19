namespace WoWAddonLab.Tests;

public sealed class CreateFrameContractTests
{
    [Fact]
    public void CreateFrameRequiresAStringCoercibleRegisteredObjectType()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false:true:false:true:false:true:false:true:true:Model",
            session.Lua.Evaluate(
                "local function check(call,needle) " +
                "local ok,err=pcall(call); return tostring(ok)," +
                "tostring(not ok and string.find(err,needle,1,true)~=nil) end; " +
                "local a,b=check(function() CreateFrame() end,'Usage: CreateFrame'); " +
                "local c,d=check(function() CreateFrame(nil) end,'Usage: CreateFrame'); " +
                "local e,f=check(function() CreateFrame(true) end,'Usage: CreateFrame'); " +
                "local g,h=check(function() CreateFrame('Frame',nil,true) end,'Usage: CreateFrame'); " +
                "local i,j=check(function() CreateFrame('NotARegisteredWidget') end," +
                "\"CreateFrame: Unknown frame type 'NotARegisteredWidget'\"); " +
                "local model=CreateFrame('Model'); " +
                "return table.concat({a,b,c,d,e,f,g,h,i,j," +
                "tostring(type(model.ClearModel)=='function'),model:GetObjectType()},':')"));
    }

    [Fact]
    public void CreateFrameAppliesNumericIdBeforeTemplateOnLoad()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-create-frame-id-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "CreateFrameId.xml"),
                "<Ui><Frame name=\"CreateFrameIdTemplate\" virtual=\"true\">" +
                "<Scripts><OnLoad>self.idSeenOnLoad=self:GetID()</OnLoad></Scripts>" +
                "</Frame></Ui>");
            File.WriteAllText(
                Path.Combine(root, "CreateFrameId.lua"),
                "CreateFrameIdTarget=CreateFrame('Frame','CreateFrameIdTarget'," +
                "UIParent,'CreateFrameIdTemplate',73)");
            File.WriteAllText(
                Path.Combine(root, "CreateFrameId.toc"),
                "## Interface: 1\n## Title: CreateFrame ID\n" +
                "CreateFrameId.xml\nCreateFrameId.lua\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "73:73",
                session.Lua.Evaluate(
                    "return CreateFrameIdTarget:GetID()..':'.." +
                    "CreateFrameIdTarget.idSeenOnLoad"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateForbiddenFrameUsesTheSharedFactoryAndMarksTheResultForbidden()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:false:true:Frame",
            session.Lua.Evaluate(
                "local ordinary=CreateFrame('Frame'); " +
                "local forbidden=CreateForbiddenFrame('Frame'); " +
                "return table.concat({type(CreateForbiddenFrame)," +
                "tostring(ordinary:IsForbidden()),tostring(forbidden:IsForbidden())," +
                "forbidden:GetObjectType()},':')"));
    }
}
