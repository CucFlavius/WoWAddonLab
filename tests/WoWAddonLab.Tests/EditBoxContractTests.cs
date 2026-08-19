using System.Numerics;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class EditBoxContractTests
{
    private static readonly string[] NativeEditBoxMethods =
    [
        "AddHistoryLine", "ClearFocus", "ClearHighlightText", "ClearHistory",
        "Disable", "Enable", "GetAltArrowKeyMode", "GetBlinkSpeed",
        "GetCursorPosition", "GetDisplayText", "GetFont", "GetFontObject",
        "GetHighlightColor", "GetHistoryLines", "GetIndentedWordWrap",
        "GetInputLanguage", "GetJustifyH", "GetJustifyV", "GetMaxBytes",
        "GetMaxLetters", "GetNumLetters", "GetNumLines", "GetNumber",
        "GetShadowColor", "GetShadowOffset", "GetSpacing", "GetText",
        "GetTextColor", "GetTextInsets", "GetUTF8CursorPosition",
        "GetVisibleTextByteLimit", "HasFocus", "HasText", "HighlightText",
        "Insert", "IsAlphabeticOnly", "IsAutoFocus", "IsCountInvisibleLetters",
        "IsEnabled", "IsInIMECompositionMode", "IsMultiLine", "IsNumeric",
        "IsNumericFullRange", "IsPassword", "IsSecureText", "ResetInputMode",
        "SetAlphabeticOnly", "SetAltArrowKeyMode", "SetAutoFocus",
        "SetBlinkSpeed", "SetCountInvisibleLetters", "SetCursorPosition",
        "SetEnabled", "SetFocus", "SetFont", "SetFontObject",
        "SetHighlightColor", "SetHistoryLines", "SetIndentedWordWrap",
        "SetJustifyH", "SetJustifyV", "SetMaxBytes", "SetMaxLetters",
        "SetMultiLine", "SetNumber", "SetNumeric", "SetNumericFullRange",
        "SetPassword", "SetSecureText", "SetSecurityDisablePaste",
        "SetSecurityDisableSetText", "SetShadowColor", "SetShadowOffset",
        "SetSpacing", "SetText", "SetTextColor", "SetTextInsets",
        "SetVisibleTextByteLimit", "ToggleInputLanguage"
    ];

    [Fact]
    public void EditBoxExposesEveryMethodInItsRecoveredNativeRegistrar()
    {
        using var session = new EmulatorSession();
        var literal = string.Join(
            ',',
            NativeEditBoxMethods.Select(value => $"'{value}'"));

        Assert.Equal(
            NativeEditBoxMethods.Length + ":" +
            string.Join(
                ',',
                Enumerable.Repeat("function", NativeEditBoxMethods.Length)),
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox','EditBoxBinarySurface',UIParent); " +
                $"local methods={{{literal}}}; local result={{}}; " +
                "for _,name in ipairs(methods) do result[#result+1]=type(edit[name]) end; " +
                "return #methods..':'..table.concat(result,',')"));
    }

    [Fact]
    public void InputLanguageTracksFocusedNativeStateAndToggleRemainsANoOp()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "LanguageEdit=CreateFrame('EditBox','LanguageEdit',UIParent); " +
            "LanguageEdit:SetFocus(); languageEvents=''; " +
            "LanguageEdit:SetScript('OnInputLanguageChanged',function(_,language) " +
            "languageEvents=languageEvents..language..',' end)");

        Assert.Equal(
            "ROMAN:0:ROMAN",
            session.Lua.Evaluate(
                "local before=LanguageEdit:GetInputLanguage(); " +
                "local returns=select('#',LanguageEdit:ToggleInputLanguage()); " +
                "return before..':'..returns..':'..LanguageEdit:GetInputLanguage()"));

        session.InputLanguageChanged(UiEditBoxInputLanguage.Chinese);
        session.InputLanguageChanged(UiEditBoxInputLanguage.Chinese);
        session.InputLanguageChanged(UiEditBoxInputLanguage.Japanese);
        session.InputLanguageChanged(UiEditBoxInputLanguage.Korean);

        Assert.Equal(
            "KOREAN:CHINESE,JAPANESE,KOREAN,",
            session.Lua.Evaluate(
                "return LanguageEdit:GetInputLanguage()..':'..languageEvents"));

        session.Lua.Evaluate("LanguageEdit:SetPassword(true)");
        session.InputLanguageChanged(UiEditBoxInputLanguage.Roman);
        Assert.Equal(
            "KOREAN:CHINESE,JAPANESE,KOREAN,",
            session.Lua.Evaluate(
                "return LanguageEdit:GetInputLanguage()..':'..languageEvents"));

        session.Lua.Evaluate(
            "LanguageEdit:SetPassword(false); LanguageEdit:SetToDefaults()");
        Assert.Equal(
            "KOREAN",
            session.Lua.Evaluate("return LanguageEdit:GetInputLanguage()"));
    }

    [Fact]
    public void HighlightTextUsesUtf8ByteOffsetsAndSelectsToEndForReversedStop()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "aXöz:abcdX",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); edit:SetText('aéöz'); " +
                "edit:HighlightText(1,3); edit:Insert('X'); local utf8=edit:GetText(); " +
                "edit:SetText('abcdef'); edit:HighlightText(4,2); edit:Insert('X'); " +
                "return utf8..':'..edit:GetText()"));
    }

    [Fact]
    public void VisibleTextByteLimitImmediatelyEnforcesUtf8Bytes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "aé:a:1",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); edit:SetText('aéz'); " +
                "edit:SetVisibleTextByteLimit(3); local three=edit:GetText(); " +
                "edit:SetText('aéz'); edit:SetVisibleTextByteLimit(2); " +
                "return three..':'..edit:GetText()..':'..edit:GetCursorPosition()"));
    }

    [Fact]
    public void PasteAndSetTextSecurityLocksRemainDistinct()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:ab:false:false:ab",
            session.Lua.Evaluate(
                "local paste=CreateFrame('EditBox'); paste:SetSecurityDisablePaste(); " +
                "local insertOk=pcall(paste.Insert,paste,'a'); " +
                "local setOk=pcall(paste.SetText,paste,'ab'); " +
                "local secure=CreateFrame('EditBox'); secure:SetText('ab'); " +
                "secure:SetSecurityDisableSetText(); " +
                "local blockedPaste=pcall(secure.Insert,secure,'c'); " +
                "local blockedText=pcall(secure.SetText,secure,'c'); " +
                "return table.concat({tostring(insertOk),tostring(setOk),paste:GetText()," +
                "tostring(blockedPaste),tostring(blockedText),secure:GetText()},':')"));
    }

    [Fact]
    public void EditBoxTextInsetsDefineTheInternalFontStringRectangle()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local edit=CreateFrame('EditBox','InsetEditBox',UIParent); " +
            "edit:SetPoint('BOTTOMLEFT',100,200); edit:SetSize(300,80); " +
            "edit:SetTextInsets(10,20,5,15)");

        var edit = session.Ui.Find("InsetEditBox")!;
        var bounds = session.Ui.ResolveTextBounds(edit);

        Assert.Equal(new Vector2(110, 215), new Vector2(bounds.Left, bounds.Bottom));
        Assert.Equal(new Vector2(270, 60), new Vector2(bounds.Width, bounds.Height));
    }

    [Fact]
    public void MultiLineSwitchesTheOwnedFontStringBetweenNativeVerticalModes()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local edit=CreateFrame('EditBox','MultilineEditBox',UIParent); " +
            "edit:SetFont('Fonts\\\\FRIZQT__.TTF',12,''); edit:SetMultiLine(true)");
        var edit = session.Ui.Find("MultilineEditBox")!;

        Assert.Equal("LEFT", edit.Font!.JustifyHorizontal);
        Assert.Equal("TOP", edit.Font.JustifyVertical);
        Assert.True(edit.Font.WordWrap);

        session.Lua.Evaluate("MultilineEditBox:SetMultiLine(false)");

        Assert.Equal("MIDDLE", edit.Font.JustifyVertical);
        Assert.False(edit.Font.WordWrap);
    }

    [Fact]
    public void FontObjectInheritancePreservesTheOwnedEditBoxLayoutModes()
    {
        using var session = new EmulatorSession();

        session.Lua.Evaluate(
            "local inherited=CreateFont('CenteredEditFont'); " +
            "inherited:SetFont('Fonts\\\\FRIZQT__.TTF',14,''); " +
            "inherited:SetJustifyH('CENTER'); inherited:SetJustifyV('BOTTOM'); " +
            "local edit=CreateFrame('EditBox','InheritedEditBox'); " +
            "edit:SetFontObject(inherited)");

        Assert.Equal(
            "LEFT:MIDDLE",
            session.Lua.Evaluate(
                "return InheritedEditBox:GetJustifyH()..':'..InheritedEditBox:GetJustifyV()"));

        session.Lua.Evaluate("InheritedEditBox:SetMultiLine(true)");

        var edit = session.Ui.Find("InheritedEditBox")!;
        Assert.Equal("LEFT", edit.Font!.JustifyHorizontal);
        Assert.Equal("TOP", edit.Font.JustifyVertical);
        Assert.True(edit.Font.WordWrap);
    }

    [Fact]
    public void RejectedInsertionDoesNotAdvanceTheCursor()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "12:1",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); edit:SetNumeric(true); " +
                "edit:SetText('12'); edit:SetCursorPosition(1); edit:Insert('x'); " +
                "return edit:GetText()..':'..edit:GetCursorPosition()"));
    }

    [Fact]
    public void SetNumberDispatchesOnTextSetImmediatelyAndTextChangedOnUpdate()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "12:missing:1",
            session.Lua.Evaluate(
                "editBoxEventTest=CreateFrame('EditBox'); " +
                "editBoxChanged='missing'; editBoxTextSet=0; " +
                "editBoxEventTest:SetScript('OnTextChanged',function(_,userInput) " +
                "editBoxChanged=tostring(userInput) end); " +
                "editBoxEventTest:SetScript('OnTextSet',function() " +
                "editBoxTextSet=editBoxTextSet+1 end); " +
                "editBoxEventTest:SetNumber(12); " +
                "return editBoxEventTest:GetText()..':'..editBoxChanged..':'..editBoxTextSet"));

        session.Tick(0);

        Assert.Equal(
            "false:1",
            session.Lua.Evaluate("return editBoxChanged..':'..editBoxTextSet"));
    }

    [Fact]
    public void AutoFocusRunsAfterOnShowAndHonorsStateChangedByTheHandler()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:false",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); edit:Hide(); edit:ClearFocus(); " +
                "local focusedDuringShow=true; " +
                "edit:SetScript('OnShow',function(self) focusedDuringShow=self:HasFocus() end); " +
                "edit:Show(); local focusedAfterShow=edit:HasFocus(); edit:ClearFocus(); " +
                "local disabled=CreateFrame('EditBox'); disabled:Hide(); " +
                "disabled:SetScript('OnShow',function(self) self:SetAutoFocus(false) end); " +
                "disabled:Show(); " +
                "return table.concat({tostring(focusedDuringShow)," +
                "tostring(focusedAfterShow),tostring(disabled:HasFocus())},':')"));
    }

    [Fact]
    public void DisablingAnInactiveInputModeDoesNotClearTheActiveMode()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:false",
            session.Lua.Evaluate(
                "local edit=CreateFrame('EditBox'); " +
                "edit:SetPassword(true); edit:SetNumeric(false); " +
                "local password=edit:IsPassword(); " +
                "edit:SetAlphabeticOnly(true); edit:SetPassword(false); " +
                "local alphabetic=edit:IsAlphabeticOnly(); " +
                "edit:SetNumeric(true); edit:SetNumericFullRange(false); " +
                "local numeric=edit:IsNumeric(); " +
                "edit:SetNumericFullRange(true); edit:SetNumeric(true); " +
                "return table.concat({tostring(password),tostring(alphabetic)," +
                "tostring(numeric),tostring(edit:IsNumeric())," +
                "tostring(edit:IsNumericFullRange())},':')"));
    }

    [Fact]
    public void ArrowKeysExtendSelectionAndDispatchTheNativeDirectionScript()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ArrowEdit=CreateFrame('EditBox'); ArrowEdit:SetText('abc'); " +
            "ArrowEdit:SetCursorPosition(1); ArrowEdit:SetFocus(); arrowTrace=''; " +
            "ArrowEdit:SetScript('OnArrowPressed',function(_,direction) " +
            "arrowTrace=arrowTrace..direction end)");

        session.Key("LEFT", true, shift: true);
        session.TextInput("X");

        Assert.Equal(
            "Xbc:LEFT",
            session.Lua.Evaluate("return ArrowEdit:GetText()..':'..arrowTrace"));
    }

    [Fact]
    public void FocusedEditBoxDispatchesKeyDownBeforeItsNativeKeyBehavior()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "KeyOrderEdit=CreateFrame('EditBox'); KeyOrderEdit:SetText('abc'); " +
            "KeyOrderEdit:SetCursorPosition(1); KeyOrderEdit:SetFocus(); keyTrace=''; " +
            "KeyOrderEdit:SetScript('OnKeyDown',function(self,key) " +
            "keyTrace=keyTrace..'KEY:'..key..':'..self:GetCursorPosition()..','; " +
            "self:SetPropagateKeyboardInput(true) end); " +
            "KeyOrderEdit:SetScript('OnArrowPressed',function(self,direction) " +
            "keyTrace=keyTrace..'ARROW:'..direction..':'..self:GetCursorPosition() end)");

        session.Key("LEFT", true);

        Assert.Equal(
            "KEY:LEFT:1,ARROW:LEFT:0",
            session.Lua.Evaluate("return keyTrace"));
    }

    [Fact]
    public void FocusedEditBoxPropagationDoesNotStopItsNativeKeyBehavior()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "OuterKeyFrame=CreateFrame('Frame'); OuterKeyFrame:EnableKeyboard(true); " +
            "outerTrace=''; OuterKeyFrame:SetScript('OnKeyDown',function(_,key) " +
            "outerTrace=key end); " +
            "BlockedKeyEdit=CreateFrame('EditBox'); BlockedKeyEdit:SetText('abc'); " +
            "BlockedKeyEdit:SetCursorPosition(1); BlockedKeyEdit:SetFocus(); " +
            "arrowTrace=''; " +
            "BlockedKeyEdit:SetScript('OnKeyDown',function(self) " +
            "self:SetPropagateKeyboardInput(false) end); " +
            "BlockedKeyEdit:SetScript('OnArrowPressed',function() arrowTrace='arrow' end)");

        session.Key("LEFT", true);

        Assert.Equal(
            "0:arrow:",
            session.Lua.Evaluate(
                "return BlockedKeyEdit:GetCursorPosition()..':'..arrowTrace..':'..outerTrace"));
    }

    [Fact]
    public void FocusedEditBoxScriptsRunAroundOwnedEditingAndEscapeBehavior()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ScriptedEdit=CreateFrame('EditBox'); ScriptedEdit:SetText('ab'); " +
            "ScriptedEdit:SetCursorPosition(2); ScriptedEdit:SetFocus(); trace=''; " +
            "ScriptedEdit:SetScript('OnKeyDown',function(_,key) " +
            "trace=trace..'down:'..key..',' end); " +
            "ScriptedEdit:SetScript('OnKeyUp',function(_,key) " +
            "trace=trace..'up:'..key..',' end); " +
            "ScriptedEdit:SetScript('OnEscapePressed',function(self) " +
            "trace=trace..'escape,'; self:SetText(''); self:Hide() end)");

        session.Key("BACKSPACE", true);
        session.Key("BACKSPACE", false);

        Assert.Equal(
            "a:down:BACKSPACE,up:BACKSPACE,",
            session.Lua.Evaluate("return ScriptedEdit:GetText()..':'..trace"));

        session.Key("ESCAPE", true);

        Assert.Equal(
            ":false:false:down:BACKSPACE,up:BACKSPACE,down:ESCAPE,escape,",
            session.Lua.Evaluate(
                "return table.concat({ScriptedEdit:GetText()," +
                "tostring(ScriptedEdit:IsShown()),tostring(ScriptedEdit:HasFocus()),trace},':')"));
    }

    [Fact]
    public void ControlArrowsUseNativeUnicodeSpaceWordBoundaries()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "WordEdit=CreateFrame('EditBox'); " +
            "WordEdit:SetText('one two'..string.char(9)..'three'); " +
            "WordEdit:SetCursorPosition(13); WordEdit:SetFocus()");

        session.Key("LEFT", true, control: true);
        var previousAcrossTab = session.Lua.Evaluate(
            "return WordEdit:GetCursorPosition()");
        session.Key("LEFT", true, control: true);
        var previousAcrossSpace = session.Lua.Evaluate(
            "return WordEdit:GetCursorPosition()");
        session.Key("RIGHT", true, control: true);
        var nextAcrossSpace = session.Lua.Evaluate(
            "return WordEdit:GetCursorPosition()");
        session.Key("RIGHT", true, control: true);
        var nextAcrossTab = session.Lua.Evaluate(
            "return WordEdit:GetCursorPosition()");

        Assert.Equal("4", previousAcrossTab);
        Assert.Equal("0", previousAcrossSpace);
        Assert.Equal("4", nextAcrossSpace);
        Assert.Equal("13", nextAcrossTab);
    }

    [Fact]
    public void ControlBackspaceAndDeleteRemoveNativeWordRanges()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "DeleteWordEdit=CreateFrame('EditBox'); " +
            "DeleteWordEdit:SetText('one  two three'); " +
            "DeleteWordEdit:SetCursorPosition(8); DeleteWordEdit:SetFocus()");

        session.Key("BACKSPACE", true, control: true);
        var afterBackspace = session.Lua.Evaluate(
            "return DeleteWordEdit:GetText()..':'..DeleteWordEdit:GetCursorPosition()");
        session.Key("DELETE", true, control: true);
        var afterDelete = session.Lua.Evaluate(
            "return DeleteWordEdit:GetText()..':'..DeleteWordEdit:GetCursorPosition()");

        Assert.Equal("one   three:5", afterBackspace);
        Assert.Equal("one  :5", afterDelete);
    }

    [Fact]
    public void NativeControlLetterEditingAliasesMutateTheFocusedEditBox()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ControlEdit=CreateFrame('EditBox'); ControlEdit:SetText('one two'); " +
            "ControlEdit:SetCursorPosition(7); ControlEdit:SetFocus()");

        session.Key("B", true, control: true);
        session.Key("D", true, control: true);
        var afterBackwardDelete = session.Lua.Evaluate(
            "return ControlEdit:GetText()..':'..ControlEdit:GetCursorPosition()");
        session.Key("U", true, control: true);
        var afterDeleteToStart = session.Lua.Evaluate(
            "return ControlEdit:GetText()..':'..ControlEdit:GetCursorPosition()");
        session.Key("A", true, control: true);
        session.TextInput("X");

        Assert.Equal("one tw:6", afterBackwardDelete);
        Assert.Equal(":0", afterDeleteToStart);
        Assert.Equal("X", session.Lua.Evaluate("return ControlEdit:GetText()"));
    }

    [Fact]
    public void ControlNAndPUseTheNativeHistoryAliases()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ControlHistory=CreateFrame('EditBox'); " +
            "ControlHistory:SetHistoryLines(2); " +
            "ControlHistory:AddHistoryLine('one'); " +
            "ControlHistory:AddHistoryLine('two'); ControlHistory:SetFocus()");

        session.Key("P", true, control: true);
        var previous = session.Lua.Evaluate("return ControlHistory:GetText()");
        session.Key("N", true, control: true);
        var next = session.Lua.Evaluate("return ControlHistory:GetText()");

        Assert.Equal("two", previous);
        Assert.Equal("one", next);
    }

    [Fact]
    public void NativeClipboardAliasesCopyCutAndPasteThroughTheSessionProvider()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "ClipboardEdit=CreateFrame('EditBox'); ClipboardEdit:SetMultiLine(true); " +
            "ClipboardEdit:SetText('alpha beta'); ClipboardEdit:HighlightText(6,10); " +
            "ClipboardEdit:SetFocus()");

        session.Key("C", true, control: true);
        Assert.Equal("beta", session.ClipboardText);
        Assert.Equal(
            "alpha beta",
            session.Lua.Evaluate("return ClipboardEdit:GetText()"));

        session.Key("X", true, control: true);
        Assert.Equal(
            "alpha ",
            session.Lua.Evaluate("return ClipboardEdit:GetText()"));

        session.ClipboardText = "|\tZ\n";
        session.Key("V", true, control: true);

        Assert.Equal(
            "alpha ||    Z\n",
            session.Lua.Evaluate("return ClipboardEdit:GetText()"));
    }

    [Fact]
    public void SecureCopyAndPasteLocksFollowTheNativeOneWayGates()
    {
        using var session = new EmulatorSession();
        session.ClipboardText = "outside";
        session.Lua.Evaluate(
            "SecureClipboard=CreateFrame('EditBox'); SecureClipboard:SetText('secret'); " +
            "SecureClipboard:HighlightText(); SecureClipboard:SetSecureText(true); " +
            "SecureClipboard:SetFocus()");

        session.Key("X", true, control: true);

        Assert.Equal("outside", session.ClipboardText);
        Assert.Equal(
            "secret",
            session.Lua.Evaluate("return SecureClipboard:GetText()"));

        session.Lua.Evaluate(
            "SecureClipboard:SetSecureText(false); " +
            "SecureClipboard:SetSecurityDisablePaste(); " +
            "SecureClipboard:SetCursorPosition(6)");
        session.ClipboardText = " blocked";
        session.Key("V", true, control: true);

        Assert.Equal(
            "secret",
            session.Lua.Evaluate("return SecureClipboard:GetText()"));
    }

    [Fact]
    public void InsertAndDeleteUseTheNativeClipboardModifierPrecedence()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "InsertClipboard=CreateFrame('EditBox'); " +
            "InsertClipboard:SetText('one two'); InsertClipboard:HighlightText(4,7); " +
            "InsertClipboard:SetFocus()");

        session.Key("INSERT", true, control: true);
        Assert.Equal("two", session.ClipboardText);

        session.Key("DELETE", true, shift: true);
        Assert.Equal(
            "one ",
            session.Lua.Evaluate("return InsertClipboard:GetText()"));

        session.Key("INSERT", true, shift: true);
        Assert.Equal(
            "one two",
            session.Lua.Evaluate("return InsertClipboard:GetText()"));

        session.Key("DELETE", true, shift: true);
        Assert.Equal(
            "one two",
            session.Lua.Evaluate("return InsertClipboard:GetText()"));
    }

    [Fact]
    public void MultilineArrowsPreserveTheVisibleColumnAcrossWrappedLines()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "WrappedEdit=CreateFrame('EditBox','WrappedEdit'); WrappedEdit:SetMultiLine(true); " +
            "WrappedEdit:SetText('abcdef'); WrappedEdit:SetCursorPosition(2); " +
            "WrappedEdit:SetFocus()");
        var edit = session.Ui.Find("WrappedEdit")!;
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(0, 0, 20, 30));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(1, 10, 20, 30));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(2, 20, 20, 30));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(3, 30, 20, 30));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(3, 0, 10, 20));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(4, 10, 10, 20));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(5, 20, 10, 20));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(6, 30, 10, 20));

        session.Key("DOWN", true);
        var lower = session.Lua.Evaluate("return WrappedEdit:GetCursorPosition()");
        session.Key("UP", true);
        var upper = session.Lua.Evaluate("return WrappedEdit:GetCursorPosition()");

        Assert.Equal("5", lower);
        Assert.Equal("2", upper);
    }

    [Fact]
    public void MultilineEnterInsertsNewlineOnlyWithoutAnEnterHandler()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "EnterEdit=CreateFrame('EditBox'); EnterEdit:SetMultiLine(true); " +
            "EnterEdit:SetText('ab'); EnterEdit:SetCursorPosition(1); " +
            "EnterEdit:SetFocus()");

        session.Key("ENTER", true);
        var inserted = session.Lua.Evaluate("return EnterEdit:GetText()");

        session.Lua.Evaluate(
            "enterCount=0; EnterEdit:SetScript('OnEnterPressed',function() " +
            "enterCount=enterCount+1 end); EnterEdit:SetCursorPosition(2)");
        session.Key("ENTER", true);

        Assert.Equal("a\nb", inserted);
        Assert.Equal(
            "a\nb:1",
            session.Lua.Evaluate(
                "return EnterEdit:GetText()..':'..enterCount"));
    }

    [Fact]
    public void AltUpAndDownNavigateTheNativeHistoryRingProgrammatically()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "HistoryEdit=CreateFrame('EditBox'); HistoryEdit:SetHistoryLines(3); " +
            "HistoryEdit:AddHistoryLine('one'); HistoryEdit:AddHistoryLine('two'); " +
            "HistoryEdit:AddHistoryLine('three'); changed=''; " +
            "HistoryEdit:SetScript('OnTextChanged',function(_,userInput) " +
            "changed=changed..tostring(userInput)..',' end); HistoryEdit:SetFocus()");

        session.Key("UP", true, alt: true);
        var latest = session.Lua.Evaluate("return HistoryEdit:GetText()");
        session.Key("UP", true, alt: true);
        var previous = session.Lua.Evaluate("return HistoryEdit:GetText()");
        session.Key("DOWN", true, alt: true);

        Assert.Equal("three", latest);
        Assert.Equal("two", previous);
        Assert.Equal(
            "three:false,false,false,",
            session.Lua.Evaluate("return HistoryEdit:GetText()..':'..changed"));
    }

    [Fact]
    public void MouseDragUsesRendererGlyphStopsToSelectText()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "MouseEdit=CreateFrame('EditBox','MouseEdit',UIParent); " +
            "MouseEdit:SetPoint('BOTTOMLEFT',100,100); MouseEdit:SetSize(200,30); " +
            "MouseEdit:EnableMouse(true); MouseEdit:SetText('abcd'); MouseEdit:ClearFocus()");
        var edit = session.Ui.Find("MouseEdit")!;
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(0, 100, 100, 130));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(1, 110, 100, 130));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(2, 120, 100, 130));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(3, 130, 100, 130));
        edit.EditBoxCaretStops.Add(new UiEditBoxCaretStop(4, 140, 100, 130));

        session.MouseMove(111, 115);
        session.MouseButton("LeftButton", true);
        session.MouseMove(129, 115);
        session.MouseButton("LeftButton", false);
        session.TextInput("X");

        Assert.Equal(
            "aXd:2",
            session.Lua.Evaluate(
                "return MouseEdit:GetText()..':'..MouseEdit:GetCursorPosition()"));
    }
}
