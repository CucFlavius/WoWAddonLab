using System.Runtime.InteropServices;

namespace WoWAddonLab.Rendering;

internal static class DarkTitleBar
{
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeBefore20H1 = 19;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int valueSize);

    public static bool Apply(nint window)
    {
        if (!OperatingSystem.IsWindows() || window == 0)
            return false;

        var enabled = 1;
        if (DwmSetWindowAttribute(window, UseImmersiveDarkMode, ref enabled, sizeof(int)) == 0)
            return true;
        return DwmSetWindowAttribute(
            window,
            UseImmersiveDarkModeBefore20H1,
            ref enabled,
            sizeof(int)) == 0;
    }
}
