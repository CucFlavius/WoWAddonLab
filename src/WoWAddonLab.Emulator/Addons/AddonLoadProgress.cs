namespace WoWAddonLab.Emulator.Addons;

public sealed record AddonLoadProgress(
    string AddonName,
    int Completed,
    int Total);
