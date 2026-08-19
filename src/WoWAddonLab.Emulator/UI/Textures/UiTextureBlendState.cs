namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiTextureBlendState(
    byte NativeIndex,
    bool Enabled,
    UiBlendFactor SourceRgb,
    UiBlendFactor DestinationRgb,
    UiBlendFactor SourceAlpha,
    UiBlendFactor DestinationAlpha)
{
    public static UiTextureBlendState Resolve(UiTextureBlendMode mode) => mode switch
    {
        UiTextureBlendMode.Disable => new(
            0, false,
            UiBlendFactor.One, UiBlendFactor.Zero,
            UiBlendFactor.One, UiBlendFactor.Zero),
        UiTextureBlendMode.AlphaKey => new(
            1, false,
            UiBlendFactor.One, UiBlendFactor.Zero,
            UiBlendFactor.One, UiBlendFactor.Zero),
        UiTextureBlendMode.Blend => new(
            2, true,
            UiBlendFactor.SourceAlpha, UiBlendFactor.OneMinusSourceAlpha,
            UiBlendFactor.One, UiBlendFactor.OneMinusSourceAlpha),
        UiTextureBlendMode.Add => new(
            3, true,
            UiBlendFactor.SourceAlpha, UiBlendFactor.One,
            UiBlendFactor.Zero, UiBlendFactor.One),
        UiTextureBlendMode.Mod => new(
            4, true,
            UiBlendFactor.DestinationColor, UiBlendFactor.Zero,
            UiBlendFactor.DestinationAlpha, UiBlendFactor.Zero),
        _ => Resolve(UiTextureBlendMode.Blend)
    };
}
