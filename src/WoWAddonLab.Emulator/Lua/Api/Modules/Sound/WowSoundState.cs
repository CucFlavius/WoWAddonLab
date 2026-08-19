using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSoundState
{
    public bool Available { get; set; } = true;
    public bool PlaybackSuppressed { get; set; }
    public Func<int, bool>? SoundKitExists { get; set; }
    public Func<string, uint>? ResolveFileDataId { get; set; }
    public ISet<int> UnavailableSoundKitIds { get; } = new HashSet<int>();
    public ISet<uint> UnavailableFileDataIds { get; } = new HashSet<uint>();
    public ISet<uint> MutedFileDataIds { get; } = new HashSet<uint>();
    public ISet<string> UnavailableFilePaths { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<int, WowSoundPlayback> ActivePlaybacks { get; } =
        new Dictionary<int, WowSoundPlayback>();
    public IList<WowSoundPlayback> PlaybackRequests { get; } =
        new List<WowSoundPlayback>();
    public IList<WowSoundStopRequest> StopRequests { get; } =
        new List<WowSoundStopRequest>();
}
