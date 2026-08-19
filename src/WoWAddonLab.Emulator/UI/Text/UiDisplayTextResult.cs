using System.Text;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiDisplayTextResult(
    string Text,
    bool WasTruncated);
