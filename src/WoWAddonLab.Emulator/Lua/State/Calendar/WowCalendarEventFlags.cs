using System.Text;

namespace WoWAddonLab.Emulator.Lua;

[Flags]
public enum WowCalendarEventFlags : ushort
{
    None = 0,
    Player = 0x1,
    System = 0x4,
    Holiday = 0x8,
    Locked = 0x10,
    AutoApprove = 0x20,
    GuildAnnouncement = 0x40,
    RaidLockout = 0x80,
    RaidReset = 0x200,
    CommunityEvent = 0x400,
    GuildEvent = 0x800
}
