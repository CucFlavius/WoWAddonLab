namespace WoWAddonLab.Emulator.Lua;

public static class WowMsaaAlphaTestCVar
{
    public static bool TryResolve(string value, out bool enabled)
    {
        var mode = WowGraphicsCVarIntegerParser.ParseStrtol(value);
        enabled = mode == 1;
        return mode is 0 or 1;
    }
}
