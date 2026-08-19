using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialQueueState
{
    public WowSocialQueueConfig? Config { get; set; }
    public string? CurrentGroupGuid { get; set; }
    public IDictionary<string, WowSocialQueueGroup> Groups { get; } =
        new Dictionary<string, WowSocialQueueGroup>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, WowSocialQueuePlayerGroup> GroupsByPlayer { get; } =
        new Dictionary<string, WowSocialQueuePlayerGroup>(
            StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, bool> JoinRequestResults { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    public bool DefaultJoinRequestResult { get; set; }
    public IList<WowSocialQueueRequest> Requests { get; } = [];
}
