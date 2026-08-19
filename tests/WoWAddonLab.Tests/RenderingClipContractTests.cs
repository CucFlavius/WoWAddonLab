using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class RenderingClipContractTests
{
    [Fact]
    public void FractionalViewportClipPreservesFragmentCenterCoverage()
    {
        var clip = UiFragmentClip.FromTopLeft(
            10.75f,
            20.25f,
            30.6f,
            40.9f,
            100,
            100);

        Assert.Equal(
            new UiFragmentClip(10.75f, 59.1f, 30.6f, 79.75f),
            clip);
        Assert.Equal(
            new UiIntegerScissor(10, 59, 21, 21),
            clip.ConservativeScissor());

        Assert.False(clip.ContainsFragmentCenter(10.5f, 59.5f));
        Assert.True(clip.ContainsFragmentCenter(11.5f, 59.5f));
        Assert.True(clip.ContainsFragmentCenter(30.5f, 79.5f));
        Assert.False(clip.ContainsFragmentCenter(31.5f, 79.5f));
    }

    [Fact]
    public void ViewportClipClampsToFramebufferBeforeScissorConversion()
    {
        var clip = UiFragmentClip.FromTopLeft(
            -4.5f,
            -2.25f,
            104.75f,
            103.5f,
            100,
            100);

        Assert.Equal(new UiFragmentClip(0, 0, 100, 100), clip);
        Assert.Equal(
            new UiIntegerScissor(0, 0, 100, 100),
            clip.ConservativeScissor());
    }
}
