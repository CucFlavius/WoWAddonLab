using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCombatAudioAlertState
{
    public bool Available { get; set; } = true;
    public bool Enabled { get; set; }
    public int SpeakerSpeed { get; set; }
    public uint VoiceCount { get; set; } = uint.MaxValue;
    public uint ActiveSpecializationIndex { get; set; }
    public IDictionary<int, int> CategoryVoices { get; } = new Dictionary<int, int>();
    public IDictionary<int, int> CategoryVolumes { get; } = new Dictionary<int, int>();
    public IDictionary<(int Unit, int AlertType), int> FormatSettings { get; } =
        new Dictionary<(int Unit, int AlertType), int>();
    public IDictionary<(int Setting, uint SpecializationIndex), int> SpecSettings { get; } =
        new Dictionary<(int Setting, uint SpecializationIndex), int>();
    public IDictionary<int, float> Throttles { get; } = new Dictionary<int, float>();
    public ISet<string> KnownTargetingUnits { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public WowCombatAudioAlertSpeech? LastSpeechRequest { get; internal set; }
    public IList<WowCombatAudioAlertSpeech> SpokenRequests { get; } =
        new List<WowCombatAudioAlertSpeech>();
}
