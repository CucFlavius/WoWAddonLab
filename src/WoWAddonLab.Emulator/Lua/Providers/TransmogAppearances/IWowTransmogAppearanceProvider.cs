namespace WoWAddonLab.Emulator.Lua;

public interface IWowTransmogAppearanceProvider
{
    int Count { get; }

    bool TryGetSource(int sourceId, out WowAppearanceSourceDefinition definition);

    bool TryGetSourceForItem(
        int itemId,
        int? itemModId,
        out WowAppearanceSourceDefinition definition);

    IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByCategory(int categoryId);

    IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByVisual(int visualId);
}
