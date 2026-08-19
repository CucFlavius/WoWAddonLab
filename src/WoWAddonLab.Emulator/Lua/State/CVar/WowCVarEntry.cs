using System.Globalization;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCVarEntry
{
    public required string Name { get; init; }
    public required string DefaultValue { get; init; }
    public required string Value { get; set; }
    public bool IsStoredServerAccount { get; init; }
    public bool IsStoredServerCharacter { get; init; }
    public bool IsLockedFromUser { get; init; }
    public bool IsSecure { get; init; }
    public bool IsReadOnly { get; init; }
}
