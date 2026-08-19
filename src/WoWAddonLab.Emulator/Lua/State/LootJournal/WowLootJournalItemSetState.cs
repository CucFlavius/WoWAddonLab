namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLootJournalItemSetState(
    int SetId,
    int ItemLevel,
    string? Name,
    IReadOnlySet<int>? ClassIds = null,
    IReadOnlySet<int>? SpecializationIds = null);
