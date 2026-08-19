using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowModelResourceProvider
{
    bool SimulateUnresolvedModels => false;

    uint ResolveFileDataId(string assetPath);

    bool FileExists(uint fileDataId);

    WowModelAnimationPayloadState GetAnimationSequencePayloadState(
        uint modelFileDataId,
        uint animationFileDataId) =>
        FileExists(animationFileDataId)
            ? WowModelAnimationPayloadState.Resident
            : WowModelAnimationPayloadState.Pending;

    WowModelResourceMetadata? GetMetadata(uint fileDataId) => null;

    bool TryGetAnimationFallback(
        ushort animationId,
        out WowAnimationFallback fallback)
    {
        fallback = default;
        return false;
    }

    bool TryGetAnimationKit(
        int animationKitId,
        out WowAnimationKitDefinition animationKit)
    {
        animationKit = null!;
        return false;
    }

    bool TryGetSpellVisualKit(
        uint spellVisualKitId,
        out WowSpellVisualKitDefinition spellVisualKit)
    {
        spellVisualKit = null!;
        return false;
    }

    bool TryGetShadowyEffect(
        uint shadowyEffectId,
        out WowShadowyEffectDefinition shadowyEffect)
    {
        shadowyEffect = default;
        return false;
    }

    bool TryGetEdgeGlowEffect(
        uint edgeGlowEffectId,
        out WowEdgeGlowEffectDefinition edgeGlowEffect)
    {
        edgeGlowEffect = default;
        return false;
    }

    bool TryGetDissolveEffect(
        uint dissolveEffectId,
        out WowDissolveEffectDefinition dissolveEffect)
    {
        dissolveEffect = default;
        return false;
    }
}
