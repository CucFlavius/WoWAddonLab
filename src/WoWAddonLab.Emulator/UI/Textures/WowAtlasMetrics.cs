namespace WoWAddonLab.Emulator.UI;

public static class WowAtlasMetrics
{
    public static float ResolveLogicalDimension(
        float rawDimension,
        float overrideDimension,
        float canvasScale)
    {
        if (overrideDimension > 0)
            return overrideDimension;

        return rawDimension / Math.Max(1, canvasScale);
    }
}
