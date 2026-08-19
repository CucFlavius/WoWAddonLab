using System.Globalization;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCVarState
{
    private readonly Dictionary<string, WowCVarEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, WowCVarEntry> Entries => _entries;
    public event Action<string, string>? ValueChanged;

    public WowCVarState()
    {
        ResetToBuiltIns();
    }

    public void ResetToBuiltIns()
    {
        _entries.Clear();

        Define("gxMonitor", "0");
        Define("gxapi", "d3d12");
        Define("MSAAQuality", "0");
        Define("MSAAAlphaTest", "1");
        Define("ffxAntiAliasingMode", "0");
        Define("cameraFov", "90");
        Define("physicsLevel", "1");
        Define("LowLatencyMode", "0");
        Define("uiScale", "1");
        Define("useUiScale", "0");
        Define("uiScaleMultiplier", "-1");
        Define("RenderScale", "1");
        Define("ResampleSharpness", "0.2");
        Define("textureFilteringMode", "5");
        Define("worldBaseMip", "0");
        Define("graphicsTextureResolution", "2");
        Define("encounterTimelineIconographyHiddenMask", "0");
        Define("nameplateAuraScale", "1.000000");
        Define("nameplateDebuffPadding", "0");
        Define("nameplateSize", "1");
        Define("nameplateStyle", "0");
        Define("SoftTargetNameplateSize", "19");
        Define("userFontScale", "1");
        Define("partyBackgroundOpacity", "0.5");
        Define("spellActivationOverlayOpacity", "0.65");
        Define("timeMgrUseMilitaryTime", "0");
        Define("timeMgrUseLocalTime", "0");
        Define("timeMgrAlarmTime", "0");
        Define("timeMgrAlarmMessage", "");
        Define("timeMgrAlarmEnabled", "0");
        Define("minimapZoom", "0");
        Define("cameraYawMoveSpeed", "180");
        Define("cameraPitchMoveSpeed", "90");
        Define("mouseSpeed", "1");
        Define("cameraYawSmoothSpeed", "180");
        Define("cameraPitchSmoothSpeed", "45");
        Define("test_cameraOverShoulder", "0");
        Define("CameraKeepCharacterCentered", "1");
        Define("CameraReduceUnexpectedMovement", "0");
        Define("ShakeStrengthCamera", "1");
        Define("ShakeStrengthUI", "1");
        Define("colorblindMode", "0");
        Define("colorblindSimulator", "0");
        Define("colorblindWeaknessFactor", "0.5");
        Define("lastTalkedToGM", "");
        Define("housingStoragePanelWidth", "600");
        Define("housingStoragePanelHeight", "651");
        Define("housingStoragePanelCollapsed", "0");
        Define("raidFramesHealthBarColor", "FF2B9305");
        Define("raidFramesHealthBarColorBG", "FF141414");
        Define("Sound_PingVolume", "1.0");
        Define("Sound_MasterVolume", "1.0");
        Define("Sound_MusicVolume", "0.4");
        Define("Sound_SFXVolume", "1.0");
        Define("Sound_AmbienceVolume", "0.6");
        Define("Sound_DialogVolume", "1.0");
        Define("Sound_GameplaySFX", "1.0");
        Define("Sound_EncounterWarningsVolume", "1.0");
        Define("Sound_OutputDriverIndex", "0");
        Define("VoiceOutputVolume", "50");
        Define("VoiceInputVolume", "50");
        Define("VoiceVADSensitivity", "43");
        Define("VoiceChatMasterVolumeScale", "1");
        Define("disableServerNagle", "1");
        Define("useIPv6", "0");
        Define("advancedCombatLogging", "0");
        Define("movieSubtitle", "1");
        Define("movieSubtitleBackground", "1");
        Define("movieSubtitleBackgroundAlpha", "70");
        Define("loadDeprecationFallbacks", "1");
    }

    public int ImportConfigFile(string path)
    {
        if (!File.Exists(path))
            return 0;
        return ImportConfigLines(File.ReadLines(path));
    }

    public int ImportConfigLines(IEnumerable<string> lines)
    {
        var imported = 0;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
                continue;

            var separator = line.IndexOf(' ', 4);
            if (separator <= 4)
                continue;
            var name = line[4..separator].Trim();
            var encodedValue = line[(separator + 1)..].Trim();
            if (name.Length == 0)
                continue;

            var value = encodedValue.Length >= 2 &&
                        encodedValue[0] == '"' &&
                        encodedValue[^1] == '"'
                ? encodedValue[1..^1]
                    .Replace("\\\"", "\"", StringComparison.Ordinal)
                    .Replace("\\\\", "\\", StringComparison.Ordinal)
                : encodedValue;
            SetValue(name, value);
            imported++;
        }
        return imported;
    }

    public WowCVarEntry SetValue(string name, string value)
    {
        if (!CanAcceptValue(name, value))
        {
            return _entries.TryGetValue(name, out var current)
                ? current
                : Define(name, value);
        }

        WowCVarEntry entry;
        var changed = true;
        if (_entries.TryGetValue(name, out var existing))
        {
            changed = !string.Equals(existing.Value, value, StringComparison.Ordinal);
            existing.Value = value;
            entry = existing;
        }
        else
        {
            entry = Define(name, value);
        }

        ApplyDependentCVarValues(name, value);
        if (changed)
            ValueChanged?.Invoke(entry.Name, value);
        return entry;
    }

    private static bool CanAcceptValue(string name, string value)
    {
        if (name.Equals(
                "textureFilteringMode",
                StringComparison.OrdinalIgnoreCase))
        {
            return WowTextureFilteringModeCVar.TryResolve(value, out _);
        }
        if (name.Equals("MSAAQuality", StringComparison.OrdinalIgnoreCase))
            return WowMsaaQualityCVar.TryResolve(value, out _, out _);
        if (name.Equals("MSAAAlphaTest", StringComparison.OrdinalIgnoreCase))
            return WowMsaaAlphaTestCVar.TryResolve(value, out _);
        if (name.Equals(
                "graphicsTextureResolution",
                StringComparison.OrdinalIgnoreCase))
        {
            return WowGraphicsTextureResolutionCVar.TryResolve(value, out _);
        }
        return true;
    }

    private void ApplyDependentCVarValues(string name, string value)
    {
        if (!name.Equals("graphicsTextureResolution", StringComparison.OrdinalIgnoreCase) ||
            !WowGraphicsTextureResolutionCVar.TryResolve(
                value,
                out var resolvedWorldBaseMip))
        {
            return;
        }

        if (!_entries.TryGetValue("worldBaseMip", out var worldBaseMip))
            return;

        worldBaseMip.Value = resolvedWorldBaseMip.ToString(CultureInfo.InvariantCulture);
    }

    public WowCVarEntry Define(
        string name,
        string defaultValue,
        string? value = null,
        bool isStoredServerAccount = false,
        bool isStoredServerCharacter = false,
        bool isLockedFromUser = false,
        bool isSecure = false,
        bool isReadOnly = false)
    {
        var entry = new WowCVarEntry
        {
            Name = name,
            DefaultValue = defaultValue,
            Value = value ?? defaultValue,
            IsStoredServerAccount = isStoredServerAccount,
            IsStoredServerCharacter = isStoredServerCharacter,
            IsLockedFromUser = isLockedFromUser,
            IsSecure = isSecure,
            IsReadOnly = isReadOnly
        };
        _entries[name] = entry;
        return entry;
    }

    public WowCVarEntry Register(string name, string? defaultValue)
    {
        if (_entries.TryGetValue(name, out var existing))
            return existing;
        return Define(name, defaultValue ?? string.Empty);
    }

    public bool TryGet(string? name, out WowCVarEntry entry)
    {
        if (name is not null && _entries.TryGetValue(name, out var found))
        {
            entry = found;
            return true;
        }

        entry = null!;
        return false;
    }
}
