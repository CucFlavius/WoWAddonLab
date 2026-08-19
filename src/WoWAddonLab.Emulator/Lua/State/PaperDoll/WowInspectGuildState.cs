namespace WoWAddonLab.Emulator.Lua;

public sealed record WowInspectGuildState(
    int AchievementPoints = 0,
    int MemberCount = 0,
    string GuildName = "",
    string RealmName = "");
