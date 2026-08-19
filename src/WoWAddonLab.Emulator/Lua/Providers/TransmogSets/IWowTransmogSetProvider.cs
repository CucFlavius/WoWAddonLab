namespace WoWAddonLab.Emulator.Lua;

public interface IWowTransmogSetProvider
{
    IReadOnlyList<WowTransmogSetDefinition> Sets { get; }

    bool TryGetSet(int setId, out WowTransmogSetDefinition definition);

    IReadOnlyList<int> GetSourceIds(int setId);

    IReadOnlyList<WowTransmogSetDefinition> GetVariantSets(int setId);

    IReadOnlyList<int> GetSetIdsContainingSource(int sourceId);
}
