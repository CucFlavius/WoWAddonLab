namespace WoWAddonLab.Emulator.UI;

public interface IWowGlobalColorProvider
{
    IReadOnlyList<WowGlobalColor> Colors { get; }
}
