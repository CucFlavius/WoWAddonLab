namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGuildRecipeQueryKey(
    int SkillLineId,
    int RecipeSpellId,
    int? RecipeLevel);
