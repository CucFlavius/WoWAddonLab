namespace WoWAddonLab.Tests;

public sealed class FrameXmlIntrinsicContractTests
{
    [Fact]
    public void EmptyXmlScriptReplacesTheInheritedHandler()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-empty-xml-script-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "Scripts.xml"),
                "<Ui><Frame name=\"InheritedUpdateTemplate\" virtual=\"true\">" +
                "<Scripts><OnUpdate>InheritedUpdateCount=InheritedUpdateCount+1" +
                "</OnUpdate></Scripts></Frame>" +
                "<Frame name=\"EmptyUpdateFrame\" inherits=\"InheritedUpdateTemplate\">" +
                "<Scripts><OnUpdate></OnUpdate></Scripts></Frame></Ui>");
            File.WriteAllText(
                Path.Combine(root, "Scripts.lua"),
                "InheritedUpdateCount=0");
            File.WriteAllText(
                Path.Combine(root, "Scripts.toc"),
                "## Interface: 1\n## Title: Empty Script\nScripts.lua\nScripts.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);
            session.Tick(1.0 / 60.0);

            Assert.Equal(
                "0:function",
                session.Lua.Evaluate(
                    "return InheritedUpdateCount..':'..type(EmptyUpdateFrame:GetScript('OnUpdate'))"));
            Assert.Null(session.LastError);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void RuntimeScriptsPreserveIntrinsicCallOrder()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-intrinsic-script-chain-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "Scripts.xml"),
                "<Ui><Button name=\"ScriptChainIntrinsic\" intrinsic=\"true\">" +
                "<Scripts>" +
                "<OnClick intrinsicOrder=\"precall\">table.insert(ScriptTrace,'before')</OnClick>" +
                "<OnClick intrinsicOrder=\"postcall\">table.insert(ScriptTrace,'after')</OnClick>" +
                "</Scripts></Button>" +
                "<ScriptChainIntrinsic name=\"ScriptChainButton\"/></Ui>");
            File.WriteAllText(
                Path.Combine(root, "Scripts.lua"),
                "ScriptTrace={}; " +
                "ScriptChainButton:SetScript('OnClick',function() " +
                "table.insert(ScriptTrace,'normal') end); " +
                "ScriptChainButton:HookScript('OnClick',function() " +
                "table.insert(ScriptTrace,'hook') end); " +
                "ScriptChainButton:Click(); " +
                "ScriptTraceWithHandler=table.concat(ScriptTrace,','); " +
                "ScriptTrace={}; ScriptChainButton:SetScript('OnClick',nil); " +
                "ScriptChainButton:Click(); " +
                "ScriptTraceWithoutHandler=table.concat(ScriptTrace,',')");
            File.WriteAllText(
                Path.Combine(root, "Scripts.toc"),
                "## Interface: 1\n## Title: Script Chain\nScripts.xml\nScripts.lua\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "before,normal,hook,after|before,hook,after",
                session.Lua.Evaluate(
                    "return ScriptTraceWithHandler..'|'..ScriptTraceWithoutHandler"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void InlineTopLevelScriptsExecuteWithAddonArguments()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-inline-script-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "Inline.xml"),
                "<Ui><Script>local name, private = ...; " +
                "INLINE_XML_RESULT = name .. ':' .. type(private)</Script></Ui>");
            File.WriteAllText(
                Path.Combine(root, "InlineScript.toc"),
                "## Interface: 1\n## Title: Inline Script\nInline.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                $"{Path.GetFileName(root)}:table",
                session.Lua.Evaluate("return INLINE_XML_RESULT"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void XmlProtectedAttributeAppliesThroughInheritedTemplates()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"wow-addon-lab-protected-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "Protected.xml"),
                "<Ui><Frame name=\"SecureFrameTemplate\" protected=\"true\" virtual=\"true\"/>" +
                "<Frame name=\"InheritedSecureFrame\" inherits=\"SecureFrameTemplate\"/>" +
                "<Frame name=\"OrdinaryFrame\"/></Ui>");
            File.WriteAllText(
                Path.Combine(root, "Protected.toc"),
                "## Interface: 1\n## Title: Protected XML\nProtected.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "true:true:false:false",
                session.Lua.Evaluate(
                    "local secure,explicit=InheritedSecureFrame:IsProtected(); " +
                    "local ordinary,ordinaryExplicit=OrdinaryFrame:IsProtected(); " +
                    "return table.concat({tostring(secure),tostring(explicit)," +
                    "tostring(ordinary),tostring(ordinaryExplicit)},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ConcreteXmlFrameStrataPropagatesThroughInheritedFrameChildren()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-frame-strata-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "FrameStrata.xml"),
                "<Ui><Frame name=\"FrameStrataTemplate\" virtual=\"true\">" +
                "<Frames><Frame parentKey=\"TemplateChild\">" +
                "<Frames><Frame parentKey=\"TemplateGrandchild\"/></Frames>" +
                "</Frame></Frames></Frame>" +
                "<Frame name=\"XmlHighStrataFrame\" " +
                "inherits=\"FrameStrataTemplate\" frameStrata=\"HIGH\"/>" +
                "</Ui>");
            File.WriteAllText(
                Path.Combine(root, "FrameStrata.toc"),
                "## Interface: 1\n## Title: Frame Strata\nFrameStrata.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "HIGH:HIGH:HIGH:HIGH",
                session.Lua.Evaluate(
                    "local child=XmlHighStrataFrame.TemplateChild; " +
                    "local dynamic=CreateFrame('Frame',nil,child); " +
                    "return table.concat({XmlHighStrataFrame:GetFrameStrata()," +
                    "child:GetFrameStrata()," +
                    "child.TemplateGrandchild:GetFrameStrata()," +
                    "dynamic:GetFrameStrata()},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void EditBoxAutoFocusAttributeIsInheritedAndCanBeOverridden()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-editbox-autofocus-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "EditBoxAutoFocus.xml"),
                "<Ui><EditBox name=\"NonFocusingEditBoxTemplate\" " +
                "autoFocus=\"false\" virtual=\"true\"/>" +
                "<EditBox name=\"InheritedNonFocusingEditBox\" " +
                "inherits=\"NonFocusingEditBoxTemplate\" hidden=\"true\"/>" +
                "<EditBox name=\"OverriddenFocusingEditBox\" " +
                "inherits=\"NonFocusingEditBoxTemplate\" autoFocus=\"true\" " +
                "hidden=\"true\"/></Ui>");
            File.WriteAllText(
                Path.Combine(root, "EditBoxAutoFocus.toc"),
                "## Interface: 1\n## Title: EditBox AutoFocus\n" +
                "EditBoxAutoFocus.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "false:false:true:true",
                session.Lua.Evaluate(
                    "InheritedNonFocusingEditBox:Show(); " +
                    "local inheritedFocused=InheritedNonFocusingEditBox:HasFocus(); " +
                    "OverriddenFocusingEditBox:Show(); " +
                    "return table.concat({" +
                    "tostring(InheritedNonFocusingEditBox:IsAutoFocus())," +
                    "tostring(inheritedFocused)," +
                    "tostring(OverriddenFocusingEditBox:IsAutoFocus())," +
                    "tostring(OverriddenFocusingEditBox:HasFocus())},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AnchorOffsetElementAcceptsDirectAndDimensionChildForms()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-anchor-offset-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "AnchorOffset.xml"),
                "<Ui><Frame name=\"AnchorOffsetParent\">" +
                "<Size x=\"200\" y=\"100\"/>" +
                "<Anchors><Anchor point=\"BOTTOMLEFT\" x=\"100\" y=\"100\"/></Anchors>" +
                "<Frames>" +
                "<Frame name=\"DirectAnchorOffset\"><Size x=\"20\" y=\"10\"/>" +
                "<Anchors><Anchor point=\"BOTTOMLEFT\" relativeTo=\"AnchorOffsetParent\" " +
                "relativePoint=\"BOTTOMLEFT\"><Offset x=\"19\" y=\"-30\"/>" +
                "</Anchor></Anchors></Frame>" +
                "<Frame name=\"DimensionAnchorOffset\"><Size x=\"20\" y=\"10\"/>" +
                "<Anchors><Anchor point=\"BOTTOMLEFT\" relativeTo=\"AnchorOffsetParent\" " +
                "relativePoint=\"BOTTOMLEFT\"><Offset><AbsDimension x=\"7\" y=\"-8\"/>" +
                "</Offset></Anchor></Anchors></Frame>" +
                "</Frames></Frame></Ui>");
            File.WriteAllText(
                Path.Combine(root, "AnchorOffset.toc"),
                "## Interface: 1\n## Title: Anchor Offset\nAnchorOffset.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            var direct = Assert.Single(session.Ui.Find("DirectAnchorOffset")!.Anchors);
            var dimension = Assert.Single(session.Ui.Find("DimensionAnchorOffset")!.Anchors);
            Assert.Equal((19f, -30f), (direct.X, direct.Y));
            Assert.Equal((7f, -8f), (dimension.X, dimension.Y));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFrameTemplateAppliesNormalFontToItsFinalButtonText()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-button-font-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "ButtonFont.xml"),
                "<Ui><Font name=\"TemplateButtonFont\" font=\"Fonts\\FRIZQT__.TTF\" height=\"10\"/>" +
                "<Button name=\"TemplateButton\" virtual=\"true\">" +
                "<ButtonText parentKey=\"Text\"/><NormalFont style=\"TemplateButtonFont\"/>" +
                "</Button></Ui>");
            File.WriteAllText(
                Path.Combine(root, "Create.lua"),
                "CreatedTemplateButton=CreateFrame('Button','CreatedTemplateButton'," +
                "UIParent,'TemplateButton'); CreatedTemplateButton:SetText('Ready')");
            File.WriteAllText(
                Path.Combine(root, "ButtonFont.toc"),
                "## Interface: 1\n## Title: Button Font\nButtonFont.xml\nCreate.lua\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "Fonts\\FRIZQT__.TTF:10:TemplateButtonFont:Ready",
                session.Lua.Evaluate(
                    "local text=CreatedTemplateButton:GetFontString(); " +
                    "local file,height=text:GetFont(); " +
                    "return table.concat({file,height,text:GetFontObject():GetName()," +
                    "CreatedTemplateButton:GetText()},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void EditBoxEmbeddedFontStringConfiguresItsFontInstance()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-editbox-font-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "EditBoxFont.xml"),
                "<Ui><Font name=\"EmbeddedEditFont\" font=\"Fonts\\ARIALN.TTF\" height=\"15\" virtual=\"true\"/>" +
                "<EditBox name=\"EmbeddedFontEditBox\"><FontString inherits=\"EmbeddedEditFont\"/></EditBox></Ui>");
            File.WriteAllText(
                Path.Combine(root, "EditBoxFont.toc"),
                "## Interface: 1\n## Title: EditBox Font\nEditBoxFont.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "Fonts\\ARIALN.TTF:15:Font:LEFT:MIDDLE",
                session.Lua.Evaluate(
                    "local file,height=EmbeddedFontEditBox:GetFont(); " +
                    "return table.concat({file,height,EmbeddedFontEditBox:GetFontObject():GetObjectType()," +
                    "EmbeddedFontEditBox:GetJustifyH(),EmbeddedFontEditBox:GetJustifyV()},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void EditBoxXmlTextInsetsApplyThroughInheritedTemplates()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-editbox-insets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "EditBoxInsets.xml"),
                "<Ui><EditBox name=\"InsetSearchTemplate\" virtual=\"true\">" +
                "<TextInsets left=\"16\" right=\"20\" top=\"2\" bottom=\"3\"/>" +
                "</EditBox><EditBox name=\"InheritedInsetSearch\" " +
                "inherits=\"InsetSearchTemplate\"/></Ui>");
            File.WriteAllText(
                Path.Combine(root, "EditBoxInsets.toc"),
                "## Interface: 1\n## Title: EditBox Insets\nEditBoxInsets.xml\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "16:20:2:3",
                session.Lua.Evaluate(
                    "local l,r,t,b=InheritedInsetSearch:GetTextInsets(); " +
                    "return table.concat({l,r,t,b},':')"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CreateFrameResolvesIntrinsicTagToItsDeclaredNativeBase()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"dungeonmire-contained-alert-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "ContainedAlert.lua"),
                "ContainedAlertFrameMixin={" +
                "OnLoad=function(self) self.intrinsicLoaded=true end," +
                "SetAlertContainer=function(self,container) self.alertContainer=container end," +
                "GetAlertContainer=function(self) return self.alertContainer end}");
            File.WriteAllText(
                Path.Combine(root, "ContainedAlert.xml"),
                "<Ui><Button name=\"ContainedAlertFrame\" intrinsic=\"true\" " +
                "mixin=\"ContainedAlertFrameMixin\">" +
                "<Scripts><OnLoad method=\"OnLoad\" intrinsicOrder=\"precall\"/></Scripts>" +
                "</Button></Ui>");
            File.WriteAllText(
                Path.Combine(root, "Create.lua"),
                "CreatedContainedAlert=CreateFrame('ContainedAlertFrame'," +
                "'CreatedContainedAlert',UIParent); " +
                "CreatedContainedAlert:SetText('Alert'); " +
                "CreatedContainedAlert:SetAlertContainer(UIParent)");
            File.WriteAllText(
                Path.Combine(root, "ContainedAlert.toc"),
                "## Interface: 1\n## Title: Contained Alert\n" +
                "ContainedAlert.lua\nContainedAlert.xml\nCreate.lua\n");

            using var session = new EmulatorSession();
            session.Load(root);

            Assert.Equal(
                "Button:true:Alert:true:true",
                session.Lua.Evaluate(
                    "return table.concat({CreatedContainedAlert:GetObjectType()," +
                    "tostring(CreatedContainedAlert:IsEnabled())," +
                    "CreatedContainedAlert:GetText()," +
                    "tostring(CreatedContainedAlert:GetAlertContainer()==UIParent)," +
                    "tostring(CreatedContainedAlert.intrinsicLoaded)},':')"));
            Assert.Equal("Button", session.Ui.Find("CreatedContainedAlert")!.ObjectType);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
