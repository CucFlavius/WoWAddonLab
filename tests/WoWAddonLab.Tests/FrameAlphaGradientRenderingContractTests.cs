using System.Numerics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class FrameAlphaGradientRenderingContractTests
{
    [Fact]
    public void ShaderEquationMultipliesFourSaturatedEdgeDistances()
    {
        var gradient = new UiFrameAlphaGradientParameters(
            new Vector4(20, 10, 25, 5),
            new Vector4(10, 20, 110, 120));

        Assert.Equal(.25f, gradient.Evaluate(new Vector2(20, 25)), 5);
        Assert.Equal(1, gradient.Evaluate(new Vector2(60, 60)));
        Assert.Equal(0, gradient.Evaluate(new Vector2(10, 20)));
    }

    [Fact]
    public void ZeroAndNegativeWidthsFollowDirect3DSaturateBehavior()
    {
        var zero = new UiFrameAlphaGradientParameters(
            Vector4.Zero,
            new Vector4(10, 20, 110, 120));
        var negative = new UiFrameAlphaGradientParameters(
            new Vector4(-10, 0, 0, 0),
            new Vector4(10, 20, 110, 120));

        Assert.Equal(1, zero.Evaluate(new Vector2(60, 60)));
        Assert.Equal(0, zero.Evaluate(new Vector2(10, 60)));
        Assert.Equal(0, negative.Evaluate(new Vector2(60, 60)));
    }

    [Fact]
    public void IndependentBatchGradientOwnsDescendantRenderables()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local parent=CreateFrame('Frame','GradientParent',UIParent); " +
            "local child=CreateFrame('Frame','GradientChild',parent); " +
            "child:CreateTexture('GradientTexture'); " +
            "parent:SetFlattensRenderLayers(true); " +
            "parent:SetAlphaGradient(0,{x=3,y=4}); " +
            "parent:SetAlphaGradient(1,{x=5,y=6})");

        var parent = session.Ui.Find("GradientParent")!;
        var child = session.Ui.Find("GradientChild")!;
        var texture = session.Ui.Find("GradientTexture")!;

        var inherited = UiFrameAlphaGradient.Resolve(session.Ui, texture);
        Assert.NotNull(inherited);
        Assert.Same(parent, inherited.Value.Owner);
        Assert.Equal(new Vector2(3, 4), inherited.Value.LeadingEdge);
        Assert.Equal(new Vector2(5, 6), inherited.Value.TrailingEdge);

        child.FlattensRenderLayers = true;
        Assert.Null(UiFrameAlphaGradient.Resolve(session.Ui, texture));

        child.HasFrameAlphaGradient = true;
        child.FrameAlphaGradientEdges[0] = new Vector2(7, 8);
        var overridden = UiFrameAlphaGradient.Resolve(session.Ui, texture);
        Assert.NotNull(overridden);
        Assert.Same(child, overridden.Value.Owner);
        Assert.Equal(new Vector2(7, 8), overridden.Value.LeadingEdge);
    }

    [Fact]
    public void NonIndependentGradientDoesNotReplaceOwningBatchGradient()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local parent=CreateFrame('Frame','BatchGradientParent',UIParent); " +
            "local child=CreateFrame('Frame','BatchGradientChild',parent); " +
            "child:CreateTexture('BatchGradientTexture'); " +
            "parent:SetFlattensRenderLayers(true); " +
            "parent:SetAlphaGradient(0,{x=3,y=4}); " +
            "child:SetAlphaGradient(0,{x=7,y=8})");

        var parent = session.Ui.Find("BatchGradientParent")!;
        var child = session.Ui.Find("BatchGradientChild")!;
        var texture = session.Ui.Find("BatchGradientTexture")!;

        Assert.True(child.HasFrameAlphaGradient);
        var resolved = UiFrameAlphaGradient.Resolve(session.Ui, texture);
        Assert.NotNull(resolved);
        Assert.Same(parent, resolved.Value.Owner);

        parent.FlattensRenderLayers = false;
        Assert.Null(UiFrameAlphaGradient.Resolve(session.Ui, texture));
    }

    [Fact]
    public void EffectiveFlatteningIsOwnIndependentBatchStateNotAncestorState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local parent=CreateFrame('Frame','FlattenParent',UIParent); " +
            "CreateFrame('Frame','FlattenChild',parent)");

        var parent = session.Ui.Find("FlattenParent")!;
        var child = session.Ui.Find("FlattenChild")!;

        parent.FlattensRenderLayers = true;
        Assert.False(session.Ui.EffectivelyFlattensRenderLayers(child));

        child.ClipsChildren = true;
        Assert.True(session.Ui.EffectivelyFlattensRenderLayers(child));
        child.ClipsChildren = false;
        child.Toplevel = true;
        Assert.True(session.Ui.EffectivelyFlattensRenderLayers(child));
        child.Toplevel = false;
        parent.ScrollChildId = child.Id;
        Assert.True(session.Ui.EffectivelyFlattensRenderLayers(child));
        parent.ScrollChildId = null;
        child.WindowReference = 2;
        parent.WindowReference = 1;
        Assert.True(session.Ui.EffectivelyFlattensRenderLayers(child));
    }

    [Fact]
    public void FrameBufferRenderPlanMakesNestedBatchesContiguous()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local outer=CreateFrame('Frame','OuterBuffer',UIParent); " +
            "outer:SetAllPoints(UIParent); outer:SetIsFrameBuffer(true); " +
            "local outerTexture=outer:CreateTexture('OuterTexture'); " +
            "outerTexture:SetAllPoints(outer); " +
            "local ordinary=CreateFrame('Frame','OrdinaryChild',outer); " +
            "ordinary:SetAllPoints(outer); " +
            "local ordinaryTexture=ordinary:CreateTexture('OrdinaryTexture'); " +
            "ordinaryTexture:SetAllPoints(ordinary); " +
            "local inner=CreateFrame('Frame','InnerBuffer',ordinary); " +
            "inner:SetAllPoints(ordinary); inner:SetIsFrameBuffer(true); " +
            "local innerTexture=inner:CreateTexture('InnerTexture'); " +
            "innerTexture:SetAllPoints(inner); " +
            "local sibling=CreateFrame('Frame','RootSibling',UIParent); " +
            "sibling:SetAllPoints(UIParent); " +
            "local siblingTexture=sibling:CreateTexture('SiblingTexture'); " +
            "siblingTexture:SetAllPoints(sibling)");

        var plan = UiRenderBatchPlan.Build(
            session.Ui,
            session.Ui.RenderOrder().ToArray());
        var outer = Assert.Single(
            Descendants(plan).OfType<UiFrameBufferBatchEntry>(),
            value => value.Frame.Name == "OuterBuffer");
        var inner = Assert.Single(
            outer.Entries.OfType<UiFrameBufferBatchEntry>());

        Assert.Equal("InnerBuffer", inner.Frame.Name);
        Assert.Contains(
            outer.Entries.OfType<UiRenderObjectEntry>(),
            value => value.Value.Name == "OuterTexture");
        Assert.Contains(
            outer.Entries.OfType<UiRenderObjectEntry>(),
            value => value.Value.Name == "OrdinaryTexture");
        Assert.Contains(
            inner.Entries.OfType<UiRenderObjectEntry>(),
            value => value.Value.Name == "InnerTexture");
        Assert.Contains(
            plan.OfType<UiRenderObjectEntry>(),
            value => value.Value.Name == "SiblingTexture");
        Assert.DoesNotContain(
            outer.Entries.OfType<UiRenderObjectEntry>(),
            value => value.Value.Name == "InnerTexture");
    }

    [Fact]
    public void FrameBufferRenderAlphaStopsBeforeCompositeOwner()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local ancestor=CreateFrame('Frame','AlphaAncestor',UIParent); " +
            "ancestor:SetAlpha(0.25); " +
            "local buffer=CreateFrame('Frame','AlphaBuffer',ancestor); " +
            "buffer:SetAlpha(0.5); buffer:SetIsFrameBuffer(true); " +
            "local child=CreateFrame('Frame','AlphaChild',buffer); " +
            "child:SetAlpha(0.75); " +
            "local texture=child:CreateTexture('AlphaTexture'); texture:SetAlpha(0.5)");

        var ancestor = session.Ui.Find("AlphaAncestor")!;
        var buffer = session.Ui.Find("AlphaBuffer")!;
        var child = session.Ui.Find("AlphaChild")!;
        var texture = session.Ui.Find("AlphaTexture")!;

        Assert.Equal(
            95f / 255f,
            session.Ui.EffectiveAlpha(texture),
            5);
        Assert.Equal(
            95f / 255f,
            session.Ui.RenderAlpha(texture, buffer.Id),
            5);
        Assert.Equal(
            32f / 255f,
            session.Ui.RenderAlpha(buffer, null),
            5);
    }

    private static IEnumerable<UiRenderBatchEntry> Descendants(
        IEnumerable<UiRenderBatchEntry> entries)
    {
        foreach (var entry in entries)
        {
            yield return entry;
            if (entry is UiFrameBufferBatchEntry batch)
            {
                foreach (var child in Descendants(batch.Entries))
                    yield return child;
            }
        }
    }
}
