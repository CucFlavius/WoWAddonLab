using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record XmlTemplateKeyValueInfo(
    string Key,
    string KeyType,
    string Type,
    string Value);
