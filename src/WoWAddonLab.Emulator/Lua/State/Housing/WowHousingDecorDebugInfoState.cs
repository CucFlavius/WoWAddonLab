namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingDecorDebugInfoState(
    WowHousingDecorInstanceInfoState BaseInfo,
    string AssetName,
    int FileDataId,
    WowHousingVector3State WorldPosition,
    WowHousingVector3State RotationYawPitchRoll,
    float Scale,
    string? RoomGuid,
    string? ParentGuid,
    IReadOnlyList<string> ChildDecorGuids);
