using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialQueueGroupQueueInfo
{
    public int ClientId { get; init; }
    public bool Eligible { get; init; }
    public bool NeedTank { get; init; }
    public bool NeedHealer { get; init; }
    public bool NeedDamage { get; init; }
    public bool IsAutoAccept { get; init; }
    public WowSocialQueueQueueData QueueData { get; init; } = new();
}
