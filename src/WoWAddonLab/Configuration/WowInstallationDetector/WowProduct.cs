using Microsoft.Win32;

namespace WoWAddonLab.Configuration;

public sealed record WowProduct(
    string UninstallName,
    string FolderName,
    string ExecutableName,
    string ProductCode);
