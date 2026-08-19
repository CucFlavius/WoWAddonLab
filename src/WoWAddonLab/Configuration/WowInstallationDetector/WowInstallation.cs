using Microsoft.Win32;

namespace WoWAddonLab.Configuration;

public sealed record WowInstallation(
    string RootPath,
    string ProductPath,
    WowProduct Product,
    string? Version);
