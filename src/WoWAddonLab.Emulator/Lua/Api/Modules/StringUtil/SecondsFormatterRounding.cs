using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal enum SecondsFormatterRounding : byte
{
    RoundUp,
    Truncate
}
