namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemInteractionInfo(
    string TextureKit,
    int OpenSoundKitId,
    int CloseSoundKitId,
    string TitleText,
    string TutorialText,
    string ButtonText,
    byte InteractionType,
    byte Flags,
    string? Description = null,
    string? ButtonTooltip = null,
    string? ConfirmationDescription = null,
    string? SlotTooltip = null,
    int? Cost = null,
    int? CurrencyTypeId = null,
    int? DropInSlotSoundKitId = null);
