namespace WoWAddonLab.Emulator.Lua;

public static class WowTextureFilteringModeCVar
{
    public const int DefaultMode = 5;
    public const int ModeCount = 6;

    public static bool TryResolve(string value, out int mode)
    {
        mode = WowGraphicsCVarIntegerParser.ParseStrtol(value);
        return mode >= 0 && mode < ModeCount;
    }
}
