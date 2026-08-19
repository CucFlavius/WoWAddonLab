using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WoWAddonLab.Assets;
using WoWAddonLab.Configuration;

namespace WoWAddonLab.Addons;

public sealed record BlizzardUiBootstrapResult(
    IReadOnlyList<string> AddonPaths,
    IReadOnlyList<string> AvailableAddonPaths,
    IReadOnlyList<string> ModuleNames,
    IReadOnlyList<string> RuntimeFiles,
    int ExtractedFiles,
    IReadOnlyList<string> Warnings,
    bool CacheHit);
