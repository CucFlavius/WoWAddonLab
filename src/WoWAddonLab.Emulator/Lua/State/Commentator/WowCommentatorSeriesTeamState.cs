namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCommentatorSeriesTeamState(string name)
{
    public string Name { get; } = name;
    public uint Score { get; set; }
}
