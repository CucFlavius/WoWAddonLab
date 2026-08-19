using System.Numerics;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class StatusBarContractTests
{
    [Fact]
    public void FillProgressRoundsTheNativeUnfilledExtent()
    {
        var standard = UiStatusBarFillGeometry.Resolve(
            new UiRect(0, 0, 10.3f, 4),
            0.5,
            "HORIZONTAL",
            0,
            false,
            true,
            1);
        var centered = UiStatusBarFillGeometry.Resolve(
            new UiRect(0, 0, 10.3f, 4),
            0.5,
            "HORIZONTAL",
            2,
            false,
            true,
            1);

        Assert.Equal(5.3f, standard.Bounds.Width, 4);
        Assert.Equal(3f, centered.Bounds.Left, 4);
        Assert.Equal(4.3f, centered.Bounds.Width, 4);
        Assert.Equal(5.3f / 10.3f, standard.NormalizedUv![2].X, 4);
        Assert.Equal(4.3f / 10.3f, centered.NormalizedUv![2].X, 4);
    }

    [Fact]
    public void FillGeometryCropsUvsForEveryNativeOrientationMode()
    {
        var bounds = new UiRect(10, 20, 100, 40);

        var horizontal = UiStatusBarFillGeometry.Resolve(
            bounds, 0.25, "HORIZONTAL", 0, false, true);
        Assert.Equal(new UiRect(10, 20, 25, 40), horizontal.Bounds);
        Assert.Equal(
            [
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(0.25f, 0),
                new Vector2(0.25f, 1)
            ],
            horizontal.NormalizedUv!);

        var vertical = UiStatusBarFillGeometry.Resolve(
            bounds, 0.25, "VERTICAL", 3, false, true);
        Assert.Equal(new UiRect(10, 50, 100, 10), vertical.Bounds);
        Assert.Equal(
            [
                new Vector2(0, 0.75f),
                new Vector2(0, 1),
                new Vector2(1, 0.75f),
                new Vector2(1, 1)
            ],
            vertical.NormalizedUv!);

        var rotatedVertical = UiStatusBarFillGeometry.Resolve(
            bounds, 0.25, "VERTICAL", 0, true, true);
        Assert.Equal(
            [
                new Vector2(0.25f, 0),
                new Vector2(0, 0),
                new Vector2(0.25f, 1),
                new Vector2(0, 1)
            ],
            rotatedVertical.NormalizedUv!);

        var rotatedHorizontal = UiStatusBarFillGeometry.Resolve(
            bounds, 0.25, "HORIZONTAL", 0, true, true);
        Assert.Equal(
            [
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, 0.75f),
                new Vector2(1, 0.75f)
            ],
            rotatedHorizontal.NormalizedUv!);
    }

    [Fact]
    public void TiledFillShrinksGeometryWithoutOverridingTextureCoordinates()
    {
        var result = UiStatusBarFillGeometry.Resolve(
            new UiRect(0, 0, 80, 20),
            0.5,
            "HORIZONTAL",
            2,
            false,
            false);

        Assert.Equal(new UiRect(20, 0, 40, 20), result.Bounds);
        Assert.Null(result.NormalizedUv);
    }

    [Fact]
    public void FillTextureObjectsAreOwnedLayeredAndReplacedLikeNativeRegions()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:ARTWORK:true:true:false:false:false:1",
            session.Lua.Evaluate(
                "local bar=CreateFrame('StatusBar',nil,UIParent); " +
                "local firstOwner=CreateFrame('Frame',nil,UIParent); " +
                "local first=firstOwner:CreateTexture(); first:SetTexture(1); " +
                "local secondOwner=CreateFrame('Frame',nil,UIParent); " +
                "local second=secondOwner:CreateTexture(); second:SetTexture(2); " +
                "second:SetPoint('CENTER',secondOwner,'CENTER',4,5); " +
                "local firstResult=bar:SetStatusBarTexture(first); " +
                "local firstOwned=first:GetParent()==bar; " +
                "local layer=first:GetDrawLayer(); " +
                "local secondResult=bar:SetStatusBarTexture(second); " +
                "local badObject=pcall(bar.SetStatusBarTexture,bar,firstOwner); " +
                "local badTable=pcall(bar.SetStatusBarTexture,bar,{}); " +
                "return table.concat({tostring(firstResult),layer," +
                "tostring(firstOwned),tostring(secondResult)," +
                "tostring(first:IsShown()),tostring(badObject)," +
                "tostring(badTable),second:GetNumPoints()},':')"));
    }
}
