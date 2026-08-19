namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerChoiceInfo
{
    public string ObjectGuid { get; init; } = "";
    public int ChoiceId { get; init; }
    public string QuestionText { get; init; } = "";
    public string PendingChoiceText { get; init; } = "";
    public string? UiTextureKit { get; init; }
    public bool HideWarboardHeader { get; init; }
    public bool KeepOpenAfterChoice { get; init; }
    public bool ShowChoicesAsList { get; init; }
    public bool RequiresSelection { get; init; }
    public bool ShowChoicesAsGrid { get; init; }
    public IList<WowPlayerChoiceOptionInfo> Options { get; } = [];
    public int? SoundKitId { get; init; }
    public int? CloseUiSoundKitId { get; init; }
}
