using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSecureExecutionState
{
    private readonly Dictionary<string, int> _secureReferences =
        new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> SecureReferences => _secureReferences;

    internal void TryStoreReference(string name, int objectId) =>
        _secureReferences.TryAdd(name, objectId);
}
