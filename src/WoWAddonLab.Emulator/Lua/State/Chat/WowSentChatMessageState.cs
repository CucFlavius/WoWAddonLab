namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSentChatMessageState(
    string Message,
    string ChatType,
    uint? LanguageId,
    string? Target);
