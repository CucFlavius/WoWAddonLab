namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGuildNewsInfo
{
    public bool IsSticky { get; init; }
    public bool IsHeader { get; init; }
    public int NewsType { get; init; }
    public string? WhoText { get; init; }
    public string? WhatText { get; init; }
    public int NewsDataId { get; init; }
    public IReadOnlyList<int> Data { get; init; } = [];
    public int Weekday { get; init; }
    public int Day { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public int GuildMembersPresent { get; init; }
}
