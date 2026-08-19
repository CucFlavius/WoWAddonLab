using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct WowTextSpan(string Text, uint? Argb);
