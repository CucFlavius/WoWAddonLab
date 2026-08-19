namespace WoWAddonLab.Emulator.Lua;

public interface IWowSpellProvider
{
    int Count { get; }
    WowSpellStaticInfo? Find(int id);
    int FindIdByName(string name);
}
