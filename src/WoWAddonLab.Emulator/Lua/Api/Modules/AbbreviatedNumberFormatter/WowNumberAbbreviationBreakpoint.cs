using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal readonly record struct WowNumberAbbreviationBreakpoint(
    double Breakpoint,
    string Abbreviation,
    double SignificandDivisor,
    double FractionDivisor,
    bool AbbreviationIsGlobal);
