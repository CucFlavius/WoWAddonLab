namespace WoWAddonLab.Tests;

public sealed class InputGlobalsContractTests
{
    [Fact]
    public void CursorPositionUsesNativeUiCoordinates()
    {
        using var session = new EmulatorSession();
        session.MouseMove(321.25f, 144.5f);

        Assert.Equal(
            "274.133:123.307:274.133:123.307:321.250:144.500:2",
            session.Lua.Evaluate(
                "local x,y=C_Input.GetCursorPosition('ignored'); " +
                "local gx,gy=GetCursorPosition(false,17); " +
                "local scale=UIParent:GetEffectiveScale(); " +
                "return string.format('%.3f:%.3f:%.3f:%.3f:%.3f:%.3f:%d'," +
                "x,y,gx,gy,x/scale,y/scale,select('#',C_Input.GetCursorPosition()))"));
    }

    [Fact]
    public void KeyboardFocusReturnsOneSharedObjectOrOneNilAndIgnoresArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:nil",
            session.Lua.Evaluate(
                "return select('#',GetCurrentKeyBoardFocus({},17))..':'.." +
                "tostring(GetCurrentKeyBoardFocus())"));

        Assert.Equal(
            "1:true",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); edit:SetFocus(); " +
                "return select('#',GetCurrentKeyBoardFocus(false))..':'.." +
                "tostring(GetCurrentKeyBoardFocus()==edit)"));
    }

    [Fact]
    public void RepresentedGeneratedApisUseTheirNativeOwners()
    {
        using var session = new EmulatorSession();
        session.Lua.Input.SupportsClipCursor = false;

        Assert.Equal(
            "function:function:function:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Input.GetCursorPosition)," +
                "type(C_Input.GetMouseFoci)," +
                "type(C_Input.MakeModifiers)," +
                "tostring(C_Client.SupportsClipCursor())," +
                "tostring(SupportsClipCursor())},':')"));
    }

    [Fact]
    public void ModifierQueriesUseInputStateAndIgnoreArguments()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:1:1:1:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(C_Input.IsAltKeyDown('ignored'))," +
                "tostring(C_Input.IsControlKeyDown({},17))," +
                "tostring(C_Input.IsShiftKeyDown(false))," +
                "tostring(C_Input.IsModifierKeyDown(nil,17))," +
                "select('#',IsAltKeyDown())," +
                "select('#',IsControlKeyDown())," +
                "select('#',IsShiftKeyDown())," +
                "select('#',IsModifierKeyDown())},':')"));

        session.Lua.Input.AltDown = true;
        Assert.Equal(
            "true:false:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(IsAltKeyDown())," +
                "tostring(IsControlKeyDown())," +
                "tostring(IsShiftKeyDown())," +
                "tostring(IsModifierKeyDown())},':')"));

        session.Lua.Input.AltDown = false;
        session.Lua.Input.ControlDown = true;
        session.Lua.Input.ShiftDown = true;
        Assert.Equal(
            "false:true:true:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(C_Input.IsAltKeyDown())," +
                "tostring(C_Input.IsControlKeyDown())," +
                "tostring(C_Input.IsShiftKeyDown())," +
                "tostring(C_Input.IsModifierKeyDown())},':')"));
    }

    [Fact]
    public void ModifierGlobalsAndInputNamespaceShareTheNativeCallbacks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:function:function:function:function:function:function:function",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(IsAltKeyDown),type(C_Input.IsAltKeyDown)," +
                "type(IsControlKeyDown),type(C_Input.IsControlKeyDown)," +
                "type(IsModifierKeyDown),type(C_Input.IsModifierKeyDown)," +
                "type(IsShiftKeyDown),type(C_Input.IsShiftKeyDown)},':')"));
    }
}
