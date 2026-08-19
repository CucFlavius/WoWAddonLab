namespace WoWAddonLab.Emulator.Lua;

public sealed class WowVoiceChatState
{
    public bool CanAccessSettings { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsLoggedIn { get; set; }
    public bool IsConnected { get; set; }
    public bool CanPlayerUseVoiceChat { get; set; } = true;
    public int OutputVolume { get; set; } = 50;
    public int InputVolume { get; set; } = 50;
    public int VadSensitivity { get; set; } = 43;
    public double MasterVolumeScale { get; set; } = 1;
    public int CommunicationMode { get; set; }
    public bool AreOutputDevicesAvailable { get; set; } = true;
    public bool AreInputDevicesAvailable { get; set; } = true;
    public bool IsCommunicationModeAvailable { get; set; } = true;
    public bool IsOutputVolumeAvailable { get; set; } = true;
    public bool IsInputVolumeAvailable { get; set; } = true;
    public bool IsVadSensitivityAvailable { get; set; } = true;
    public string OutputDeviceId { get; set; } = string.Empty;
    public string InputDeviceId { get; set; } = string.Empty;
    public bool IsCapturingLocally { get; set; }
    public bool ListenToLocalUser { get; set; }
    public List<WowVoiceAudioDeviceState> OutputDevices { get; } = [];
    public List<WowVoiceAudioDeviceState> InputDevices { get; } = [];
}
