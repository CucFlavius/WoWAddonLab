using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSocialQueueConfig
{
    public bool ToastsDisabled { get; init; }
    public float ToastDuration { get; init; }
    public float DelayDuration { get; init; }
    public float QueueMultiplier { get; init; }
    public float PlayerMultiplier { get; init; }
    public float PlayerFriendValue { get; init; }
    public float PlayerGuildValue { get; init; }
    public float ThrottleInitialThreshold { get; init; }
    public float ThrottleDecayTime { get; init; }
    public float ThrottlePrioritySpike { get; init; }
    public float ThrottleMinThreshold { get; init; }
    public float ThrottlePvpPriorityNormal { get; init; }
    public float ThrottlePvpPriorityLow { get; init; }
    public float ThrottlePvpHonorThreshold { get; init; }
    public float ThrottleLfgListPriorityDefault { get; init; }
    public float ThrottleLfgListPriorityAbove { get; init; }
    public float ThrottleLfgListPriorityBelow { get; init; }
    public float ThrottleLfgListItemLevelScalingAbove { get; init; }
    public float ThrottleLfgListItemLevelScalingBelow { get; init; }
    public float ThrottleRfPriorityAbove { get; init; }
    public float ThrottleRfItemLevelScalingAbove { get; init; }
    public float ThrottleDfMaxItemLevel { get; init; }
    public float ThrottleDfBestPriority { get; init; }
}
