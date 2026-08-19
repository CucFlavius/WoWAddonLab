namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPrivateWarningTextAnchorState(
    int ParentObjectId,
    string Point,
    int? RelativeToObjectId,
    string RelativePoint,
    double OffsetX,
    double OffsetY);
