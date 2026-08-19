using System.Reflection;

namespace WoWAddonLab.Tests;

public sealed class FontContractTests
{
    [Fact]
    public void NativeTwentyFourMethodSurfaceUsesOnlyScriptObjectBase()
    {
        var coreAssembly = typeof(EmulatorSession).Assembly;
        var apiType = coreAssembly.GetType(
            "WoWAddonLab.Emulator.Lua.WowWidgetApi",
            throwOnError: true)!;
        var ownedMethods = Assert.IsType<string[]>(
            apiType.GetField(
                "Font",
                BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null));
        Assert.Equal(
            [
                "CopyFontObject", "GetAlpha", "GetFont", "GetFontHeight",
                "GetFontObject", "GetFontObjectForAlphabet",
                "GetIndentedWordWrap", "GetJustifyH", "GetJustifyV",
                "GetShadowColor", "GetShadowOffset", "GetSpacing", "GetTextColor",
                "SetAlpha", "SetFont", "SetFontHeight", "SetFontObject",
                "SetIndentedWordWrap", "SetJustifyH", "SetJustifyV",
                "SetShadowColor", "SetShadowOffset", "SetSpacing", "SetTextColor"
            ],
            ownedMethods);

        using var session = new EmulatorSession();
        Assert.Equal(
            "function:function:function:nil:nil:nil:nil",
            session.Lua.Evaluate(
                "local font=CreateFont('NativeSurfaceFont'); " +
                "return table.concat({type(font.GetName),type(font.GetObjectType)," +
                "type(font.GetFontObjectForAlphabet),type(font.GetParent)," +
                "type(font.ClearParentKey),type(font.AddLine)," +
                "type(font.SetText)},':')"));
    }

