using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

internal sealed class TestModelResourceProvider : IWowModelResourceProvider
{
    public bool SimulateUnresolvedModels { get; set; }

    public Dictionary<string, uint> Paths { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<uint> ExistingFileDataIds { get; } = [];

    public HashSet<uint> FailedAnimationFileDataIds { get; } = [];

    public Dictionary<uint, WowModelResourceMetadata> Metadata { get; } = [];

    public Dictionary<ushort, WowAnimationFallback> AnimationFallbacks { get; } = [];

    public Dictionary<int, WowAnimationKitDefinition> AnimationKits { get; } = [];

    public Dictionary<uint, WowSpellVisualKitDefinition> SpellVisualKits { get; } = [];

    public Dictionary<uint, WowShadowyEffectDefinition> ShadowyEffects { get; } = [];

    public Dictionary<uint, WowEdgeGlowEffectDefinition> EdgeGlowEffects { get; } = [];

    public Dictionary<uint, WowDissolveEffectDefinition> DissolveEffects { get; } = [];

    public uint ResolveFileDataId(string assetPath) =>
        Paths.GetValueOrDefault(assetPath);

    public bool FileExists(uint fileDataId) =>
        ExistingFileDataIds.Contains(fileDataId);

    public WowModelAnimationPayloadState GetAnimationSequencePayloadState(
        uint modelFileDataId,
        uint animationFileDataId) =>
        FailedAnimationFileDataIds.Contains(animationFileDataId)
            ? WowModelAnimationPayloadState.Failed
            : ExistingFileDataIds.Contains(animationFileDataId)
                ? WowModelAnimationPayloadState.Resident
                : WowModelAnimationPayloadState.Pending;

    public WowModelResourceMetadata? GetMetadata(uint fileDataId) =>
        Metadata.GetValueOrDefault(fileDataId);

    public bool TryGetAnimationFallback(
        ushort animationId,
        out WowAnimationFallback fallback) =>
        AnimationFallbacks.TryGetValue(animationId, out fallback);

    public bool TryGetAnimationKit(
        int animationKitId,
        out WowAnimationKitDefinition animationKit) =>
        AnimationKits.TryGetValue(animationKitId, out animationKit!);

    public bool TryGetSpellVisualKit(
        uint spellVisualKitId,
        out WowSpellVisualKitDefinition spellVisualKit) =>
        SpellVisualKits.TryGetValue(spellVisualKitId, out spellVisualKit!);

    public bool TryGetShadowyEffect(
        uint shadowyEffectId,
        out WowShadowyEffectDefinition shadowyEffect) =>
        ShadowyEffects.TryGetValue(shadowyEffectId, out shadowyEffect);

    public bool TryGetEdgeGlowEffect(
        uint edgeGlowEffectId,
        out WowEdgeGlowEffectDefinition edgeGlowEffect) =>
        EdgeGlowEffects.TryGetValue(edgeGlowEffectId, out edgeGlowEffect);

    public bool TryGetDissolveEffect(
        uint dissolveEffectId,
        out WowDissolveEffectDefinition dissolveEffect) =>
        DissolveEffects.TryGetValue(dissolveEffectId, out dissolveEffect);
}
