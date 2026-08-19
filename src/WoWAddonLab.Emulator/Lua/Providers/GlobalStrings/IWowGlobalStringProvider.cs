using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowGlobalStringProvider
{
    IReadOnlyDictionary<string, string> Strings { get; }
}