    [Fact]
    public void FontAlphabetGetterValidatesTheNativeFiveValueEnum()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local parent=CreateFont('AlphabetParent'); " +
                "local child=CreateFont('AlphabetChild'); child:SetFontObject(parent); " +
                "local values={'Roman','Korean','SimplifiedChinese'," +
                "'TraditionalChinese','Russian'}; local result={}; " +
                "for _,name in ipairs(values) do " +
                "result[#result+1]=tostring(child:GetFontObjectForAlphabet(name)==parent) end; " +
                "result[#result+1]=tostring(" +
                "pcall(child.GetFontObjectForAlphabet,child,'Unknown')); " +
                "return table.concat(result,':')"));
    }

    [Fact]
    public void CreateFontFamilyOwnsFiveAlphabetFontsAndSelectsTheClientAlphabet()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:Roman.ttf:11:Russian.ttf:15:nil:true",
            session.Lua.Evaluate(
                "local family=CreateFontFamily('NativeFamily',{ " +
                "{alphabet='Roman',file='Roman.ttf',height=11,flags=''}," +
                "{alphabet='Korean',file='Korean.ttf',height=12,flags='OUTLINE'}," +
                "{alphabet='SimplifiedChinese',file='Simplified.ttf',height=13,flags=''}," +
                "{alphabet='TraditionalChinese',file='Traditional.ttf',height=14,flags=''}," +
                "{alphabet='Russian',file='Russian.ttf',height=15,flags=''} }); " +
                "local roman=family:GetFontObjectForAlphabet('Roman'); " +
                "local russian=family:GetFontObjectForAlphabet('Russian'); " +
                "local child=CreateFont('NativeFamilyChild'); child:SetFontObject(family); " +
                "local romanFile,romanHeight=roman:GetFont(); " +
                "local russianFile,russianHeight=russian:GetFont(); " +
                "return table.concat({tostring(family:GetFontObject()==roman)," +
                "tostring(roman~=russian),tostring(child:GetFontObjectForAlphabet('Roman')==roman)," +
                "romanFile,romanHeight,russianFile,russianHeight,type(roman:GetName())," +
                "tostring(NativeFamily==family)},':')"));
    }

    [Fact]
    public void CopyFontObjectDeepCopiesAlphabetMembersButRetainsTheNativeParentLink()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:0.200:0.800",
            session.Lua.Evaluate(
                "local family=CreateFontFamily('CopySourceFamily',{ " +
                "{alphabet='Roman',file='Roman.ttf',height=11,flags=''}," +
                "{alphabet='Korean',file='Korean.ttf',height=12,flags=''}," +
                "{alphabet='SimplifiedChinese',file='Simplified.ttf',height=13,flags=''}," +
                "{alphabet='TraditionalChinese',file='Traditional.ttf',height=14,flags=''}," +
                "{alphabet='Russian',file='Russian.ttf',height=15,flags=''} }); " +
                "local original=family:GetFontObjectForAlphabet('Roman'); " +
                "original:SetTextColor(0.2,0.3,0.4,1); " +
                "local copy=CreateFont('CopiedFamily'); copy:CopyFontObject(family); " +
                "local copied=copy:GetFontObjectForAlphabet('Roman'); " +
                "copied:SetTextColor(0.8,0.7,0.6,1); " +
                "local originalR=original:GetTextColor(); local copiedR=copied:GetTextColor(); " +
                "return table.concat({tostring(copied~=original)," +
                "tostring(copy:GetFontObject()==original)," +
                "tostring(copy:GetFontObjectForAlphabet('Russian')~=" +
                "family:GetFontObjectForAlphabet('Russian'))," +
                "string.format('%.3f',originalR),string.format('%.3f',copiedR)},':')"));
    }

    [Fact]
    public void CreateFontFamilyRejectsWrongCountsAndDuplicateAlphabets()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "local short=pcall(CreateFontFamily,'ShortFamily',{}); " +
                "local duplicate=pcall(CreateFontFamily,'DuplicateFamily',{ " +
                "{alphabet='Roman',file='1',height=1,flags=''}," +
                "{alphabet='Roman',file='2',height=1,flags=''}," +
                "{alphabet='SimplifiedChinese',file='3',height=1,flags=''}," +
                "{alphabet='TraditionalChinese',file='4',height=1,flags=''}," +
                "{alphabet='Russian',file='5',height=1,flags=''} }); " +
                "return tostring(short)..':'..tostring(duplicate)"));
    }

    [Fact]
    public void FontGlobalsReuseNamedFontsEnumerateTheRegistryAndReturnNativeInfoShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:true:true:1:function:17:OUTLINE, THICKOUTLINE, MONOCHROME:function:2:-3:" +
            "true:false:false:true:true:true:true:0:7",
            session.Lua.Evaluate(
                "local missingCreate=pcall(CreateFont); " +
                "local first=CreateFont('RegistryFont'); " +
                "first:SetFont('Fonts\\\\ARIALN.TTF',17.75,'thickoutline,monochrome'); " +
                "first:SetTextColor(.2,.4,.6,.8); " +
                "first:SetShadowColor(.1,.3,.5,.7); first:SetShadowOffset(2,-3); " +
                "local second=CreateFont('RegistryFont'); " +
                "local caseVariant=CreateFont('registryfont'); local names=GetFonts(); " +
                "local occurrences=0; for _,name in ipairs(names) do " +
                "if name=='RegistryFont' then occurrences=occurrences+1 end end; " +
                "local info=GetFontInfo(first); " +
                "local noShadowInfo=GetFontInfo(CreateFont('NoShadowFont')); " +
                "local noShadow=noShadowInfo.shadow==nil; " +
                "local missingInfo=pcall(GetFontInfo); " +
                "local namedOk,namedInfo=pcall(GetFontInfo,'RegistryFont'); " +
                "local unknownName=GetFontInfo('MissingRegistryFont')==nil; " +
                "local numeric=CreateFont(7); " +
                "return table.concat({tostring(missingCreate),tostring(first==second)," +
                "tostring(first==caseVariant)," +
                "occurrences,type(info.color.GetRGBA),info.height,info.outline," +
                "type(info.shadow.color.GetRGBA),info.shadow.x,info.shadow.y," +
                "tostring(info.fontObject==first),tostring(info.canBeUserScaled)," +
                "tostring(missingInfo),tostring(namedOk)," +
                "tostring(namedInfo.fontObject==first),tostring(unknownName)," +
                "tostring(noShadow)," +
                "noShadowInfo.height,numeric:GetName()},':')"));
    }

    [Fact]
    public void FontScalarMethodsUseOwnedFontStateAndNativeReturnContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "nil:0:0:0:0.400000:0.200000:0.301961:0.400000:0.250980:" +
            "0.501961:-3.5:17.0:21.5",
            session.Lua.Evaluate(
                "local empty=CreateFont('EmptyScalarFont'); " +
                "local emptyFile,emptyHeight=empty:GetFont(); " +
                "empty:SetFontHeight(33); local _,emptyHeightAfter=empty:GetFont(); " +
                "local font=CreateFont('ScalarFont'); " +
                "local setReturns=select('#',font:SetFont('Fonts\\\\ARIALN.TTF',17,'')); " +
                "font:SetTextColor(.2,.3,.4,.4); local alphaFromColor=font:GetAlpha(); " +
                "font:SetAlpha(.25); local r,g,b,a=font:GetTextColor(); " +
                "local text=UIParent:CreateFontString(); text:SetFontObject(font); " +
                "font:SetAlpha(.5); local _,_,_,inheritedAlpha=text:GetTextColor(); " +
                "font:SetSpacing(-3.5); font:SetFontHeight(-5); " +
                "local retained=font:GetFontHeight(); font:SetFontHeight(21.5); " +
                "return string.format('%s:%g:%g:%d:%.6f:%.6f:%.6f:%.6f:%.6f:'.." +
                "'%.6f:%.1f:%.1f:%.1f',tostring(emptyFile),emptyHeight," +
                "emptyHeightAfter,setReturns,alphaFromColor,r,g,b,a,inheritedAlpha," +
                "font:GetSpacing(),retained,font:GetFontHeight())"));
    }

    [Fact]
    public void SetToDefaultsUsesTheNativeScriptsOnlyFontReset()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "Fonts\\ARIALN.TTF:21:OUTLINE:true:0.200:4:true:true",
            session.Lua.Evaluate(
                "local parent=CreateFont('ResetParent'); " +
                "local font=CreateFont('ResetFont'); font:SetFontObject(parent); " +
                "font:SetFont('Fonts\\\\ARIALN.TTF',21,'OUTLINE'); " +
                "font:SetTextColor(.2,.3,.4,.5); font:SetSpacing(4); " +
                "local family=CreateFontFamily('ResetFamily',{ " +
                "{alphabet='Roman',file='Roman.ttf',height=11,flags=''}," +
                "{alphabet='Korean',file='Korean.ttf',height=12,flags=''}," +
                "{alphabet='SimplifiedChinese',file='Simplified.ttf',height=13,flags=''}," +
                "{alphabet='TraditionalChinese',file='Traditional.ttf',height=14,flags=''}," +
                "{alphabet='Russian',file='Russian.ttf',height=15,flags=''} }); " +
                "local roman=family:GetFontObjectForAlphabet('Roman'); " +
                "font:SetToDefaults(); family:SetToDefaults(); " +
                "local file,height,flags=font:GetFont(); local red=font:GetTextColor(); " +
                "return table.concat({file,height,flags," +
                "tostring(font:GetFontObject()==parent),string.format('%.3f',red)," +
                "font:GetSpacing(),tostring(family:GetFontObject()==roman)," +
                "tostring(family:GetFontObjectForAlphabet('Roman')==roman)},':')"));
    }
}
