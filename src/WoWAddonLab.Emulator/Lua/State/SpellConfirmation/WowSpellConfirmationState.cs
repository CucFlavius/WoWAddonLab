namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellConfirmationState
{
    public bool IsPlayerAvailable { get; set; } = true;
    public List<WowSpellConfirmationPrompt> Prompts { get; } = [];
}
