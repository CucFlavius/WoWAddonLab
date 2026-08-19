using System.Text.Json;
using System.Text.Json.Serialization;
using WoWAddonLab.Emulator.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEditModeState
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly EmulatorLog? _log;
    private readonly string? _path;

    public WowEditModeState(string? savedVariablesDirectory = null, EmulatorLog? log = null)
    {
        _log = log;
        _path = savedVariablesDirectory is null
            ? null
            : Path.Combine(savedVariablesDirectory, "Client", "EditMode.json");
        for (var setting = 0; setting <= 33; setting++)
            AccountSettings[setting] = 0;
        AccountSettings[1] = 100;
        Load();
    }

    public Dictionary<int, int> AccountSettings { get; } = [];
    public int ActiveLayout { get; set; } = 1;
    public int SavedLayoutsReference { get; set; }
    internal List<WowEditModeLayoutInfo> SavedLayouts { get; } = [];
    public int ExitRequestCount { get; set; }
    public List<(int LayoutIndex, bool Activate, bool Imported)> AddedLayoutRequests { get; } = [];
    public List<int> DeletedLayoutRequests { get; } = [];
    public List<(int Setting, int Value)> AccountSettingRequests { get; } = [];
    public List<int> ActiveLayoutRequests { get; } = [];

    public void Persist()
    {
        if (_path is null)
            return;

        try
        {
            var directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = _path + ".tmp";
            var snapshot = new WowEditModeSnapshot
            {
                ActiveLayout = ActiveLayout,
                AccountSettings = new Dictionary<int, int>(AccountSettings),
                Layouts = [.. SavedLayouts]
            };
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(snapshot, JsonOptions));
            File.Move(temporaryPath, _path, true);
        }
        catch (Exception exception)
        {
            _log?.Warn("edit-mode", "Could not save the local Edit Mode profile.", exception.Message);
        }
    }

    private void Load()
    {
        if (_path is null || !File.Exists(_path))
            return;

        try
        {
            var snapshot = JsonSerializer.Deserialize<WowEditModeSnapshot>(
                File.ReadAllText(_path),
                JsonOptions);
            if (snapshot is null)
                return;

            ActiveLayout = Math.Max(1, snapshot.ActiveLayout);
            foreach (var (setting, value) in snapshot.AccountSettings)
            {
                if (setting is >= 0 and <= 33)
                    AccountSettings[setting] = value;
            }
            SavedLayouts.AddRange(snapshot.Layouts);
        }
        catch (Exception exception)
        {
            _log?.Warn("edit-mode", "Could not load the local Edit Mode profile.", exception.Message);
        }
    }
}
