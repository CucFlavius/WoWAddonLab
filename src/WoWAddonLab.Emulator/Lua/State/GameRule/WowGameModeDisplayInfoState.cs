namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGameModeDisplayInfoState(
    int Logo,
    int LogoHeight,
    int LogoVerticalOffset,
    int LogoShrunkenHeight,
    bool LogoUsesDarkBackdrop,
    int CharacterCreateExtraHeight,
    int CharacterCreateOuterBorder);
