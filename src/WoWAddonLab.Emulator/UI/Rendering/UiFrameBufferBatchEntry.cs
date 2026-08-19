namespace WoWAddonLab.Emulator.UI;

public sealed record UiFrameBufferBatchEntry(
    UiObject Frame,
    IReadOnlyList<UiRenderBatchEntry> Entries) : UiRenderBatchEntry;
