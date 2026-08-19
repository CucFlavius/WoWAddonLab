namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClubFinderWhisperRequest(
    string ClubFinderGuid,
    string PlayerGuid,
    int ApplicantType,
    string Name);
