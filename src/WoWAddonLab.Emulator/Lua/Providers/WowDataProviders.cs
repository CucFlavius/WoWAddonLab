using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDataProviders
{
    private IWowAtlasProvider? _atlas;
    private IWowDyeColorProvider? _dyeColor;
    private IWowGlobalColorProvider? _globalColor;
    private IWowGameRuleProvider? _gameRule;
    private IWowMapProvider? _map;
    private IWowQuestProvider? _quest;
    private IWowGlobalStringProvider? _globalString;
    private IWowAchievementProvider? _achievement;
    private IWowAccountStoreProvider? _accountStore;
    private IWowAzeriteEssenceProvider? _azeriteEssence;
    private IWowModelInfoProvider? _modelInfo;
    private IWowModelResourceProvider? _modelResource;
    private IWowMacroIconProvider? _macroIcon;
    private IWowSpellProvider? _spell;
    private IWowItemProvider? _item;
    private IWowItemClassProvider? _itemClass;
    private IWowInventorySlotProvider? _inventorySlot;
    private IWowRaceProvider? _race;
    private IWowFactionProvider? _faction;
    private IWowTransmogAppearanceProvider? _transmogAppearance;
    private IWowTransmogSetProvider? _transmogSet;
    private IWowCharacterServiceProvider? _characterService;
    private IWowEncounterJournalProvider? _encounterJournal;

    internal event Action<WowDataProviderKind>? Changed;

    public IWowAtlasProvider? Atlas
    {
        get => _atlas;
        set => Set(ref _atlas, value, WowDataProviderKind.Atlas);
    }

    public IWowDyeColorProvider? DyeColor
    {
        get => _dyeColor;
        set => Set(ref _dyeColor, value, WowDataProviderKind.DyeColor);
    }

    public IWowGlobalColorProvider? GlobalColor
    {
        get => _globalColor;
        set => Set(ref _globalColor, value, WowDataProviderKind.GlobalColor);
    }

    public IWowGameRuleProvider? GameRule
    {
        get => _gameRule;
        set => Set(ref _gameRule, value, WowDataProviderKind.GameRule);
    }

    public IWowMapProvider? Map
    {
        get => _map;
        set => Set(ref _map, value, WowDataProviderKind.Map);
    }

    public IWowQuestProvider? Quest
    {
        get => _quest;
        set => Set(ref _quest, value, WowDataProviderKind.Quest);
    }

    public IWowGlobalStringProvider? GlobalString
    {
        get => _globalString;
        set => Set(ref _globalString, value, WowDataProviderKind.GlobalString);
    }

    public IWowAchievementProvider? Achievement
    {
        get => _achievement;
        set => Set(ref _achievement, value, WowDataProviderKind.Achievement);
    }

    public IWowAccountStoreProvider? AccountStore
    {
        get => _accountStore;
        set => Set(ref _accountStore, value, WowDataProviderKind.AccountStore);
    }

    public IWowAzeriteEssenceProvider? AzeriteEssence
    {
        get => _azeriteEssence;
        set => Set(ref _azeriteEssence, value, WowDataProviderKind.AzeriteEssence);
    }

    public IWowModelInfoProvider? ModelInfo
    {
        get => _modelInfo;
        set => Set(ref _modelInfo, value, WowDataProviderKind.ModelInfo);
    }

    public IWowModelResourceProvider? ModelResource
    {
        get => _modelResource;
        set => Set(ref _modelResource, value, WowDataProviderKind.ModelResource);
    }

    public IWowMacroIconProvider? MacroIcon
    {
        get => _macroIcon;
        set => Set(ref _macroIcon, value, WowDataProviderKind.MacroIcon);
    }

    public IWowSpellProvider? Spell
    {
        get => _spell;
        set => Set(ref _spell, value, WowDataProviderKind.Spell);
    }

    public IWowItemClassProvider? ItemClass
    {
        get => _itemClass;
        set => Set(ref _itemClass, value, WowDataProviderKind.ItemClass);
    }

    public IWowItemProvider? Item
    {
        get => _item;
        set => Set(ref _item, value, WowDataProviderKind.Item);
    }

    public IWowInventorySlotProvider? InventorySlot
    {
        get => _inventorySlot;
        set => Set(
            ref _inventorySlot,
            value,
            WowDataProviderKind.InventorySlot);
    }

    public IWowRaceProvider? Race
    {
        get => _race;
        set => Set(ref _race, value, WowDataProviderKind.Race);
    }

    public IWowFactionProvider? Faction
    {
        get => _faction;
        set => Set(ref _faction, value, WowDataProviderKind.Faction);
    }

    public IWowTransmogSetProvider? TransmogSet
    {
        get => _transmogSet;
        set => Set(ref _transmogSet, value, WowDataProviderKind.TransmogSet);
    }

    public IWowTransmogAppearanceProvider? TransmogAppearance
    {
        get => _transmogAppearance;
        set => Set(ref _transmogAppearance, value, WowDataProviderKind.TransmogAppearance);
    }

    public IWowCharacterServiceProvider? CharacterService
    {
        get => _characterService;
        set => Set(ref _characterService, value, WowDataProviderKind.CharacterService);
    }

    public IWowEncounterJournalProvider? EncounterJournal
    {
        get => _encounterJournal;
        set => Set(ref _encounterJournal, value, WowDataProviderKind.EncounterJournal);
    }

    private void Set<T>(ref T? field, T? value, WowDataProviderKind kind)
        where T : class
    {
        if (ReferenceEquals(field, value))
            return;

        field = value;
        Changed?.Invoke(kind);
    }
}
