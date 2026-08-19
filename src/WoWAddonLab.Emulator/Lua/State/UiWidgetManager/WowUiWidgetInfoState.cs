namespace WoWAddonLab.Emulator.Lua;

public sealed record WowUiWidgetInfoState(
    int WidgetId,
    byte WidgetType,
    int? WidgetSetId = null,
    string? UnitToken = null);
