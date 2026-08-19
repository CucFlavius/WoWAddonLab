namespace WoWAddonLab.Emulator.Lua;

public sealed class WowNamePlateState
{
    public IDictionary<string, int> ObjectIdsByUnit { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public ISet<int> ForbiddenObjectIds { get; } = new HashSet<int>();
    public double Width { get; set; } = 110;
    public double Height { get; set; } = 45;
}
