namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEventUtilsState
{
    public ISet<string> ValidEvents { get; } =
        new HashSet<string>(StringComparer.Ordinal);
}
