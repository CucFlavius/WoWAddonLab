namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAuraAnchorPointState(
    string Point,
    int RelativeToObjectId,
    string RelativePoint,
    double OffsetX,
    double OffsetY);
