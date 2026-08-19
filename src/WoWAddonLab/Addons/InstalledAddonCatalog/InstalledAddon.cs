using WoWAddonLab.Emulator.Addons;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Addons;

public sealed record InstalledAddon(
    string DirectoryName,
    string RootPath,
    AddonManifest? Manifest,
    string Title,
    string PlainTitle,
    string? Version,
    string? InterfaceVersion,
    IReadOnlyList<string> RequiredDependencies,
    bool LoadOnDemand,
    string? Error);
