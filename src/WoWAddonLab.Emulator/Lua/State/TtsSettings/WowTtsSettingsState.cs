namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTtsSettingsState
{
    private static readonly string[] NativeDefaultChatTypes =
    [
        "SYSTEM",
        "SAY",
        "PARTY",
        "RAID",
        "GUILD",
        "OFFICER",
        "YELL",
        "WHISPER",
        "DND",
        "BN",
        "BN_WHISPER_INFORM",
        "VOICE_TEXT",
        "ACHIEVEMENT",
        "GUILD_ACHIEVEMENT",
        "PARTY_LEADER",
        "INSTANCE_CHAT",
        "INSTANCE_CHAT_LEADER",
        "RAID_LEADER",
        "LOOT",
        "CURRENCY",
        "MONEY",
        "PING"
    ];

    public WowTtsSettingsState()
    {
        for (var setting = 0; setting <= 4; setting++)
        {
            DefaultBooleanSettings[setting] = setting != 4;
        }
        foreach (var chatType in NativeDefaultChatTypes)
        {
            DefaultChatTypes[chatType] = true;
        }
        ResetToDefaults();
    }

    public int SpeechRate { get; set; }
    public uint SpeechVolume { get; set; } = 100;
    public bool CharacterSettingsSaved { get; set; }
    public IDictionary<int, bool> BooleanSettings { get; } = new Dictionary<int, bool>();
    public IDictionary<int, uint> VoiceOptionIds { get; } = new Dictionary<int, uint>();
    public IDictionary<int, string> VoiceOptionNames { get; } = new Dictionary<int, string>();
    public IDictionary<string, bool> ChatTypes { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, bool> Channels { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<int, bool> DefaultBooleanSettings { get; } =
        new Dictionary<int, bool>();

    public IDictionary<int, uint> DefaultVoiceOptionIds { get; } =
        new Dictionary<int, uint>();

    public IDictionary<int, string> DefaultVoiceOptionNames { get; } =
        new Dictionary<int, string>();

    public IDictionary<string, bool> DefaultChatTypes { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, bool> DefaultChannels { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public ISet<(uint Language, string Message)> OverrideMessages { get; } =
        new HashSet<(uint Language, string Message)>();

    public void ResetToDefaults()
    {
        SpeechRate = 0;
        SpeechVolume = 100;
        Copy(DefaultBooleanSettings, BooleanSettings);
        Copy(DefaultVoiceOptionIds, VoiceOptionIds);
        Copy(DefaultVoiceOptionNames, VoiceOptionNames);
        Copy(DefaultChatTypes, ChatTypes);
        Copy(DefaultChannels, Channels);
    }

    private static void Copy<TKey, TValue>(
        IDictionary<TKey, TValue> source,
        IDictionary<TKey, TValue> destination)
        where TKey : notnull
    {
        destination.Clear();
        foreach (var pair in source)
        {
            destination[pair.Key] = pair.Value;
        }
    }
}
