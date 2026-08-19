using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreProductCard(
    string Title,
    int ModelSceneId,
    int CreatureDisplayInfoId,
    IReadOnlyList<int> ItemModifiedAppearanceIds,
    bool DisplayTransmogItemsIndividually);
