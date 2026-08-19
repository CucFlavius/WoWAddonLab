namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerChoiceButtonInfo
{
    public int Id { get; init; }
    public string Text { get; init; } = "";
    public bool Disabled { get; init; }
    public bool ShowCheckmark { get; init; }
    public bool HideButtonShowText { get; init; }
    public bool Selected { get; init; }
    public string? Confirmation { get; init; }
    public string? Tooltip { get; init; }
    public int? RewardQuestId { get; init; }
    public int? SoundKitId { get; init; }
    public string? ListText { get; init; }
}
