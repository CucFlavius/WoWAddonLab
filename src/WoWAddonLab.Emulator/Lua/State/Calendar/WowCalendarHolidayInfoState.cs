using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarHolidayInfoState(
    string? Name,
    string? Description,
    int TextureFileId,
    DateTime? StartTime,
    DateTime? EndTime);
