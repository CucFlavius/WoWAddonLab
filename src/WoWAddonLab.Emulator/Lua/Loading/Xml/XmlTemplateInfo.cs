using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record XmlTemplateInfo(
    string Type,
    float Width,
    float Height,
    IReadOnlyList<XmlTemplateKeyValueInfo> KeyValues,
    string? Inherits,
    string SourceLocation);
