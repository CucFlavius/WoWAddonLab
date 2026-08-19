namespace WoWAddonLab.Emulator.Lua;

public sealed record WowProfessionInfo(
    int? Profession = null,
    int ProfessionId = 0,
    int SourceCounter = 0,
    string? ProfessionName = "",
    string? ExpansionName = "",
    int SkillLevel = 0,
    int MaxSkillLevel = 0,
    int SkillModifier = 0,
    bool IsPrimaryProfession = false,
    int? ParentProfessionId = null,
    string? ParentProfessionName = null);
