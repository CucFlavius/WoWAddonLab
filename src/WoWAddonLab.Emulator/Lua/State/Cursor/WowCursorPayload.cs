namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCursorPayload(
    WowCursorPayloadKind Kind,
    IReadOnlyList<object?> InfoValues,
    WowItemLocation? ItemLocation = null);
