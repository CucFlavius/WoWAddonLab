using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public enum WowAddOnPerformanceMessageType
{
    SpecificAddOnChatWarning = 0,
    SpecificAddOnErrorDialog = 1,
    OverallAddOnErrorDialog = 2
}
