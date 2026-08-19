using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowDurationTextFormatComponent(
    DurationTextBindingProperty Property,
    WowDurationFormatterReference Formatter);
