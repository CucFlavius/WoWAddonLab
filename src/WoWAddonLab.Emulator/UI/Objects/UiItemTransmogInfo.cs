using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiItemTransmogInfo(
    int AppearanceId,
    int SecondaryAppearanceId,
    int IllusionId);
