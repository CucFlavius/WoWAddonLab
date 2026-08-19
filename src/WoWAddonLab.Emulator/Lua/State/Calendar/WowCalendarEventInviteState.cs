using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCalendarEventInviteState
{
    public ulong InviteId { get; set; }
    public string? Name { get; set; }
    public int Level { get; set; }
    public string? ClassName { get; set; }
    public string? ClassFilename { get; set; }
    public byte? InviteStatus { get; set; }
    public byte ModeratorStatus { get; set; }
    public bool InviteIsMine { get; set; }
    public byte Type { get; set; }
    public bool IsInPlayerGroup { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? ClassId { get; set; }
    public string Guid { get; set; } = string.Empty;
    public DateTime? ResponseTime { get; set; }
}
